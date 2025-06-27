using Microsoft.AspNetCore.Mvc.Filters;
using Monkify.Common.Exceptions;
using Monkify.Common.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Monkify.Api.Filters
{
    internal class ModelValidationFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if(!context.ModelState.IsValid)
            {
                var messaging = context.HttpContext.RequestServices.GetService<INotifications>();
                var modelErrors = context.ModelState.Values.SelectMany(x => x.Errors);

                foreach (var errors in modelErrors)
                    messaging.AddValidationFailureNotification(errors.ErrorMessage);

                if (messaging.HasValidationFailures())
                    throw new ValidationFailureException();
            }

            base.OnActionExecuting(context);
        }


    }
}
