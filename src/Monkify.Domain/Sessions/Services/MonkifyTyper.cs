using Microsoft.IdentityModel.Tokens;
using Monkify.Common.Extensions;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using System.Collections.ObjectModel;
using System.Linq;

namespace Monkify.Domain.Sessions.Services
{
    public class MonkifyTyper
    {
        public MonkifyTyper(SessionCharacterType characterType, ICollection<Bet> bets)
        {
            if (bets.IsNullOrEmpty())
                throw new ArgumentException("At least one bet must be made to start a session. Session has ended");

            Bets = new Collection<Bet>();

            foreach (var bet in bets)
                Bets.Add(bet);

            _queueLength = Bets.Max(x => x.Choice.Length);

            SetCharactersOnTyper(characterType);

            _random = new Random();
            _lastTypedCharacters = new Queue<char>();
        }

        public bool HasWinners { get; private set; }
        public int NumberOfWinners { get; private set; }
        public string FirstChoiceTyped { get; private set; }

        public ICollection<Bet> Bets { get; private set; }
        private char[] _charactersOnTyper { get; set; }

        private Random _random;
        private int _queueLength { get; set; }
        private Queue<char> _lastTypedCharacters { get; set; }

        private void SetCharactersOnTyper(SessionCharacterType characterType)
        {
            var terminalCharactersForType = characterType.StringValueOf();
            if (!string.IsNullOrWhiteSpace(terminalCharactersForType))
                _charactersOnTyper = terminalCharactersForType.ToArray();
            else
                SetCharactersOnTyperByBets();
        }

        private void SetCharactersOnTyperByBets()
        {
            var result = new HashSet<char>();

            foreach (var bet in Bets)
            {
                foreach (var character in bet.Choice)
                    result.Add(character);
            }

            _charactersOnTyper = result.Order().ToArray();
        }

        public char GenerateNextCharacter()
        {
            if (HasWinners)
                throw new ArgumentException("Cannot generate next character, as there is already a Winner.");

            var characterIndex = _random.Next(0, _charactersOnTyper.Length - 1);
            var character = _charactersOnTyper[characterIndex];

            if (_lastTypedCharacters.Count() == _queueLength + 1)
                _lastTypedCharacters.Dequeue();

            _lastTypedCharacters.Enqueue(character);

            CheckForWinners();

            return character;
        }

        private void CheckForWinners()
        {
            foreach (var bet in Bets)
            {
                if (new string(_lastTypedCharacters.ToArray()).Contains(bet.Choice))
                {
                    HasWinners = true;
                    NumberOfWinners++;
                    FirstChoiceTyped = bet.Choice;
                    bet.Won = true;
                }
            }
        }
    }
}