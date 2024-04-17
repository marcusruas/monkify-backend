using Microsoft.IdentityModel.Tokens;
using Monkify.Common.Extensions;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;

namespace Monkify.Domain.Sessions.Services
{
    public class MonkifyTyper
    {
        public MonkifyTyper(Session session)
        {
            if (session.Bets.IsNullOrEmpty())
                throw new ArgumentException("At least one bet must be made to start a session. Session has ended");

            GenerateRandom();
            SetBets(session);
            SetCharactersOnTyper(session);            
        }

        public bool HasWinners { get; private set; }
        public int NumberOfWinners { get; private set; }
        public string FirstChoiceTyped { get; private set; }

        private Dictionary<string, List<Bet>> Bets;
        private int QueueLength { get; set; }
        private char[] CharactersOnTyper { get; set; }
        private Queue<char> TypedCharacters { get; set; }
        private Random _random;

        private void SetBets(Session session)
        {
            Bets = [];

            if (session.Parameters.PresetChoices.IsNullOrEmpty())
            {
                foreach(var presetChoice in  session.Parameters.PresetChoices)
                {
                    if (presetChoice.Choice.Length > QueueLength)
                        QueueLength = presetChoice.Choice.Length;

                    Bets.Add(presetChoice.Choice, new List<Bet>());
                }
            }

            foreach (var bet in session.Bets)
            {
                if (bet.Choice.Length > QueueLength)
                    QueueLength = bet.Choice.Length;

                if (Bets.ContainsKey(bet.Choice))
                    Bets[bet.Choice].Add(bet);
                else
                    Bets.Add(bet.Choice, new List<Bet>() { bet });
            }

            TypedCharacters = new Queue<char>(QueueLength);
        }

        private void SetCharactersOnTyper(Session session)
        {
            if (session.Parameters.SessionCharacterType.ContainsAttribute<DescriptionAttribute>())
                CharactersOnTyper = [.. session.Parameters.SessionCharacterType.StringValueOf()];
            else
                SetCharactersOnTyperByBets(session);
        }

        private void SetCharactersOnTyperByBets(Session session)
        {
            var result = new HashSet<char>();

            foreach (var bet in session.Bets)
            {
                foreach (var character in bet.Choice)
                    result.Add(character);
            }

            CharactersOnTyper = [.. result.Order()];
        }

        public char GenerateNextCharacter()
        {
            if (HasWinners)
                throw new ArgumentException("Cannot generate next character, as there is already a Winner.");

            var characterIndex = _random.Next(CharactersOnTyper.Length);
            var character = CharactersOnTyper[characterIndex];

            if (TypedCharacters.Count == QueueLength)
                TypedCharacters.Dequeue();

            TypedCharacters.Enqueue(character);

            CheckForWinners();

            return character;
        }

        private void CheckForWinners()
        {
            string choice = new (TypedCharacters.ToArray());

            if (Bets.TryGetValue(choice, out List<Bet>? value))
            {
                HasWinners = value.Count != 0;
                NumberOfWinners += value.Count;
                FirstChoiceTyped = choice;
                value.ForEach(x => x.Won = true);
            }
        }

        private void GenerateRandom()
        {
            var seedInBytes = RandomNumberGenerator.GetBytes(4);
            var seed = BitConverter.ToInt32(seedInBytes, 0);

            _random = new Random(seed);
        }
    }
}