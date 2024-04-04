using MediatR;
using Monkify.Common.Extensions;
using Monkify.Domain.Users.Entities;
using Monkify.Domain.Users.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Users.RegisterUser
{
    public class RegisterUserRequest : IRequest<bool>
    {
        public string? Username { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid Email.")]
        public string? Email { get; set; }
        [Required(ErrorMessage = "Wallet ID is required.")]
        public string? WalletId { get; set; }

        public User ToUser()
            => new User()
            {
                Email = Email,
                WalletId = WalletId
            };
    }
}
