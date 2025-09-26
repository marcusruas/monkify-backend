using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Services;
using Shouldly;

namespace Monkify.Tests.UnitTests.Domain.Sessions
{
    public class SessionBetsTrackerTests
    {
        [Fact]
        public async Task AddSession_NewSession_ShouldInitializeTracking()
        {
            var tracker = new SessionBetsTracker();
            var sessionId = Guid.NewGuid();

            tracker.AddSession(sessionId);

            var hasEnough = await tracker.SessionHasEnoughPlayersAsync(sessionId, 1);
            hasEnough.ShouldBeFalse();
        }

        [Fact]
        public async Task AddBetAsync_MultipleUniqueBets_ShouldTrackAndCountPlayers()
        {
            var tracker = new SessionBetsTracker();
            var sessionId = Guid.NewGuid();

            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var bet = new Bet(sessionId,
                    Guid.NewGuid().ToString("N"),
                    Guid.NewGuid().ToString("N"),
                    $"wallet-{i}",
                    $"choice-{i}",
                    1);
                tasks.Add(tracker.AddBetAsync(bet));
            }

            await Task.WhenAll(tasks);

            (await tracker.SessionHasEnoughPlayersAsync(sessionId, 10)).ShouldBeTrue();
            (await tracker.SessionHasEnoughPlayersAsync(sessionId, 11)).ShouldBeFalse();
        }

        [Fact]
        public async Task SessionHasEnoughPlayersAsync_SessionDoesNotExist_ShouldThrowArgumentException()
        {
            var tracker = new SessionBetsTracker();
            var sessionId = Guid.NewGuid();

            var ex = await Should.ThrowAsync<ArgumentException>(async () =>
                await tracker.SessionHasEnoughPlayersAsync(sessionId, 1));

            ex.Message.ShouldBe($"Session {sessionId} not found");
        }

        [Fact]
        public async Task RemoveSessionAsync_SessionExists_ShouldRemoveSessionState()
        {
            var tracker = new SessionBetsTracker();
            var sessionId = Guid.NewGuid();

            await tracker.AddBetAsync(new Bet(sessionId,
                Guid.NewGuid().ToString("N"),
                Guid.NewGuid().ToString("N"),
                "wallet-1",
                "choice-1",
                1));

            await tracker.RemoveSessionAsync(sessionId);

            var ex = await Should.ThrowAsync<ArgumentException>(async () =>
                await tracker.SessionHasEnoughPlayersAsync(sessionId, 1));

            ex.Message.ShouldBe($"Session {sessionId} not found");
        }

        [Fact]
        public async Task RemoveSessionAsync_SessionDoesNotExist_ShouldDoNothing()
        {
            var tracker = new SessionBetsTracker();

            await tracker.RemoveSessionAsync(Guid.NewGuid());

            true.ShouldBeTrue();
        }
    }
}
