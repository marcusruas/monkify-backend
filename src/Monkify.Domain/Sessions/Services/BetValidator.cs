using MediatR;
using Microsoft.IdentityModel.Tokens;
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
using static System.Collections.Specialized.BitVector32;

namespace Monkify.Domain.Sessions.Services
{
    public class BetValidator
    {
        public BetValidator(Session session, TokenSettings settings)
        {
            _settings = settings;

            if (session.Bets.IsNullOrEmpty())
                throw new ArgumentException("Session has no bets.");

            Winners = session.Bets.Where(x => x.Won);

            if (Winners.IsNullOrEmpty())
                throw new ArgumentException("There are no winners in this session. Transfers cannot be made");

            SetPotAmount(session);
        }

        public BetValidator(TokenSettings settings)
        {
            _settings = settings;
        }

        public readonly IEnumerable<Bet> Winners;
        public decimal PotAmount { get; private set; } 

        private readonly TokenSettings _settings;

        public BetTransactionAmountResult CalculateRewardForBet(Bet winner)
        {
            if (!winner.Won)
                throw new ArgumentException("Bet has not won, therefore cannot receive a reward for this session.");

            decimal winnerReward = (PotAmount / Winners.Count()) - winner.Amount;

            if (winnerReward < 0)
                throw new ArgumentException("Bet reward cannot be bigger than the pot");

            winnerReward = Math.Round(winnerReward, _settings.Decimals, MidpointRounding.ToZero);
            ulong rewardInTokens = (ulong)(winnerReward * (decimal)Math.Pow(10, _settings.Decimals));

            return new BetTransactionAmountResult(winnerReward, rewardInTokens);
        }

        public BetTransactionAmountResult CalculateRefundForBet(Bet bet)
        {
            decimal credits = 0;

            if (!bet.Logs.IsNullOrEmpty())
                credits = bet.Logs.Sum(x => x.Amount);

            var refundValue = Math.Round(bet.Amount, _settings.Decimals, MidpointRounding.ToZero);
            refundValue -= credits;

            if (refundValue < 0)
                return new BetTransactionAmountResult(0, 0);

            var refundInTokens = (ulong)(refundValue * (decimal)Math.Pow(10, _settings.Decimals));

            return new BetTransactionAmountResult(refundValue, refundInTokens);
        }

        public static BetValidationResult ChoiceIsValidForSession(Bet bet, Session session)
        {
            var parameters = session.Parameters;

            if (!parameters.PresetChoices.IsNullOrEmpty() && !parameters.PresetChoices.Any(x => x.Choice == bet.Choice))
            {
                return BetValidationResult.InvalidChoice;
            }
            else if (parameters.SessionCharacterType.ContainsAttribute<DescriptionAttribute>())
            {
                var acceptedCharacters = parameters.SessionCharacterType.StringValueOf().ToArray();
                if (!bet.Choice.All(character => acceptedCharacters.Contains(character)))
                    return BetValidationResult.InvalidCharacters;
            }
            else if (string.IsNullOrEmpty(bet.Choice) || bet.Choice.Length != parameters.ChoiceRequiredLength)
            {
                return BetValidationResult.WrontChoiceLength;
            }

            if (!parameters.AcceptDuplicatedCharacters && bet.Choice.ContainsDuplicateCharacters())
                return BetValidationResult.UnacceptedDuplicateCharacters;

            if (bet.Amount != session.Parameters.RequiredAmount)
                return BetValidationResult.InvalidAmount;

            return BetValidationResult.Valid;
        }

        private void SetPotAmount(Session session)
        {
            PotAmount = session.Bets.Sum(x => x.Amount);
            PotAmount *= (1 - _settings.CommisionPercentage);
        }
    }
}
