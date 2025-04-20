using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Policy;
using System.Text;
using Bogus;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Background.Hubs;
using Monkify.Infrastructure.Services.Sessions;
using Monkify.Tests.Shared;
using Moq;
using Shouldly;
using Xunit.Abstractions;

namespace Monkify.Tests.Domain.Sessions
{
    public class LetterSessionTests : BaseSessionMetricsTestsClass
    {
        public LetterSessionTests(ITestOutputHelper console) : base(console) { }

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
        public async Task Session_WithFourLetters_ShouldEventuallySelectWinner(int betsPerSession)
        {
            var parameters = new SessionMetricsTestParameters()
            {
                BetsPerGame = betsPerSession,
                CharacterType = SessionCharacterType.Letters,
                AcceptsDuplicateCharacters = true,
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
        public async Task Session_WithFiveLetters_ShouldEventuallySelectWinner(int betsPerSession)
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
    }
}
