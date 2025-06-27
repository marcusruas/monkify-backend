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
    public class ProductionSessionParametersTests : BaseSessionMetricsTestsClass
    {
        public ProductionSessionParametersTests(ITestOutputHelper console) : base(console)
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
        public async Task Session_WordLength5_ShouldRunFast(int betsPerSession)
        {
            var parameters = new SessionMetricsTestParameters()
            {
                BetsPerGame = betsPerSession,
                CharacterType = SessionCharacterType.Letters,
                AcceptsDuplicateCharacters = true,
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
        public async Task Session_WordLength6_ShouldRunFast(int betsPerSession)
        {
            var parameters = new SessionMetricsTestParameters()
            {
                BetsPerGame = betsPerSession,
                CharacterType = SessionCharacterType.Letters,
                AcceptsDuplicateCharacters = false,
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
        public async Task Session_NumberSequence6_ShouldEventuallySelectWinner(int betsPerSession)
        {
            var parameters = new SessionMetricsTestParameters()
            {
                BetsPerGame = betsPerSession,
                CharacterType = SessionCharacterType.Number,
                AcceptsDuplicateCharacters = true,
                WordLength = 6,
                Charset = "0123456789"
            };

            var sessionResults = await RunMultipleSessions(parameters);
            ValidateSessionRuns(sessionResults);
        }

        [Fact]
        public async Task Session_HorseRace_ShouldEventuallySelectWinner()
        {
            var parameters = new SessionMetricsTestParameters()
            {
                CharacterType = SessionCharacterType.Letters,
                AcceptsDuplicateCharacters = false,
                WordLength = 7,
            };

            parameters.PresetChoices.Add("muster");
            parameters.PresetChoices.Add("calibe");
            parameters.PresetChoices.Add("dogmas");
            parameters.PresetChoices.Add("rubfas");
            parameters.PresetChoices.Add("toncad");
            parameters.PresetChoices.Add("hermai");
            parameters.PresetChoices.Add("sunlef");
            parameters.PresetChoices.Add("bigfar");

            var sessionResults = await RunMultipleSessions(parameters);

            ValidateSessionRuns(sessionResults);
        }
    }
}
