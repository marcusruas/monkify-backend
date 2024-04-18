using Monkify.Common.Resources;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Shouldly;
using Solnet.Wallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Domain.Sessions
{
    public class BetDomainServiceTests
    {
        [Fact]
        public void Constructor_NoBets_ShouldThrowException()
        {
            var session = new Session();
            var settings = new TokenSettings();

            var exception = Should.Throw<ArgumentException>(() => new BetDomainService(session, settings));
            exception.Message.ShouldBe(ErrorMessages.SessionWithNoBets);
        }

        [Fact]
        public void Constructor_NoWinner_ShouldThrowException()
        {
            var session = new Session();
            session.Bets = new List<Bet>()
            {
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
            };
            var settings = new TokenSettings();

            var exception = Should.Throw<ArgumentException>(() => new BetDomainService(session, settings));
            exception.Message.ShouldBe(ErrorMessages.SessionWithoutWinners);
        }

        [Fact]
        public void Constructor_SetPotAmount_ShouldCalculateCorrectly()
        {
            var session = new Session();
            session.Bets = new List<Bet>()
            {
                new () { Status = BetStatus.NeedsRewarding, Amount = 4 },
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
            };
            var settings = new TokenSettings();
            settings.CommisionPercentage = 0.5m;

            var validator = new BetDomainService(session, settings);
            validator.PotAmount.ShouldBe(6);
        }

        [Fact]
        public void CalculateRewardForBet_SessionWithBets_ShouldCalculateCorrectly()
        {
            var winnerBet = new Bet() { Status = BetStatus.NeedsRewarding, Amount = 4 };
            var session = new Session();

            session.Bets = new List<Bet>()
            {
                winnerBet,
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
            };
            var settings = new TokenSettings();
            settings.CommisionPercentage = 0.1m;
            settings.Decimals = 5;

            var validator = new BetDomainService(session, settings);
            var result = validator.CalculateRewardForBet(winnerBet);

            result.Value.ShouldBe(6.8m);
        }

        [Fact]
        public void CalculateRewardForBet_BetWithCredits_ShouldCalculateCorrectly()
        {
            var winnerBet = new Bet() { Status = BetStatus.NeedsRewarding, Amount = 4 };
            winnerBet.TransactionLogs = new List<TransactionLog>() { new() { Amount = 3 } };
            var session = new Session();

            session.Bets = new List<Bet>()
            {
                winnerBet,
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
            };
            var settings = new TokenSettings();
            settings.CommisionPercentage = 0.1m;
            settings.Decimals = 5;

            var validator = new BetDomainService(session, settings);
            var result = validator.CalculateRewardForBet(winnerBet);

            result.Value.ShouldBe(3.8m);
        }

        [Fact]
        public void CalculateRewardForBet_SessionWithMultipleWinners_ShouldCalculateCorrectly()
        {
            var winnerBet = new Bet() { Status = BetStatus.NeedsRewarding, Amount = 4 };
            var session = new Session();

            session.Bets = new List<Bet>()
            {
                winnerBet,
                new () { Status = BetStatus.NeedsRewarding, Amount = 7.33654m },
                new () { Status = BetStatus.NeedsRewarding, Amount = 7.33654m },
                new () { Status = BetStatus.NotApplicable, Amount = 7.33654m },
                new () { Status = BetStatus.NotApplicable, Amount = 7.33654m },
                new () { Status = BetStatus.NotApplicable, Amount = 7.33654m },
            };
            var settings = new TokenSettings();
            settings.CommisionPercentage = 0.1m;
            settings.Decimals = 5;

            var validator = new BetDomainService(session, settings);
            var result = validator.CalculateRewardForBet(winnerBet);

            result.Value.ShouldBe(8.20481M);
        }

        [Fact]
        public void CalculateRewardForBet_NonWinnerBet_ShouldThrowException()
        {
            var loserBet = new Bet() { Status = BetStatus.NotApplicable, Amount = 4 };
            var session = new Session();
            session.Bets = new List<Bet>()
            {
                new () { Status = BetStatus.NeedsRewarding, Amount = 4 },
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
                loserBet,
            };
            var settings = new TokenSettings();
            settings.CommisionPercentage = 0.5m;
            var validator = new BetDomainService(session, settings);

            var result = validator.CalculateRewardForBet(loserBet);
            result.Value.ShouldBe(0);
            result.ErrorMessage.ShouldBe(ErrorMessages.BetCannotReceiveReward);
        }

        [Fact]
        public void CalculateRewardForBet_BiggerRewardThanPot_ShouldThrowException()
        {
            var winnerBet = new Bet() { Status = BetStatus.NeedsRewarding, Amount = 20 };
            var session = new Session();

            session.Bets = new List<Bet>()
            {
                new () { Status = BetStatus.NeedsRewarding, Amount = 4 },
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
            };
            var settings = new TokenSettings();
            settings.CommisionPercentage = 0.1m;
            settings.Decimals = 5;

            var validator = new BetDomainService(session, settings);

            var result = validator.CalculateRewardForBet(winnerBet);
            result.Value.ShouldBe(0);
            result.ErrorMessage.ShouldBe(ErrorMessages.BetRewardBiggerThanThePot);
        }

        [Fact]
        public void CalculateRewardForBet_AlreadyRewardedBet_ShouldCalculateCorrectly()
        {
            var winnerBet = new Bet() { Status = BetStatus.NeedsRewarding, Amount = 4 };
            winnerBet.TransactionLogs = new List<TransactionLog>() { new () { Amount = 6.8m } };
            var session = new Session();

            session.Bets = new List<Bet>()
            {
                winnerBet,
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
                new () { Status = BetStatus.NotApplicable, Amount = 4 },
            };
            var settings = new TokenSettings();
            settings.CommisionPercentage = 0.1m;
            settings.Decimals = 5;

            var validator = new BetDomainService(session, settings);
            var result = validator.CalculateRewardForBet(winnerBet);

            result.Value.ShouldBe(0);
            result.ErrorMessage.ShouldBe(ErrorMessages.BetHasAlreadyBeenRewarded);
        }

        [Fact]
        public void CalculateRefundForBet_ValidBet_ShouldCalculateCorrectly()
        {
            var bet = new Bet() { Status = BetStatus.NeedsRefunding, Amount = 6 };

            var settings = new TokenSettings();
            settings.CommisionPercentage = 0.1m;
            settings.Decimals = 5;

            var refund = BetDomainService.CalculateRefundForBet(settings, bet);
            refund.Value.ShouldBe(6);
        }

        [Fact]
        public void CalculateRefundForBet_BetWithCredits_ShouldCalculateCorrectly()
        {
            var log = new TransactionLog() { Amount = 4 };
            var bet = new Bet() { Status = BetStatus.NeedsRefunding, Amount = 6, TransactionLogs = new List<TransactionLog>() { log } };

            var settings = new TokenSettings();
            settings.CommisionPercentage = 0.1m;
            settings.Decimals = 5;

            var refund = BetDomainService.CalculateRefundForBet(settings, bet);
            refund.Value.ShouldBe(2);
        }

        [Theory]
        [InlineData(6, 0)]
        [InlineData(7, -1)]
        public void CalculateRefundForBet_RefundedBet_ShouldCalculateCorrectly(int amount, decimal value)
        {
            var log = new TransactionLog() { Amount = amount };
            var bet = new Bet() { Status = BetStatus.NeedsRefunding, Amount = 6, TransactionLogs = new List<TransactionLog>() { log } };

            var settings = new TokenSettings();
            settings.Decimals = 5;

            var refund = BetDomainService.CalculateRefundForBet(settings, bet);
            refund.Value.ShouldBe(value);
            refund.ValueInTokens.ShouldBe(ulong.MinValue);
        }

        [Theory]
        [InlineData("macaco", "MACACO")]
        [InlineData("abcdef", "ghijkl")]
        [InlineData(" macac", "macaco")]
        public void ChoiceIsValidForSession_ChoiceNotInPresetChoices_ShouldReturnInvalidChoice(string choice, string presetChoice)
        {
            var session = new Session
            {
                Parameters = new SessionParameters
                {
                    PresetChoices = new List<PresetChoice> { new PresetChoice { Choice = presetChoice } },
                    ChoiceRequiredLength = 6,
                    AcceptDuplicatedCharacters = true
                }
            };
            var bet = new Bet { Choice = choice };

            var result = BetDomainService.ChoiceIsValidForSession(bet, session);

            result.ShouldBe(BetValidationResult.InvalidChoice);
        }

        [Theory]
        [InlineData(SessionCharacterType.LowerCaseLetter, "macacO")]
        [InlineData(SessionCharacterType.UpperCaseLetter, "MACACo")]
        [InlineData(SessionCharacterType.Number, "12345a")]
        public void ChoiceIsValidForSession_ChoiceWithInvalidCharacters_ShouldReturnInvalidCharacters(SessionCharacterType characterType, string choice)
        {
            var session = new Session
            {
                Parameters = new SessionParameters
                {
                    SessionCharacterType = characterType,
                    ChoiceRequiredLength = 6,
                    AcceptDuplicatedCharacters = true
                }
            };
            var bet = new Bet { Choice = choice };

            var result = BetDomainService.ChoiceIsValidForSession(bet, session);

            result.ShouldBe(BetValidationResult.InvalidCharacters);
        }

        [Theory]
        [InlineData("abc", 4)]
        [InlineData("abcde", 4)]
        [InlineData("", 1)]
        public void ChoiceIsValidForSession_ChoiceWithInvalidLength_ShouldReturnWrongChoiceLength(string choice, int choiceRequiredLength)
        {
            var session = new Session
            {
                Parameters = new SessionParameters
                {
                    SessionCharacterType = SessionCharacterType.LowerCaseLetter,
                    ChoiceRequiredLength = choiceRequiredLength
                }
            };
            var bet = new Bet { Choice = choice };

            var result = BetDomainService.ChoiceIsValidForSession(bet, session);

            result.ShouldBe(BetValidationResult.WrongChoiceLength);
        }

        [Theory]
        [InlineData("aaaaaaa", false)]
        [InlineData("macaaco", false)]
        [InlineData("abahtkm", false)]
        public void ChoiceIsValidForSession_ChoiceWithDuplicatesNotAllowed_ShouldReturnUnacceptedDuplicateCharacters(string choice, bool acceptDuplicates)
        {
            var session = new Session
            {
                Parameters = new SessionParameters
                {
                    SessionCharacterType = SessionCharacterType.LowerCaseLetter,
                    AcceptDuplicatedCharacters = acceptDuplicates,
                    ChoiceRequiredLength = 7
                }
            };
            var bet = new Bet { Choice = choice };

            var result = BetDomainService.ChoiceIsValidForSession(bet, session);

            result.ShouldBe(BetValidationResult.UnacceptedDuplicateCharacters);
        }

        [Theory]
        [InlineData(50, 100)]
        [InlineData(150, 100)]
        [InlineData(200, 100)]
        public void ChoiceIsValidForSession_ChoiceWithInvalidAmount_ShouldReturnInvalidAmount(decimal betAmount, decimal requiredAmount)
        {
            var session = new Session
            {
                Parameters = new SessionParameters
                {
                    RequiredAmount = requiredAmount,
                    ChoiceRequiredLength = 3
                }
            };
            var bet = new Bet { Choice = "abc", Amount = betAmount };

            var result = BetDomainService.ChoiceIsValidForSession(bet, session);

            result.ShouldBe(BetValidationResult.InvalidAmount);
        }

        [Fact]
        public void ChoiceIsValidForSession_ValidChoice_ShouldReturnValid()
        {
            var session = new Session
            {
                Parameters = new SessionParameters
                {
                    SessionCharacterType = SessionCharacterType.LowerCaseLetter,
                    RequiredAmount = 2,
                    ChoiceRequiredLength = 4,
                    AcceptDuplicatedCharacters = true
                }
            };
            var bet = new Bet { Choice = "abcd", Amount = 2 };

            var result = BetDomainService.ChoiceIsValidForSession(bet, session);

            result.ShouldBe(BetValidationResult.Valid);
        }

        [Fact]
        public void ChoiceIsValidForSession_ValidChoiceWithPresetChoice_ShouldReturnValid()
        {
            var session = new Session
            {
                Parameters = new SessionParameters
                {

                    SessionCharacterType = SessionCharacterType.LowerCaseLetter,
                    PresetChoices = new List<PresetChoice>() { new() { Choice = "abcd" } },
                    RequiredAmount = 2,
                    ChoiceRequiredLength = 4,
                    AcceptDuplicatedCharacters = true
                }
            };
            var bet = new Bet { Choice = "abcd", Amount = 2 };

            var result = BetDomainService.ChoiceIsValidForSession(bet, session);

            result.ShouldBe(BetValidationResult.Valid);
        }
    }
}
