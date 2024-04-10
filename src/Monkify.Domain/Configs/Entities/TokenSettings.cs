using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Configs.Entities
{
    public class TokenSettings
    {
        public string MintAddress { get; set; }
        public string SenderAccount { get; set; }
        public string TokenOwnerPublicKey { get; set; }
        public string TokenOwnerPrivateKey { get; set; }
        public int Decimals { get; set; }
    }
}
