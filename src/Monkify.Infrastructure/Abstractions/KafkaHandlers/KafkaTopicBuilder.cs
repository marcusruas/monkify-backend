using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monkify.Domain.Configs.Entities;
using Monkify.Infrastructure.Consumers.BetPlaced;

namespace Monkify.Infrastructure.Abstractions.KafkaHandlers
{
    public static class KafkaTopicBuilder
    {
        public static void AddEventConfiguration<TEvent>(this IServiceCollection services, IConfiguration configuration)
        {
            var kafkaSection = configuration.GetSection("Kafka");

            var kafkaBootstrapServers = kafkaSection?.GetSection("BootstrapServersUrl")?.Value;
            if (string.IsNullOrWhiteSpace(kafkaBootstrapServers))
            {
                throw new ArgumentException("Kafka BootstrapServersUrl is not configured.");
            }

            string eventConfigurationKey = typeof(TEvent).Name;

            var eventConfig = kafkaSection.GetSection(eventConfigurationKey);

            if (eventConfig == null)
            {
                throw new ArgumentException($"Kafka configuration for event '{eventConfigurationKey}' is not found.");
            }

            services.Configure<ConsumerOptions>(eventConfigurationKey, options =>
            {
                options.BootstrapServersUrl = kafkaBootstrapServers;
                options.Topic = eventConfig.GetValue<string>("Topic");
                options.GroupId = eventConfig.GetValue<string>("GroupId");
                options.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
            });
        }

        public static void AddProducer<TEvent>(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddEventConfiguration<TEvent>(configuration);
            services.AddSingleton<IKafkaProducer<TEvent>, KafkaProducer<TEvent>>();
        }

        public static void AddConsumer<TEvent, TService>(this IServiceCollection services, IConfiguration configuration) where TService : BaseConsumer<TEvent>
        {
            services.AddEventConfiguration<TEvent>(configuration);
            services.AddSingleton<TService>();
            services.AddHostedService<TService>();
        }
    }
}
