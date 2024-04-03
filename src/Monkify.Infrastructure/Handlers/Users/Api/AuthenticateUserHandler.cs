using Microsoft.EntityFrameworkCore;
using Monkify.Common.Messaging;
using Monkify.Domain.Configs.Entities;
using Monkify.Domain.Users.Entities;
using Monkify.Domain.Users.Requests;
using Monkify.Domain.Users.Services;
using Monkify.Domain.Users.ValueObjects;
using Monkify.Infrastructure.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Users.Api
{
    public class AuthenticateUserHandler : BaseRequestHandler<AuthenticateUserRequest, TokenDto>
    {
        public AuthenticateUserHandler(MonkifyDbContext context, IMessaging messaging, GeneralSettings settings) : base(context, messaging)
        {
            _settings = settings.Authentication;
        }

        private readonly AuthenticationSettings _settings;

        private Guid? _userId;
        private TokenDto _token;

        public override async Task<TokenDto> HandleRequest(AuthenticateUserRequest request, CancellationToken cancellationToken)
        {
            await ValidateUser(request);
            GenerateToken();

            return _token;
        }

        private async Task ValidateUser(AuthenticateUserRequest request)
        {
            _userId = await Context.Users.Where(x => x.Email == request.Email && x.Active).Select(x => x.Id).FirstOrDefaultAsync();

            if (_userId is null || _userId == Guid.Empty)
                Messaging.ReturnValidationFailureMessage("Invalid login credentials. Please check your information and try again.");
        }

        private void GenerateToken()
        {
            var tokenHandler = new JweTokenHandler(_settings);
            _token = tokenHandler.CreateToken(_userId.Value);
        }
    }
}
