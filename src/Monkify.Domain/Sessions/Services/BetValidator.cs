using Monkify.Common.Extensions;
using Monkify.Common.Messaging;
using Monkify.Domain.Sessions.Entities;
using Monkify.Domain.Sessions.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.Services
{
    public class BetValidator
    {
        public static BetValidationResult ChoiceIsValidForSession(Bet bet, Session session)
        {
            var parameters = session.Parameters;

            if (string.IsNullOrEmpty(bet.BetChoice) && bet.BetChoice.Length != parameters.ChoiceRequiredLength)
                return BetValidationResult.WrontChoiceLength;

            if (!parameters.AcceptDuplicatedCharacters && bet.BetChoice.ContainsDuplicateCharacters())
                return BetValidationResult.UnacceptedDuplicateCharacters;

            if (bet.BetAmount != session.Parameters.RequiredAmount)
                return BetValidationResult.InvalidAmount;

            if (parameters.SessionCharacterType.ContainsAttribute<DescriptionAttribute>())
            {
                var acceptedCharacters = parameters.SessionCharacterType.StringValueOf().ToArray();
                if (!bet.BetChoice.All(character => acceptedCharacters.Contains(character)))
                    return BetValidationResult.InvalidCharacters;
            }

            return BetValidationResult.Valid;
        }
    }
}
