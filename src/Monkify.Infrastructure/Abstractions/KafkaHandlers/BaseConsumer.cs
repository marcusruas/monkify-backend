using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using MassTransit.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monkify.Domain.Configs.Entities;
using Serilog;

namespace Monkify.Infrastructure.Abstractions.KafkaHandlers
{
    public abstract class BaseConsumer<TEvent> : BackgroundService
    {
        public BaseConsumer(IOptionsMonitor<ConsumerOptions> options)
        {
            _options = options.Get(typeof(TEvent).Name);
        }

        private readonly ConsumerOptions _options;

        protected abstract Task ConsumeAsync(TEvent message, CancellationToken cancellationToken);

        protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var config = new ConsumerConfig
                    {
                        BootstrapServers = _options.BootstrapServersUrl,
                        GroupId = _options.GroupId,
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    };

                    using var consumer = new ConsumerBuilder<Null, string>(config).Build();
                    consumer.Subscribe(_options.Topic);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));

                            if (consumeResult == null || string.IsNullOrWhiteSpace(consumeResult.Message?.Value))
                                continue;

                            Log.Information("Received message {messageId} for consumer {consumer}", consumeResult.Message.Key, GetType().Name);

                            var messageBody = consumeResult.Message.Value.CastToObject<TEvent>();
                            await ConsumeAsync(messageBody, stoppingToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to consume messages from topic {Topic}.", _options.Topic);
                        }
                    }
                    consumer.Close();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to connect consumer {consumer} to topic {Topic}.", GetType().Name, _options.Topic);
                }
            });
            
        }
    }
}
