using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monkify.Common.Messaging;
using Monkify.Results;
using System;
using System.Net;

namespace Monkify.Api.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        public BaseController(IMediator mediador)
        {
            Mediator = mediador;
        }

        protected readonly IMediator Mediator;

        /// <summary>
        /// Passes the object of type <see cref="IRequest" /> to the mediator of type <see cref="IMediator"/> and returns the formatted response ready for the endpoint
        /// </summary>
        protected async Task<IActionResult> ProcessRequest<T>(IRequest<T> request)
        {
            var result = await Mediator.Send(request);

            var messaging = HttpContext.RequestServices.GetService<IMessaging>();
            var statusCode = (int)GetStatusCodeResult(messaging);

            var defaultResult = new ApiResult<T>(result, messaging.Messages);
            return StatusCode(statusCode, defaultResult);
        }

        private HttpStatusCode GetStatusCodeResult(IMessaging messaging)
        {
            if (messaging.HasErrors())
                return HttpStatusCode.InternalServerError;

            if (messaging.HasValidationFailures())
                return HttpStatusCode.BadRequest;

            return HttpStatusCode.OK;
        }
    }
}
