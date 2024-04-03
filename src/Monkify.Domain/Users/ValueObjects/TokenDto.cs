using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Users.ValueObjects
{
    public class TokenDto
    {
        public TokenDto(Guid userId, string bearerToken, DateTime expirationDate)
        {
            UserId = userId;
            BearerToken = bearerToken;
            ExpirationDate = expirationDate;
        }

        public Guid UserId { get; set; }
        public string BearerToken { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
