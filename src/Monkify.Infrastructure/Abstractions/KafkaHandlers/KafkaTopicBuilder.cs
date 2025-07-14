using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Monkify.Domain.Configs.Entities;
using Monkify.Infrastructure.Consumers.BetPlaced;

namespace Monkify.Infrastructure.Abstractions.KafkaHandlers
{
    public static class KafkaTopicBuilder
    {
        public static void AddProducerConfiguration<TEvent>(this IServiceCollection services, string bootstrapServers, string topic, string groupId)
        {
            services.Configure<ConsumerOptions>(typeof(TEvent).Name, options =>
            {
                options.BootstrapServersUrl = bootstrapServers;
                options.Topic = topic;
                options.GroupId = groupId;
                options.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
            });
        }

        public static void AddProducer<TEvent>(this IServiceCollection services, string bootstrapServers, string topic, string groupId)
        {
            services.AddProducerConfiguration<TEvent>(bootstrapServers, topic, groupId);
            services.AddSingleton<IKafkaProducer<TEvent>, KafkaProducer<TEvent>>();
        }

        public static void AddConsumer<TEvent, TService>(this IServiceCollection services, string bootstrapServers, string topic, string groupId) where TService : BaseConsumer<TEvent>
        {
            services.AddProducerConfiguration<TEvent>(bootstrapServers, topic, groupId);

            services.AddSingleton<TService>();
            services.AddHostedService<TService>();
        }
    }
}
