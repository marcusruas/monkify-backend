using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace Monkify.Domain.Configs.Entities
{
    [ExcludeFromCodeCoverage]
    public class ConsumerOptions
    {
        public string BootstrapServersUrl { get; set; }
        public string Topic { get; set; }
        public string GroupId { get; set; }
        public AutoOffsetReset AutoOffsetReset { get; set; }
    }
}
