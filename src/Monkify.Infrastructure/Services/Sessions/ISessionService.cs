using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Services.Sessions
{
    public interface ISessionService
    {
        Task UpdateSessionStatus(Session session, SessionStatus status, MonkifyTyper? monkey = null);
        Task UpdateBetStatus(IEnumerable<Bet> bets, BetStatus status);
        Task UpdateBetStatus(Bet bet, BetStatus status);
        void CloseOpenSessions();
    }
}
