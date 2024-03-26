using MediatR;
using Microsoft.AspNetCore.Mvc;
using Monkify.Domain.Users.Requests;

namespace Monkify.Api.Controllers
{
    [Route("api/users")]
    [Produces("application/json")]
    public class UsersController : BaseController
    {
        public UsersController(IMediator mediador) : base(mediador) { }

        [HttpPost]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request)
            => await ProcessRequest(request);
    }
}
