using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.ValueObjects
{
    public enum BetValidationResult
    {
        [Description("Requested choice does not match the required length.")]
        WrongChoiceLength,
        [Description("Requested choice has duplicate characters.")]
        UnacceptedDuplicateCharacters,
        [Description("Requested choice has invalid characters.")]
        InvalidCharacters,
        [Description("Requested bet amount is invalid for this session.")]
        InvalidAmount,
        [Description("Requested choice is invalid due to it not being part of the preset choices.")]
        InvalidChoice,
        Valid
    }
}
