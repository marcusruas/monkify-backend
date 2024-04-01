using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Endpoints
{
    public static class QueuesEndpoints
    {
        public static string ACTIVE_SESSIONS_ENDPOINT = "/active-sessions";
        public static string SESSION_STATUS_ENDPOINT = "/{0}/status";
        public static string SESSION_BETS_ENDPOINT = "/{0}/bets";
        public static string SESSION_TERMINAL_ENDPOINT = "/{0}/terminal";
    }
}
