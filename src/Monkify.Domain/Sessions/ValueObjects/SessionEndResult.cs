using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Sessions.ValueObjects
{
    public record SessionResult(IEnumerable<string> Winners, int NumberOfWinners, string FirstChoiceTyped);
}
