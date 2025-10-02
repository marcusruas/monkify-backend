using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.UnitTests.Shared
{
    public record SessionMetricsResult(TimeSpan Duration, int NumberOfBatches)
    {
        public double CharactersPerSecond = Math.Round((NumberOfBatches * SessionServiceTestFactory.TERMINAL_BATCH_LIMIT_FOR_TESTS) / Duration.TotalSeconds);

        public override string ToString()
        {
            return $"Duration: {Duration}, Number of Characters Typed: {NumberOfBatches} at an average of {CharactersPerSecond} char/s";
        }
    }
}
