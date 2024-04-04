using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.ValueObjects
{
    public enum SessionStatus
    {
        [Description("Session was created and is waiting for bets. This is the initial Step.")]
        WaitingBets,
        [Description("Session could not be started due to the number of players.")]
        NotEnoughPlayersToStart,
        [Description("Session has started, there was enough players.")]
        Started,
        [Description("Session has ended.")]
        Ended,
        [Description("There was an error processing the session, so all players need to be refunded.")]
        NeedsRefund,
        [Description("The winners on this session are getting paid.")]
        PaymentInProgress,
        [Description("The winners on this session got paid.")]
        PaymentCompleted
            
    }
}
