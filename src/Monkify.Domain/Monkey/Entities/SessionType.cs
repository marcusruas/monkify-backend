using Monkify.Common.Models;
using Monkify.Domain.Monkey.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Monkey.Entities
{
    public class SessionType : TableEntity
    {
        public SessionCharacterType SessionCharacterType { get; set; }
        public int Order { get; set; }
        public bool Active { get; set; }
    }
}
