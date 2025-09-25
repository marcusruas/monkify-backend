using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Sessions.Entities;
using Serilog;

namespace Monkify.Domain.Sessions.Services
{
    public class SessionBetsTracker
    {
        private readonly ConcurrentDictionary<Guid, ConcurrentBag<Bet>> _sessionBets = new();
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _sessionSemaphores = new();

        public void AddSession(Guid sessionId)
        {
            _sessionBets.GetOrAdd(sessionId, new ConcurrentBag<Bet>());
            _sessionSemaphores.GetOrAdd(sessionId, new SemaphoreSlim(1, 1));
        }

        public async Task AddBetAsync(Bet bet, CancellationToken cancellationToken = default)
        {
            var semaphore = _sessionSemaphores.GetOrAdd(bet.SessionId, new SemaphoreSlim(1, 1));
            
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var bets = _sessionBets.GetOrAdd(bet.SessionId, new ConcurrentBag<Bet>());
                bets.Add(bet);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<bool> SessionHasEnoughPlayersAsync(Guid sessionId, int minimumNumberOfPlayers, CancellationToken cancellationToken = default)
        {
            if (!_sessionSemaphores.TryGetValue(sessionId, out var semaphore))
            {
                throw new ArgumentException($"Session {sessionId} not found");
            }

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!_sessionBets.TryGetValue(sessionId, out var bets))
                {
                    throw new ArgumentException($"Session {sessionId} not found");
                }

                var playerCount = bets.DistinctBy(x => x.Choice).DistinctBy(x => x.Wallet).Count();
                return playerCount >= minimumNumberOfPlayers;
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task RemoveSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            if (_sessionSemaphores.TryRemove(sessionId, out var semaphore))
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    _sessionBets.TryRemove(sessionId, out _);
                }
                finally
                {
                    semaphore.Release();
                    semaphore.Dispose();
                }
            }
        }
    }
}
