using Microsoft.IdentityModel.Tokens;
using Monkify.Common.Extensions;
using Monkify.Domain.Monkey.Entities;
using Monkify.Domain.Monkey.ValueObjects;
using System.Linq;

namespace Monkify.Domain.Monkey.Services
{
    public class MonkifyTyper
    {
        public MonkifyTyper(SessionCharacterType characterType, IEnumerable<Bet> bets)
        {
            if (bets.IsNullOrEmpty())
                throw new ArgumentException("At least one bet must be made to start a session. Session has ended");

            SetBets(bets);
            SetCharactersOnTyper(characterType);

            _random = new Random();
            _lastTypedCharacters = new Queue<char>();
            Winners = new List<Bet>();
        }

        public bool HasWinners { get => !Winners.IsNullOrEmpty(); }
        public string FirstChoiceTyped { get; private set; }
        public List<Bet> Winners { get; private set; }

        private IEnumerable<Bet> _bets;
        private char[] _charactersOnTyper { get; set; }

        private Random _random;
        private int _queueLength { get; set; }
        private Queue<char> _lastTypedCharacters { get; set; }

        private void SetBets(IEnumerable<Bet> bets)
        {
            _bets = bets;

            foreach(var bet in _bets)
            {
                if (bet.BetChoice.Length > _queueLength)
                    _queueLength = bet.BetChoice.Length;
            }
        }

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

            foreach (var bet in _bets)
            {
                foreach (var character in bet.BetChoice)
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
            foreach (var bet in _bets)
            {
                if (new string(_lastTypedCharacters.ToArray()).Contains(bet.BetChoice))
                {
                    FirstChoiceTyped = bet.BetChoice;
                    Winners.Add(bet);
                }
            }
        }
    }
}