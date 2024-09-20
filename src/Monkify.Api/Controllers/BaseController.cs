using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monkify.Common.Extensions;
using Monkify.Common.Messaging;
using Monkify.Results;
using System;
using System.Net;

namespace Monkify.Api.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        public BaseController(IMediator mediador, IMessaging messaging)
        {
            Messaging = messaging;
            Mediator = mediador;
        }

        protected readonly IMessaging Messaging;
        protected readonly IMediator Mediator;

        /// <summary>
        /// Passes the object of type <see cref="IRequest" /> to the mediator of type <see cref="IMediator"/> and returns the formatted response ready for the endpoint
        /// </summary>
        protected async Task<IActionResult> ProcessRequest<T>(IRequest<T> request)
        {
            var result = await Mediator.Send(request);
            var statusCode = (int)GetStatusCodeResult();

            if (result is bool success && success)
                statusCode = (int)HttpStatusCode.Created;

            if ((result is null) || (result is IEnumerable<object> list && list.IsNullOrEmpty()))
                statusCode = (int) HttpStatusCode.NotFound;

            var defaultResult = new ApiResult<T>(result, Messaging.Messages);
            return StatusCode(statusCode, defaultResult);
        }

        /// <summary>
        /// Formats the response of a handler into a standard API response
        /// </summary>
        protected IActionResult GenerateResultBody<T>(HttpStatusCode statusCode, T response)
        {
            var messaging = HttpContext.RequestServices.GetService<IMessaging>();
            var defaultResult = new ApiResult<T>(response, messaging.Messages);
            return StatusCode((int) statusCode, defaultResult);
        }

        private HttpStatusCode GetStatusCodeResult()
        {
            if (Messaging.HasErrors())
                return HttpStatusCode.InternalServerError;

            if (Messaging.HasValidationFailures())
                return HttpStatusCode.BadRequest;

            return HttpStatusCode.OK;
        }
    }
}
