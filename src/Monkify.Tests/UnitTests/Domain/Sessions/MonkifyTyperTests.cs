using Bogus;
using Monkify.Common.Resources;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.Services;
using Monkify.Domain.Sessions.ValueObjects;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Tests.UnitTests.Domain.Sessions
{
    public class MonkifyTyperTests
    {
        private readonly Faker Faker = new Faker();
        private const int DefaultSessionAverageDuration = 15;

        [Fact]
        public void Constructor_NoBets_ShouldThrowException()
        {
            var session = new Session();

            var exception = Should.Throw<ArgumentException>(() => new MonkifyTyper(session));
            exception.Message.ShouldBe(ErrorMessages.TyperStartedWithoutBets);
        }

        [Fact]
        public void Constructor_CorrectSessions_ShouldInstantiate()
        {
            var session = new Session();
            session.Parameters = new SessionParameters() { AllowedCharacters = SessionCharacterType.Letters };
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = Faker.Random.String2(4) });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = Faker.Random.String2(4) });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = Faker.Random.String2(4) });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = Faker.Random.String2(4) });

            MonkifyTyper? typer = null;

            Should.NotThrow(() => typer = new MonkifyTyper(session));

            typer.HasWinners.ShouldBeFalse();
            typer.NumberOfWinners.ShouldBe(0);
            typer.FirstChoiceTyped.ShouldBeNull();
            typer.Bets.Count.ShouldBe(4);
            typer.Bets.All(x => x.Value == 1).ShouldBeTrue();
            typer.QueueLength.ShouldBe(4);
            typer.CharactersOnTyper.Length.ShouldBe(26);
        }

        [Fact]
        public void Constructor_ParametersWithPresetChoices_ShouldSetBetsProperly()
        {
            string choiceA = "abcd";
            string choiceB = "defg";

            var presetChoices = new List<PresetChoice>() { new PresetChoice(choiceA), new PresetChoice(choiceB) };
            var session = new Session();
            session.Parameters = new SessionParameters() { AllowedCharacters = SessionCharacterType.Letters, PresetChoices = presetChoices };
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choiceA });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choiceA });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choiceB });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choiceB });

            MonkifyTyper? typer = null;

            Should.NotThrow(() => typer = new MonkifyTyper(session));

            typer.HasWinners.ShouldBeFalse();
            typer.NumberOfWinners.ShouldBe(0);
            typer.FirstChoiceTyped.ShouldBeNull();
            typer.Bets.Count.ShouldBe(2);
            typer.Bets.All(x => x.Value == 2).ShouldBeTrue();
            typer.QueueLength.ShouldBe(4);
            typer.CharactersOnTyper.Length.ShouldBe(7);
        }

        [Fact]
        public void Constructor_ParametersWithoutPresetChoices_ShouldSetBetsProperly()
        {
            string choiceA = "abcd";
            string choiceB = "defg";

            var session = new Session();
            session.Parameters = new SessionParameters() { AllowedCharacters = SessionCharacterType.Letters };
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choiceA });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choiceA });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choiceB });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choiceB });

            MonkifyTyper? typer = new MonkifyTyper(session);

            typer.HasWinners.ShouldBeFalse();
            typer.NumberOfWinners.ShouldBe(0);
            typer.FirstChoiceTyped.ShouldBeNull();
            typer.Bets.Count.ShouldBe(2);
            typer.Bets.All(x => x.Value == 2).ShouldBeTrue();
            typer.QueueLength.ShouldBe(4);
            typer.CharactersOnTyper.Length.ShouldBe(26);
        }

        [Fact]
        public void Constructor_TyperSetByPlayerChoices_ShouldSetBetsProperly()
        {
            string choiceA = "abcd";
            string choiceB = "defg";

            var session = new Session();
            session.Parameters = new SessionParameters() { AllowedCharacters = SessionCharacterType.Letters };
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choiceA });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choiceA });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choiceB });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choiceB });

            MonkifyTyper? typer = new MonkifyTyper(session);

            typer.HasWinners.ShouldBeFalse();
            typer.NumberOfWinners.ShouldBe(0);
            typer.FirstChoiceTyped.ShouldBeNull();
            typer.Bets.Count.ShouldBe(2);
            typer.Bets.All(x => x.Value == 2).ShouldBeTrue();
            typer.QueueLength.ShouldBe(4);
            typer.CharactersOnTyper.Length.ShouldBe(26);
        }

        [Fact]
        public void Constructor_GenerateNextCharacter_ShouldEventuallyGenerateFirstAndFinalCharacter()
        {
            var watch = new Stopwatch();
            var session = new Session();
            session.Parameters = new SessionParameters() { AllowedCharacters = SessionCharacterType.Letters };
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = Faker.Random.String2(4) });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = Faker.Random.String2(4) });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = Faker.Random.String2(4) });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = Faker.Random.String2(4) });

            var typer = new MonkifyTyper(session);

            bool firstCharacterTyped = false;
            bool lastCharacterTyped = false;

            watch.Start();
            while ((!firstCharacterTyped || !lastCharacterTyped) && watch.Elapsed.TotalSeconds < 3) //arbitrary number of seconds
            {
                var character = typer.GenerateNextCharacter(CancellationToken.None).Result;

                if (!firstCharacterTyped)
                    firstCharacterTyped = character == typer.CharactersOnTyper.First();
                if (!lastCharacterTyped)
                    lastCharacterTyped = character == typer.CharactersOnTyper.Last();
            }
            watch.Stop();

            firstCharacterTyped.ShouldBeTrue();
            lastCharacterTyped.ShouldBeTrue();
            typer.HasWinners.ShouldBeFalse();
            typer.NumberOfWinners.ShouldBe(0);
            typer.FirstChoiceTyped.ShouldBeNull();
        }
    }
}
