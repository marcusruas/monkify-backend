using Monkify.Common.Models;
using Monkify.Domain.Users.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Users.Entities
{
    public class User : TableEntity
    {
        public string Email { get; set; }
        public string? Password { get; set; }
        public RegistrationType RegistrationType { get; set; }
        public string WalletId { get; set; }
    }
}
