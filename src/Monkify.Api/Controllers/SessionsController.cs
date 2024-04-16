using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monkify.Common.Messaging;
using Monkify.Infrastructure.Handlers.Sessions.GetActiveSessions;
using Monkify.Infrastructure.Handlers.Sessions.GetAllBets;
using Monkify.Infrastructure.Handlers.Sessions.GetSessionById;
using Monkify.Infrastructure.Handlers.Sessions.RegisterBet;

namespace Monkify.Api.Controllers
{
    [Route("api/sessions")]
    [Produces("application/json")]
    public class SessionsController : BaseController
    {
        public SessionsController(IMediator mediador, IMessaging messaging) : base(mediador, messaging) { }

        [HttpGet("active-sessions")]
        public async Task<IActionResult> GetActiveSessions()
            => await ProcessRequest(new GetActiveSessionsRequest());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSessionById(Guid id)
            => await ProcessRequest(new GetSessionByIdRequest(id));

        [HttpGet("bets")]
        public async Task<IActionResult> GetAllBets([FromQuery] FilterBetsRequest request)
            => await ProcessRequest(request);

        [HttpPost("{id}/bets")]
        public async Task<IActionResult> RegisterBet(Guid id, [FromBody] RegisterBetRequestBody body)
            => await ProcessRequest(new RegisterBetRequest(id, body));
    }
}
