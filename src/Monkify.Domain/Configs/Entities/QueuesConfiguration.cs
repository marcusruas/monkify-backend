using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Configs.Entities
{
    public class QueuesConfiguration
    {
        public string ActiveSessions { get; set; }
        public string SessionStatus { get; set; }
        public string SessionBets { get; set; }
        public string SessionTerminal { get; set; }
    }
}
