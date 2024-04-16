using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Services;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.Domain.Sessions
{
    public class BetValidatorTests
    {
        [Fact]
        public void Constructor_NoBets_ShouldThrowException()
        {
            var session = new Session();
            var settings = new TokenSettings();

            var exception = Should.Throw<ArgumentException>(() => new BetValidator(session, settings));
            exception.Message.ShouldBe("Session has no bets.");
        }

        [Fact]
        public void Constructor_NoWinner_ShouldThrowException()
        {
            var session = new Session();
            session.Bets = new List<Bet>()
            {
                new () { Won = false, Amount = 4 },
                new () { Won = false, Amount = 4 },
                new () { Won = false, Amount = 4 },
            };
            var settings = new TokenSettings();

            var exception = Should.Throw<ArgumentException>(() => new BetValidator(session, settings));
            exception.Message.ShouldBe("There are no winners in this session. Transfers cannot be made");
        }

        [Fact]
        public void Constructor_SetPotAmount_ShouldCalculateCorrectly()
        {
            var session = new Session();
            session.Bets = new List<Bet>()
            {
                new () { Won = true, Amount = 4 },
                new () { Won = false, Amount = 4 },
                new () { Won = false, Amount = 4 },
            };
            var settings = new TokenSettings();
            settings.CommisionPercentage = 0.5m;

            var validator = new BetValidator(session, settings);
            validator.PotAmount.ShouldBe(6);
        }

        [Fact]
        public void Constructor_CalculateRewardForBet_ShouldCalculateCorrectly()
        {
            var session = new Session();
            session.Bets = new List<Bet>()
            {
                new () { Won = true, Amount = 4 },
                new () { Won = false, Amount = 4 },
                new () { Won = false, Amount = 4 },
            };
            var settings = new TokenSettings();
            settings.CommisionPercentage = 0.1m;
            settings.Decimals = 5;

            var winner = new Bet() { Won = true, Amount = 4 };
            var validator = new BetValidator(session, settings);
            var result = validator.CalculateRewardForBet(winner);

            result.Value.ShouldBe(6.8m);
        }

        [Fact]
        public void Constructor_CalculateRewardForNonWinnerBet_ShouldThrowException()
        {
            var session = new Session();
            session.Bets = new List<Bet>()
            {
                new () { Won = true, Amount = 4 },
                new () { Won = false, Amount = 4 },
                new () { Won = false, Amount = 4 },
            };
            var settings = new TokenSettings();
            settings.CommisionPercentage = 0.5m;

            var bet = new Bet()
            {
                Won = false,
                Amount = 2
            };

            var validator = new BetValidator(session, settings);

            var exception = Should.Throw<ArgumentException>(() => validator.CalculateRewardForBet(bet));
            exception.Message.ShouldBe("Bet has not won, therefore cannot receive a reward for this session.");
        }
    }
}
