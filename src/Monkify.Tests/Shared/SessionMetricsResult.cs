using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Shared
{
    public record SessionMetricsResult(TimeSpan Duration, int NumberOfBatches)
    {
        public override string ToString()
        {
            return $"Duration: {Duration}, Number of Batches: {NumberOfBatches}";
        }
    }
}
