using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monkify.Domain.Sessions.ValueObjects;

namespace Monkify.Tests.Shared
{
    public class SessionMetricsTestParameters
    {
        public int BetsPerGame { get; set; }

        public SessionCharacterType CharacterType { get; set; }
        public bool AcceptsDuplicateCharacters { get; set; }

        public int WordLength { get; set; }
        public string Charset { get; set; } = "abcdefghijklmnopqrstuvwxyz";
        public List<string> PresetChoices { get; set; } = new();
    }
}
