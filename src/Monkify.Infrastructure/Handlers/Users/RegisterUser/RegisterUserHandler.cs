using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Monkify.Common.Messaging;
using Monkify.Domain.Users.Entities;
using Monkify.Infrastructure.Context;
using Nelibur.ObjectMapper;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Users.RegisterUser
{
    public class RegisterUserHandler : BaseRequestHandler<RegisterUserRequest, bool>
    {
        public RegisterUserHandler(MonkifyDbContext context, IMessaging messaging) : base(context, messaging) { }

        private User _newUser;

        public override async Task<bool> HandleRequest(RegisterUserRequest request, CancellationToken cancellationToken)
        {
            _newUser = request.ToUser();

            await ValidateUser();
            await RegisterUser(cancellationToken);

            return true;
        }

        private async Task ValidateUser()
        {
            var emailExists = await Context.Users.AnyAsync(x => x.Email == _newUser.Email && x.Active);

            if (emailExists)
                Messaging.ReturnValidationFailureMessage("This email is already registered. Please try another one.");
        }

        private async Task RegisterUser(CancellationToken cancellationToken)
        {
            await Context.Users.AddAsync(_newUser);
            var affectedRows = await Context.SaveChangesAsync(cancellationToken);

            if (affectedRows <= 0)
            {
                Messaging.ReturnValidationFailureMessage("The system could not register this user at the moment, please try again later.");
                Log.Error("An error occurred while trying to register a new user with the email {0}", _newUser.Email);
            }
            Messaging.AddInformationalMessage("User successfully registered.");
        }
    }
}
