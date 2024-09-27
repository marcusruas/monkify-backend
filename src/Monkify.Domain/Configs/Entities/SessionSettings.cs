using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Configs.Entities
{
    [ExcludeFromCodeCoverage]
    public class SessionSettings
    {
        public int SupportEmail { get; set; }
        public int MinimumWaitPeriodForBets { get; set; }
        public int MaximumWaitPeriodForBets { get; set; }
        public int TerminalBatchLimit { get; set; }
        public int DelayBetweenTerminalBatches { get; set; }
        public int DelayBetweenSessions { get; set; }
        public string ActiveSessionsEndpoint { get; set; }
        public string SessionStatusEndpoint { get; set; }
        public string SessionBetsEndpoint { get; set; }
        public string SessionTerminalEndpoint { get; set; }
    }
}
