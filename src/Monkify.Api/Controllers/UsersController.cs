using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monkify.Domain.Users.Requests;

namespace Monkify.Api.Controllers
{
    [Route("api/users")]
    [Produces("application/json")]
    public class UsersController : BaseController
    {
        public UsersController(IMediator mediador) : base(mediador) { }

        [HttpGet("personal-info")]
        [Authorize]
        public async Task<IActionResult> GetUserPersonalInfo()
            => throw new NotImplementedException();

        [HttpGet("bet-history")]
        [Authorize]
        public async Task<IActionResult> GetUserBetHistory()
            => throw new NotImplementedException();

        [HttpPost]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request)
            => await ProcessRequest(request);

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate()
            => throw new NotImplementedException();

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
            => throw new NotImplementedException();

        [HttpPut("personal-info")]
        [Authorize]
        public async Task<IActionResult> UpdatePersonalInfo()
            => throw new NotImplementedException();

        [HttpPut("wallet")]
        [Authorize]
        public async Task<IActionResult> UpdateWallet()
            => throw new NotImplementedException();

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeactivateAccount()
            => throw new NotImplementedException();
    }
}
