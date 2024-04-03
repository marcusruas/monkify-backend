using MediatR;
using Monkify.Domain.Users.ValueObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Domain.Users.Requests
{
    public class AuthenticateUserRequest : IRequest<TokenDto>
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid Email.")]
        public string? Email { get; set; }
    }
}
