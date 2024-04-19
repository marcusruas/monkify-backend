﻿using Microsoft.IdentityModel.Tokens;
using Monkify.Common.Extensions;
using Monkify.Common.Resources;
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
                throw new ArgumentException(ErrorMessages.TyperStartedWithoutBets);

            GenerateRandom();
            SetBets(session);
            SetCharactersOnTyper(session);            
        }

        public bool HasWinners { get; private set; }
        public int NumberOfWinners { get; private set; }
        public string FirstChoiceTyped { get; private set; }

        public Random _random { get; private set; }
        public Dictionary<string, int> Bets { get; private set; }
        public int QueueLength { get; private set; }
        public char[] CharactersOnTyper { get; private set; }
        public Queue<char> TypedCharacters { get; private set; }

        private void SetBets(Session session)
        {
            Bets = [];

            if (!session.Parameters.PresetChoices.IsNullOrEmpty())
            {
                foreach(var presetChoice in session.Parameters.PresetChoices)
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

                if (Bets.ContainsKey(bet.Choice))
                    Bets[bet.Choice]++;
                else
                    Bets.Add(bet.Choice, 1);
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

            if (Bets.TryGetValue(choice, out int amountOfPlayers))
            {
                HasWinners = amountOfPlayers != 0;
                NumberOfWinners += amountOfPlayers;
                FirstChoiceTyped = choice;
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