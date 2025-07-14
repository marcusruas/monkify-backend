using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Abstractions.KafkaHandlers
{
    public interface IKafkaProducer<TMessage>
    {
        Task ProduceAsync(TMessage message);
    }
}
