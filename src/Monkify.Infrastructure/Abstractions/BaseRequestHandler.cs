using MediatR;
using Microsoft.Extensions.Logging;
using Monkify.Common.Exceptions;
using Monkify.Common.Messaging;
using Monkify.Infrastructure.Context;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers
{
    public abstract class BaseRequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        public BaseRequestHandler(MonkifyDbContext context, IMessaging messaging)
        {
            Context = context;
            Messaging = messaging;
        }

        protected readonly MonkifyDbContext Context;
        protected readonly IMessaging Messaging;

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

                var requestJson = JsonConvert.SerializeObject(request);
                Log.Error(ex, "The request to the handler {handler} failed. Request: {request}", GetType().Name, requestJson);
                throw;
            }
        }

        public abstract Task<TResponse> HandleRequest(TRequest request, CancellationToken cancellationToken);
    }
}
