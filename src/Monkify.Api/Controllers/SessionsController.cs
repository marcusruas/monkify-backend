using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Monkify.Api.Controllers
{
    [Route("api/sessions")]
    [Produces("application/json")]
    public class SessionsController : BaseController
    {
        public SessionsController(IMediator mediador) : base(mediador) { }

        [HttpGet]
        public async Task<IActionResult> GetActiveSessions()
            => throw new NotImplementedException();

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSessionById()
            => throw new NotImplementedException();
        
        [HttpGet("bets")]
        public async Task<IActionResult> GetAllBets()
            => throw new NotImplementedException();

        [HttpPost("{id}/bets")]
        public async Task<IActionResult> RegisterBet()
            => throw new NotImplementedException();
    }
}
