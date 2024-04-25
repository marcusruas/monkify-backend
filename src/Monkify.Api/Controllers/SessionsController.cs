using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monkify.Common.Messaging;
using Monkify.Infrastructure.Handlers.Sessions.GetAllBets;
using Monkify.Infrastructure.Handlers.Sessions.RegisterBet;

namespace Monkify.Api.Controllers
{
    [Route("api/sessions")]
    [Produces("application/json")]
    public class SessionsController(IMediator mediador, IMessaging messaging) : BaseController(mediador, messaging)
    {
        [HttpGet("bets")]
        public async Task<IActionResult> GetAllBets([FromQuery] FilterBetsRequest request)
            => await ProcessRequest(request);

        [HttpPost("{id}/bets")]
        public async Task<IActionResult> RegisterBet(Guid id, [FromBody] RegisterBetRequestBody body)
            => await ProcessRequest(new RegisterBetRequest(id, body));
    }
}
