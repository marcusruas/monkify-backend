using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.UnitTests.Shared
{
    public record SessionMetricsResult(TimeSpan Duration, int NumberOfBatches)
    {
        public int CharactersTyped = NumberOfBatches * SessionServiceTestFactory.TERMINAL_BATCH_LIMIT_FOR_TESTS;
        public double GetCharactersPerSecond() => Math.Round(CharactersTyped / Duration.TotalSeconds);

        public override string ToString()
        {
            return $"Duration: {Duration}, Number of Characters Typed: {CharactersTyped} at an average of {GetCharactersPerSecond()} char/s";
        }
    }
}
