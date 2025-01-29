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
        WaitingBets = 1,
        [Description("Session could not be started due to the number of players.")]
        NotEnoughPlayersToStart,
        [Description("The session has enough players and will start shortly.")]
        SessionStarting,
        [Description("Session is in progress, there was enough players.")]
        InProgress,
        [Description("Session has ended.")]
        Ended,
        [Description("The winners on this session are getting rewarded.")]
        RewardForWinnersInProgress,
        [Description("The winners on this session got rewarded.")]
        RewardForWinnersCompleted,
        [Description("An error occurred while processing rewards for this session..")]
        ErrorWhenProcessingRewards,
        [Description("An error occurred while processing the session so it was ended..")]
        SessionEndedAbruptely
    }
}
