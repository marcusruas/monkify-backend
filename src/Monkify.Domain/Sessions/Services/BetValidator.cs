using MediatR;
using Monkify.Common.Extensions;
using Monkify.Common.Messaging;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Services
{
    public class BetValidator
    {
        public BetValidator(Session session, TokenSettings settings)
        {
            _settings = settings;

            Winners = session.Bets.Where(x => x.Won);
            SetPotAmount(session);
        }

        public readonly IEnumerable<Bet> Winners;

        private readonly TokenSettings _settings;
        private decimal PotAmount;

        public static BetValidationResult ChoiceIsValidForSession(Bet bet, Session session)
        {
            var parameters = session.Parameters;

            if (string.IsNullOrEmpty(bet.Choice) && bet.Choice.Length != parameters.ChoiceRequiredLength)
                return BetValidationResult.WrontChoiceLength;

            if (!parameters.AcceptDuplicatedCharacters && bet.Choice.ContainsDuplicateCharacters())
                return BetValidationResult.UnacceptedDuplicateCharacters;

            if (bet.Amount != session.Parameters.RequiredAmount)
                return BetValidationResult.InvalidAmount;

            if (parameters.SessionCharacterType.ContainsAttribute<DescriptionAttribute>())
            {
                var acceptedCharacters = parameters.SessionCharacterType.StringValueOf().ToArray();
                if (!bet.Choice.All(character => acceptedCharacters.Contains(character)))
                    return BetValidationResult.InvalidCharacters;
            }

            return BetValidationResult.Valid;
        }

        private void SetPotAmount(Session session)
        {
            PotAmount = session.Bets.Sum(x => x.Amount);
            PotAmount *= (1 - _settings.CommisionPercentage);
        }

        public BetRewardResult CalculateRewardForBet(Bet winner)
        {
            decimal winnerReward = (PotAmount / Winners.Count()) - winner.Amount;
            winnerReward = Math.Round(winnerReward, _settings.Decimals, MidpointRounding.ToZero);
            ulong rewardInTokens = (ulong)(winnerReward * (decimal)Math.Pow(10, _settings.Decimals));

            return new BetRewardResult(winnerReward, rewardInTokens);
        }
    }
}
