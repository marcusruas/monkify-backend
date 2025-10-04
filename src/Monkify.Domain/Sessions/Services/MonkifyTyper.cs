using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Monkify.Common.Extensions;
using Monkify.Common.Resources;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;

namespace Monkify.Domain.Sessions.Services
{
    public class MonkifyTyper
    {
        public MonkifyTyper(Session session)
        {
            if (session.Bets.IsNullOrEmpty())
                throw new ArgumentException(ErrorMessages.TyperStartedWithoutBets);

            SessionId = session.Id;

            GenerateSessionSeed(session);
            SetBets(session);
            SetCharactersOnTyper(session);
            CalculateDelayIntervals();
        }

        public Guid SessionId { get; }
        public bool HasWinners { get; private set; }
        public int NumberOfWinners { get; private set; }
        public int SessionSeed { get; private set; }
        public string FirstChoiceTyped { get; private set; }

        public Dictionary<string, int> Bets { get; private set; }
        public int QueueLength { get; private set; }
        public char[] CharactersOnTyper { get; private set; }
        public int CharactersTypedCount { get; private set; } = 0;

        private Random _random { get; set; }
        private Queue<char> _typedCharacters { get; set; }
        private int _betDelayInterval { get; set; }
        private int _expectedCharsToWin { get; set; }

        private const int BET_INTERVAL_COEFFICIENT = 3;

        #region Session management methods

        public async Task<char> GenerateNextCharacter(CancellationToken cancellationToken)
        {
            if (_betDelayInterval > 0 && CharactersTypedCount % _betDelayInterval == 0)
                await Task.Delay(1, cancellationToken); //delay of 1ms after each x characters

            var characterIndex = _random.Next(CharactersOnTyper.Length);
            var character = CharactersOnTyper[characterIndex];

            if (_typedCharacters.Count == QueueLength)
                _typedCharacters.Dequeue();

            _typedCharacters.Enqueue(character);
            CharactersTypedCount++;

            CheckForWinners();

            if (CharactersTypedCount == _expectedCharsToWin)
            {
                _betDelayInterval = (int)(_betDelayInterval * 2.5);
            }
            
            if (CharactersTypedCount == _expectedCharsToWin * 2)
            {
                _betDelayInterval = (int)(_betDelayInterval * 2);
            }

            return character;
        }

        private void CheckForWinners()
        {
            string choice = string.Concat(_typedCharacters);

            if (Bets.TryGetValue(choice, out int amountOfPlayers))
            {
                HasWinners = amountOfPlayers != 0;
                NumberOfWinners += amountOfPlayers;
                FirstChoiceTyped = choice;
            }
        }

        #endregion

        #region Setup methods

        private void GenerateSessionSeed(Session session)
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            var concatenatedSeed = new StringBuilder(Convert.ToBase64String(bytes));

            foreach (var bet in session.Bets)
                concatenatedSeed.Append(bet.Seed);

            var seedInBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(concatenatedSeed.ToString()));
            SessionSeed = BitConverter.ToInt32(seedInBytes, 0);
            _random = new Random(SessionSeed);
        }

        private void SetBets(Session session)
        {
            Bets = [];

            if (!session.Parameters.PresetChoices.IsNullOrEmpty())
            {
                foreach (var presetChoice in session.Parameters.PresetChoices)
                {
                    if (presetChoice.Choice.Length > QueueLength)
                        QueueLength = presetChoice.Choice.Length;

                    Bets.Add(presetChoice.Choice, 0);
                }
            }

            foreach (var bet in session.Bets)
            {
                if (bet.Choice.Length > QueueLength)
                    QueueLength = bet.Choice.Length;

                if (Bets.TryGetValue(bet.Choice, out int value))
                    Bets[bet.Choice] = ++value;
                else
                    Bets.Add(bet.Choice, 1);
            }

            _typedCharacters = new Queue<char>(QueueLength);
        }

        private void SetCharactersOnTyper(Session session)
        {
            bool shouldSetByBets = !session.Parameters.PresetChoices.IsNullOrEmpty() ||
                                   (session.Parameters.AllowedCharacters == SessionCharacterType.Letters && session.Parameters.ChoiceRequiredLength == 5 && session.Bets.Count <= 6) ||
                                   (session.Parameters.AllowedCharacters == SessionCharacterType.Letters && session.Parameters.ChoiceRequiredLength == 6 && session.Bets.Count <= 12) ||
                                   (session.Parameters.AllowedCharacters == SessionCharacterType.NumbersAndLetters && session.Parameters.ChoiceRequiredLength >= 5 && session.Bets.Count <= 5);

            if (shouldSetByBets)
            {
                var result = new HashSet<char>();

                foreach (var bet in session.Bets)
                {
                    foreach (var character in bet.Choice)
                        result.Add(character);
                }

                CharactersOnTyper = [.. result.Order()];
            }
            else
            {
                CharactersOnTyper = [.. session.Parameters.AllowedCharacters.StringValueOf()];
            }
        }

        private void CalculateDelayIntervals()
        {
            _expectedCharsToWin = Convert.ToInt32(Math.Pow(CharactersOnTyper.Length, QueueLength) / Bets.Count);
            decimal paceCorrection = QueueLength > 4 ? 0.03m * decimal.Parse(Math.Pow(2, QueueLength - 5).ToString()) : 0;

            var charsPerMs = 2024422 / 1000;

            switch (QueueLength)
            {
                case 4:
                    paceCorrection = 0.02m;
                    break;
                case 5:
                    paceCorrection = 0.06m;
                    break;
                case 6:
                    paceCorrection = 0.20m;
                    break;
            }

            decimal baseTime = (_expectedCharsToWin / charsPerMs) * (1.5m - paceCorrection);

            _betDelayInterval = int.Parse(Math.Ceiling(baseTime).ToString());
        }

        #endregion
    }
}