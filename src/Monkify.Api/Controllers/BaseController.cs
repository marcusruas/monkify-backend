using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Monkify.Common.Extensions;
using Monkify.Common.Notifications;
using Monkify.Results;
using System;
using System.Net;

namespace Monkify.Api.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        public BaseController(IMediator mediador, INotifications messaging)
        {
            Notifications = messaging;
            Mediator = mediador;
        }

        protected readonly INotifications Notifications;
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

            var defaultResult = new ApiResult<T>(result, Notifications.Notifications);
            return StatusCode(statusCode, defaultResult);
        }

        /// <summary>
        /// Formats the response of a handler into a standard API response
        /// </summary>
        protected IActionResult GenerateResultBody<T>(HttpStatusCode statusCode, T response)
        {
            var messaging = HttpContext.RequestServices.GetService<INotifications>();
            var defaultResult = new ApiResult<T>(response, messaging.Notifications);
            return StatusCode((int) statusCode, defaultResult);
        }

        private HttpStatusCode GetStatusCodeResult()
        {
            if (Notifications.HasErrors())
                return HttpStatusCode.InternalServerError;

            if (Notifications.HasValidationFailures())
                return HttpStatusCode.BadRequest;

            return HttpStatusCode.OK;
        }
    }
}
