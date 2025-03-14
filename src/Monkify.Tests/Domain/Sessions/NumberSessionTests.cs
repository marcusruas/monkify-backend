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
    public class NumberSessionTests : BaseSessionMetricsTestsClass
    {
        public NumberSessionTests(ITestOutputHelper console) : base(console) { }

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
        public async Task Session_WithFourNumbers_ShouldEventuallySelectWinner(int betsPerSession)
        {
            var parameters = new SessionMetricsTestParameters()
            {
                BetsPerGame = betsPerSession,
                CharacterType = SessionCharacterType.Number,
                AcceptsDuplicateCharacters = true,
                WordLength = 4,
                Charset = "0123456789"
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
        public async Task Session_WithFiveNumbers_ShouldEventuallySelectWinner(int betsPerSession)
        {
            var parameters = new SessionMetricsTestParameters()
            {
                BetsPerGame = betsPerSession,
                CharacterType = SessionCharacterType.Number,
                AcceptsDuplicateCharacters = true,
                WordLength = 5,
                Charset = "0123456789"
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
        public async Task Session_WithSixNumbers_ShouldEventuallySelectWinner(int betsPerSession)
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
        public async Task Session_WithSevenNumbers_ShouldEventuallySelectWinner(int betsPerSession)
        {
            var parameters = new SessionMetricsTestParameters()
            {
                BetsPerGame = betsPerSession,
                CharacterType = SessionCharacterType.Number,
                AcceptsDuplicateCharacters = true,
                WordLength = 7,
                Charset = "0123456789"
            };

            var sessionResults = await RunMultipleSessions(parameters);

            ValidateSessionRuns(sessionResults);
        }
    }
}
