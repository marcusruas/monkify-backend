using MediatR;
using Microsoft.Extensions.Logging;
using Monkify.Common.Exceptions;
using Monkify.Common.Notifications;
using Monkify.Infrastructure.Context;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseRequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        public BaseRequestHandler(MonkifyDbContext context, INotifications messaging)
        {
            Context = context;
            Messaging = messaging;
        }

        protected readonly MonkifyDbContext Context;
        protected readonly INotifications Messaging;

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
        {
            try
            {
                return await HandleRequest(request, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex is MessagingException)
                    throw;

                Log.Error(ex, "The request to the handler {handler} failed. Request: {request}", GetType().Name, request.AsJson());
                throw;
            }
        }

        public abstract Task<TResponse> HandleRequest(TRequest request, CancellationToken cancellationToken);
    }
}
