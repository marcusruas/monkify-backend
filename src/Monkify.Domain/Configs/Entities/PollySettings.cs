using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Configs.Entities
{
    [ExcludeFromCodeCoverage]
    public class PollySettings
    {
        public int LatestBlockshashRetryCount { get; set; }
        public int GetTransactionRetryCount { get; set; }
    }
}
