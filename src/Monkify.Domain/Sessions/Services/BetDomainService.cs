using MediatR;
using Monkify.Common.Extensions;
using Monkify.Common.Messaging;
using Monkify.Common.Resources;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using System.ComponentModel;

namespace Monkify.Domain.Sessions.Services
{
    public class BetDomainService
    {
        public BetDomainService(Session session, TokenSettings settings)
        {
            _settings = settings;

            if (session.Bets.IsNullOrEmpty())
                throw new ArgumentException(ErrorMessages.SessionWithNoBets);

            Winners = session.Bets.Where(x => x.Status == BetStatus.NeedsRewarding);

            if (Winners.IsNullOrEmpty())
                throw new ArgumentException(ErrorMessages.SessionWithoutWinners);

            SetPotAmount(session);
        }

        public readonly IEnumerable<Bet> Winners;
        public decimal PotAmount { get; private set; } 

        private readonly TokenSettings _settings;

        public static BetValidationResult ChoiceIsValidForSession(Bet bet, Session session)
        {
            var parameters = session.Parameters;

            if (string.IsNullOrEmpty(bet.Choice) || bet.Choice.Length != parameters.ChoiceRequiredLength)
                return BetValidationResult.WrongChoiceLength;

            if (!parameters.AcceptDuplicatedCharacters && bet.Choice.ContainsDuplicateCharacters())
                return BetValidationResult.UnacceptedDuplicateCharacters;

            if (bet.Amount != session.Parameters.RequiredAmount)
                return BetValidationResult.InvalidAmount;

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

            return BetValidationResult.Valid;
        }

        public BetTransactionAmountResult CalculateRewardForBet(Bet bet)
        {
            if (bet.Status != BetStatus.NeedsRewarding)
                return new BetTransactionAmountResult(ErrorMessages.BetCannotReceiveReward);

            decimal winnerReward = PotAmount / Winners.Count();

            if (winnerReward < bet.Amount)
                return new BetTransactionAmountResult(ErrorMessages.BetRewardBiggerThanThePot);

            decimal credits = CalculateCreditsForBet(bet);
            winnerReward -= credits;

            if (winnerReward <= 0)
                return new BetTransactionAmountResult(ErrorMessages.BetHasAlreadyBeenRewarded);

            winnerReward = Math.Round(winnerReward, _settings.Decimals, MidpointRounding.ToZero);
            ulong rewardInTokens = (ulong)(winnerReward * (decimal)Math.Pow(10, _settings.Decimals));

            return new BetTransactionAmountResult(winnerReward, rewardInTokens);
        }

        public static BetTransactionAmountResult CalculateRefundForBet(TokenSettings settings, Bet bet)
        {
            if (bet.Status != BetStatus.NeedsRefunding)
                return new BetTransactionAmountResult(ErrorMessages.BetCannotReceiveRefund);

            var credits = CalculateCreditsForBet(bet);

            var refundValue = Math.Round(bet.Amount, settings.Decimals, MidpointRounding.ToZero);
            refundValue -= credits;

            if (refundValue <= 0)
                return new BetTransactionAmountResult(ErrorMessages.BetHasAlreadyBeenRefunded);

            var refundInTokens = (ulong)(refundValue * (decimal)Math.Pow(10, settings.Decimals));

            return new BetTransactionAmountResult(refundValue, refundInTokens);
        }

        private void SetPotAmount(Session session)
        {
            PotAmount = session.Bets.Sum(x => x.Amount);
            PotAmount *= (1 - _settings.CommisionPercentage);
        }

        private static decimal CalculateCreditsForBet(Bet bet)
        {
            decimal credits = 0;

            if (!bet.TransactionLogs.IsNullOrEmpty())
                credits = bet.TransactionLogs.Sum(x => x.Amount);

            return credits;
        }
    }
}
