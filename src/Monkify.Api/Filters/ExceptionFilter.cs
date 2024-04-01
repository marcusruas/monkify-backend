using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Monkify.Common.Exceptions;
using Monkify.Common.Messaging;
using Monkify.Results;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Monkify.Api.Filters;

internal class ExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        SetStatusCodeResult(context);
        GenerateResult(context);
    }

    private void SetStatusCodeResult(ExceptionContext context)
    {
        int statusCodeResult;

        if (context.Exception is ControlledException exception)
            statusCodeResult = exception.StatusCodeResult;
        else
            statusCodeResult = (int)HttpStatusCode.InternalServerError;

        context.HttpContext.Response.StatusCode = statusCodeResult;
    }

    private void GenerateResult(ExceptionContext context)
    {
        string errorMessage;

        if (context.Exception is ControlledException exception)
            errorMessage = exception.DefaultMessage;
        else
            errorMessage = "Your request could not be processed. Please check the data and try again.";

        var messaging = context.HttpContext.RequestServices.GetService<IMessaging>();
        var model =  new ApiResult<string>(errorMessage, messaging.Messages);

        context.Result = new ObjectResult(model);
    }
}
