using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Configs.Entities
{
    public class GeneralSettings
    {
        public SessionSettings Sessions { get; set; }
        public TokenSettings Token { get; set; }
        public WorkerSettings Workers { get; set; }
        public PollySettings Polly { get; set; }
    }
}
