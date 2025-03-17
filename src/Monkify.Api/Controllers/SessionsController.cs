using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monkify.Common.Messaging;
using Monkify.Infrastructure.Handlers.Sessions.GetActiveParameters;
using Monkify.Infrastructure.Handlers.Sessions.GetActiveSessionForParameter;
using Monkify.Infrastructure.Handlers.Sessions.RegisterBet;

namespace Monkify.Api.Controllers
{
    [Route("api/sessions")]
    [Produces("application/json")]
    public class SessionsController(IMediator mediador, IMessaging messaging) : BaseController(mediador, messaging)
    {
        [HttpGet("active-types")]
        public async Task<IActionResult> GetActiveParameters()
            => await ProcessRequest(new GetActiveParametersRequest());
        
        [HttpGet("{sessionTypeId}")]
        public async Task<IActionResult> GetActiveSessionForParameter(Guid sessionTypeId)
            => await ProcessRequest(new GetActiveSessionForParameterRequest(sessionTypeId));

        [HttpPost("{id}/bets")]
        public async Task<IActionResult> RegisterBet(Guid id, [FromBody] RegisterBetRequestBody body)
            => await ProcessRequest(new RegisterBetRequest(id, body));
    }
}
