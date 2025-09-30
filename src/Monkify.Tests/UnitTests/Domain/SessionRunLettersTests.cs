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
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(12)]
        [InlineData(24)]
        [InlineData(36)]
        [InlineData(48)]
        [InlineData(60)]
        [InlineData(72)]
        [InlineData(84)]
        [InlineData(96)]
        public async Task Session_FourLetterSession_ShouldEventuallySelectWinner(int betsPerSession)
        {
            var parameters = new SessionMetricsTestParameters()
            {
                BetsPerGame = betsPerSession,
                CharacterType = SessionCharacterType.Letters,
                WordLength = 4,
            };

            var sessionResults = await RunMultipleSessions(parameters);

            ValidateSessionRuns(sessionResults);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(12)]
        [InlineData(24)]
        [InlineData(36)]
        [InlineData(48)]
        [InlineData(60)]
        [InlineData(72)]
        [InlineData(84)]
        [InlineData(96)]
        public async Task Session_FiveLetterSession_ShouldEventuallySelectWinner(int betsPerSession)
        {
            var parameters = new SessionMetricsTestParameters()
            {
                BetsPerGame = betsPerSession,
                CharacterType = SessionCharacterType.Letters,
                WordLength = 5,
            };

            var sessionResults = await RunMultipleSessions(parameters);

            ValidateSessionRuns(sessionResults);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(12)]
        [InlineData(24)]
        [InlineData(36)]
        [InlineData(48)]
        [InlineData(60)]
        [InlineData(72)]
        [InlineData(84)]
        [InlineData(96)]
        public async Task Session_SixLetterSession_ShouldEventuallySelectWinner(int betsPerSession)
        {
            var parameters = new SessionMetricsTestParameters()
            {
                BetsPerGame = betsPerSession,
                CharacterType = SessionCharacterType.Letters,
                WordLength = 6,
            };

            var sessionResults = await RunMultipleSessions(parameters);

            ValidateSessionRuns(sessionResults);
        }
    }
}
