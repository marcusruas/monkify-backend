using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Tests.UnitTests.Shared;
using Xunit.Abstractions;

namespace Monkify.Tests.UnitTests.Domain
{
    public class SessionRunLettersTests : BaseSessionMetricsTestsClass
    {
        public SessionRunLettersTests(ITestOutputHelper console) : base(console)
        {
        }

        [Theory]
        [InlineData("02")]
        [InlineData("04")]
        [InlineData("06")]
        [InlineData("08")]
        [InlineData("10")]
        [InlineData("20")]
        [InlineData("30")]
        [InlineData("40")]
        [InlineData("50")]
        [InlineData("60")]
        [InlineData("70")]
        [InlineData("80")]
        [InlineData("90")]
        [InlineData("99")]
        public async Task Session_FourLetterSession_ShouldEventuallySelectWinner(string betsPerSession)
        {
            var parameters = new SessionMetricsTestParameters()
            {
                BetsPerGame = int.Parse(betsPerSession),
                CharacterType = SessionCharacterType.Letters,
                WordLength = 4,
            };

            var sessionResults = await RunMultipleSessions(parameters);

            ValidateSessionRuns(sessionResults);
        }

        [Theory]
        [InlineData("02")]
        [InlineData("04")]
        [InlineData("06")]
        [InlineData("08")]
        [InlineData("10")]
        [InlineData("20")]
        [InlineData("30")]
        [InlineData("40")]
        [InlineData("50")]
        [InlineData("60")]
        [InlineData("70")]
        [InlineData("80")]
        [InlineData("90")]
        [InlineData("99")]
        public async Task Session_FiveLetterSession_ShouldEventuallySelectWinner(string betsPerSession)
        {
            var parameters = new SessionMetricsTestParameters()
            {
                BetsPerGame = int.Parse(betsPerSession),
                CharacterType = SessionCharacterType.Letters,
                WordLength = 5,
            };

            var sessionResults = await RunMultipleSessions(parameters);

            ValidateSessionRuns(sessionResults);
        }

        [Theory]
        [InlineData("02")]
        [InlineData("04")]
        [InlineData("06")]
        [InlineData("08")]
        [InlineData("10")]
        [InlineData("20")]
        [InlineData("30")]
        [InlineData("40")]
        [InlineData("50")]
        [InlineData("60")]
        [InlineData("70")]
        [InlineData("80")]
        [InlineData("90")]
        [InlineData("99")]
        public async Task Session_SixLetterSession_ShouldEventuallySelectWinner(string betsPerSession)
        {
            var parameters = new SessionMetricsTestParameters()
            {
                BetsPerGame = int.Parse(betsPerSession),
                CharacterType = SessionCharacterType.Letters,
                WordLength = 6,
            };

            var sessionResults = await RunMultipleSessions(parameters);

            ValidateSessionRuns(sessionResults);
        }
    }
}
