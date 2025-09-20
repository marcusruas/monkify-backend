using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Sessions.Entities;
using Serilog;

namespace Monkify.Domain.Sessions.Services
{
    public class SessionBetsTracker
    {
        private ConcurrentDictionary<Guid, ConcurrentBag<Bet>> _sessionBets = new();

        public void AddSession(Guid sessionId)
        {
            _sessionBets.GetOrAdd(sessionId, new ConcurrentBag<Bet>());
        }

        public void AddBet(Bet bet)
        {
            var bets = _sessionBets.GetOrAdd(bet.SessionId, new ConcurrentBag<Bet>());
            bets.Add(bet);
        }

        public bool SessionHasEnoughPlayers(Guid sessionId, int minimumNumberOfPlayers)
        {
            if (!_sessionBets.TryGetValue(sessionId, out var bets))
            {
                throw new ArgumentException($"Session {sessionId} not found");
            }

            var playerCount = bets.DistinctBy(x => x.Wallet).Count();
            return playerCount >= minimumNumberOfPlayers;
        }

        public void RemoveSession(Guid sessionId)
        {
            _sessionBets.TryRemove(sessionId, out _);
        }
    }
}
