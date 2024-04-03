using Microsoft.IdentityModel.Tokens;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Users.ValueObjects;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Users.Services
{
    public class JweTokenHandler
    {
        public JweTokenHandler(AuthenticationSettings settings)
        {
            _settings = settings;
        }

        private readonly AuthenticationSettings _settings;

        public TokenDto CreateToken(Guid userId)
        {
            var claims = new Claim[]
            {
                new ("sub", userId.ToString())
            };

            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
            var expirationDate = DateTime.UtcNow.AddMinutes(_settings.TokenDuration);

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _settings.Issuer,
                Audience = _settings.Audience,
                Subject = new ClaimsIdentity(claims),
                Expires = expirationDate,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature),
            };

            var token = handler.CreateToken(descriptor);
            var tokenString = handler.WriteToken(token);
            return new TokenDto(userId, tokenString, expirationDate);
        }
    }
}
