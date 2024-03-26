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

namespace Monkify.Domain.Users.Requests
{
    public class RegisterUserRequest : IRequest<bool>
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid Email.")]
        public string? Email { get; set; }
        public string? Password { get; set; }
        [Required(ErrorMessage = "Registration type is required.")]
        public RegistrationType? RegistrationType { get; set; }
        [Required(ErrorMessage = "Wallet ID is required.")]
        public string? WalletId { get; set; }

        public User ToUser()
            => new User()
            {
                Email = Email,
                Password = Password.ToSHA256(),
                RegistrationType = RegistrationType.Value,
                WalletId = WalletId
            };
    }
}
