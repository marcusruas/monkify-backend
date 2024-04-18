using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Configs.Entities
{
    public class WorkerSettings
    {
        public int CreateSessionsInterval { get; set; }
        public int RefundBetsInterval { get; set; }
        public int RewardSessionsInterval { get; set; }
    }
}
