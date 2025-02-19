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

namespace Monkify.Tests.Domain.Sessions
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
            typer.CharactersOnTyper.Length.ShouldBe(26);
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
            session.Parameters = new SessionParameters() { AllowedCharacters = SessionCharacterType.Letters, PlayersDefineCharacters = true };
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
            typer.CharactersOnTyper.Length.ShouldBe(7);
            typer.CharactersOnTyper.SequenceEqual(new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g' }).ShouldBeTrue();
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
                var character = typer.GenerateNextCharacter();

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

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void GenerateNextCharacter_WithLetters_ShouldEventuallySelectWinner(int wordLength)
        {
            string[] choices = [Faker.Random.String2(wordLength), Faker.Random.String2(wordLength)];

            var watch = new Stopwatch();
            var session = new Session();
            session.Parameters = new SessionParameters() { AllowedCharacters = SessionCharacterType.Letters, PlayersDefineCharacters = true };
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choices[0] });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choices[1] });

            var typer = new MonkifyTyper(session);

            watch.Start();
            while (!typer.HasWinners)
                typer.GenerateNextCharacter();
            watch.Stop();

            typer.HasWinners.ShouldBeTrue();
            typer.NumberOfWinners.ShouldBe(1);
            choices.Contains(typer.FirstChoiceTyped).ShouldBeTrue();
            watch.Elapsed.TotalSeconds.ShouldBeLessThan(DefaultSessionAverageDuration);
        }

        [Theory]
        [InlineData(1, 9)]
        [InlineData(10, 99)]
        [InlineData(100, 999)]
        [InlineData(1000, 9999)]
        [InlineData(10000, 99999)]
        [InlineData(100000, 999999)]
        [InlineData(1000000, 9999999)]
        [InlineData(10000000, 99999999)]
        public void GenerateNextCharacter_WithNumbers_ShouldEventuallySelectWinner(int min, int max)
        {
            string[] choices = [Faker.Random.Int(min, max).ToString(), Faker.Random.Int(min, max).ToString()];

            var watch = new Stopwatch();
            var session = new Session();
            session.Parameters = new SessionParameters() { AllowedCharacters = SessionCharacterType.Number, PlayersDefineCharacters = true };
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choices[0] });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choices[1] });

            var typer = new MonkifyTyper(session);

            watch.Start();
            while (!typer.HasWinners)
                typer.GenerateNextCharacter();
            watch.Stop();

            typer.HasWinners.ShouldBeTrue();
            choices.Contains(typer.FirstChoiceTyped).ShouldBeTrue();
            watch.Elapsed.TotalSeconds.ShouldBeLessThan(DefaultSessionAverageDuration);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void GenerateNextCharacter_PresetCharacterChoices_ShouldEventuallySelectWinner(int wordLength)
        {
            string[] choices = [Faker.Random.String2(wordLength), Faker.Random.String2(wordLength)];

            var watch = new Stopwatch();
            var session = new Session();
            session.Parameters = new SessionParameters() { AllowedCharacters = SessionCharacterType.Letters, PlayersDefineCharacters = true };
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choices[0] });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choices[1] });

            var typer = new MonkifyTyper(session);

            watch.Start();
            while (!typer.HasWinners)
                typer.GenerateNextCharacter();
            watch.Stop();

            typer.HasWinners.ShouldBeTrue();
            typer.NumberOfWinners.ShouldBe(1);
            choices.Contains(typer.FirstChoiceTyped).ShouldBeTrue();
            watch.Elapsed.TotalSeconds.ShouldBeLessThan(DefaultSessionAverageDuration);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void GenerateNextCharacter_WithLettersAndNumbers_ShouldEventuallySelectWinner(int wordLength)
        {
            string[] choices = [Faker.Random.String2(wordLength, SessionCharacterType.NumbersAndLetters.StringValueOf()), Faker.Random.String2(wordLength, SessionCharacterType.NumbersAndLetters.StringValueOf())];

            var watch = new Stopwatch();
            var session = new Session();
            session.Parameters = new SessionParameters() { AllowedCharacters = SessionCharacterType.Letters, PlayersDefineCharacters = true };
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choices[0] });
            session.Bets.Add(new Bet(BetStatus.Made, 10) { Choice = choices[1] });

            var typer = new MonkifyTyper(session);

            watch.Start();
            while (!typer.HasWinners)
                typer.GenerateNextCharacter();
            watch.Stop();

            typer.HasWinners.ShouldBeTrue();
            typer.NumberOfWinners.ShouldBe(1);
            choices.Contains(typer.FirstChoiceTyped).ShouldBeTrue();
            watch.Elapsed.TotalSeconds.ShouldBeLessThan(DefaultSessionAverageDuration);
        }
    }
}
