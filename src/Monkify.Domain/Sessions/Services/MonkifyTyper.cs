using System.Security.Cryptography;
using System.Text;
using Monkify.Common.Extensions;
using Monkify.Common.Resources;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;

namespace Monkify.Domain.Sessions.Services
{
    public class MonkifyTyper
    {
        private const int TYPING_SPEED_REFRESH_INTERVAL = 1000;
        private const int INITIAL_TYPING_SPEED_MS = 1; // Start at 1ms per character

        public MonkifyTyper(Session session)
        {
            if (session.Bets.IsNullOrEmpty())
                throw new ArgumentException(ErrorMessages.TyperStartedWithoutBets);

            SessionId = session.Id;

            GenerateSessionSeed(session);
            SetBets(session);
            SetCharactersOnTyper(session);

            InitializeTiming();
        }

        public Guid SessionId { get; }
        public bool HasWinners { get; private set; }
        public int NumberOfWinners { get; private set; }
        public int SessionSeed { get; private set; }
        public string FirstChoiceTyped { get; private set; }

        public int TypingSpeed { get; private set; }
        public Dictionary<string, int> Bets { get; private set; }
        public int QueueLength { get; private set; }
        public char[] CharactersOnTyper { get; private set; }
        public int CharactersTypedCount { get; private set; }

        private Random _random { get; set; }
        private Queue<char> _typedCharacters { get; set; }

        #region Session management methods

        public char GenerateNextCharacter()
        {
            var characterIndex = _random.Next(CharactersOnTyper.Length);
            var character = CharactersOnTyper[characterIndex];

            if (_typedCharacters.Count == QueueLength)
                _typedCharacters.Dequeue();

            _typedCharacters.Enqueue(character);
            CharactersTypedCount++;

            CheckForWinners();

            if (CharactersTypedCount % TYPING_SPEED_REFRESH_INTERVAL == 0 && TypingSpeed > 0)
            {
                TypingSpeed = (int)(TypingSpeed * 0.50);
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

        private void InitializeTiming()
        {
            TypingSpeed = INITIAL_TYPING_SPEED_MS;
            CharactersTypedCount = 0;
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

        #endregion
    }
}