using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Monkify.Domain.Configs.Entities;
using Monkify.Infrastructure.Consumers.BetPlaced;

namespace Monkify.Infrastructure.Abstractions.KafkaHandlers
{
    public class KafkaProducer<TMessage> : IKafkaProducer<TMessage>
    {
        public KafkaProducer(IOptionsMonitor<ConsumerOptions> configuration)
        {
            _configuration = configuration.Get(typeof(TMessage).Name) ?? throw new ArgumentNullException(nameof(configuration), $"Kafka configuration for type {typeof(TMessage).Name} was not registered.");
            _producer = new ProducerBuilder<Null, string>(new ProducerConfig() { BootstrapServers = _configuration.BootstrapServersUrl, MessageTimeoutMs = 5000 }).Build();
        }

        private IProducer<Null, string> _producer { get; set; }
        private ConsumerOptions _configuration { get; set; }

        public async Task ProduceAsync(TMessage message)
        {
             await _producer.ProduceAsync(
                _configuration.Topic, 
                new Message<Null, string> { Value = JsonSerializer.Serialize(message) }
            );
        }
    }
}
