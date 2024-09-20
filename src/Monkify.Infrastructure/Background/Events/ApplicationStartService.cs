using Microsoft.EntityFrameworkCore;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using Monkify.Infrastructure.Context;
using Monkify.Infrastructure.Services.Sessions;
using Serilog;
using Serilog.Core;

namespace Monkify.Infrastructure.Background.Events
{
    public class ApplicationStartService
    {
        private readonly MonkifyDbContext _context;
        private readonly ISessionService _sessionService;

        public ApplicationStartService(MonkifyDbContext context, ISessionService sessionService)
        {
            _context = context;
            _sessionService = sessionService;
        }

        public void CloseOpenSessions()
        {
            try
            {
                var activeSessions = _context.Sessions
                .Include(x => x.Bets)
                .Where(x => Session.SessionInProgressStatus.Contains(x.Status))
                .AsNoTracking().ToList();

                if (activeSessions.IsNullOrEmpty())
                    return;

                foreach (var session in activeSessions)
                {
                    _sessionService.UpdateSessionStatus(session, SessionStatus.SessionEndedAbruptely).Wait();

                    foreach (var bet in session.Bets)
                    {
                        _sessionService.UpdateBetStatus(bet, BetStatus.NeedsManualAnalysis).Wait();
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Failed to shutdown the application properly.");
            }
            
        }
    }
}
