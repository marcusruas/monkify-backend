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
        [Description("Requested choice does not match the required length")]
        WrontChoiceLength,
        [Description("Requested choice has duplicate characters")]
        UnacceptedDuplicateCharacters,
        [Description("Requested choice has invalid characters")]
        InvalidCharacters,
        Valid
    }
}
