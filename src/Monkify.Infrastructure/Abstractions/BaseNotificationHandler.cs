using MediatR;
using Microsoft.Extensions.Configuration;
using Monkify.Common.Exceptions;
using Monkify.Common.Messaging;
using Monkify.Common.Models;
using Monkify.Domain.Configs.Entities;
using Monkify.Infrastructure.Context;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Abstractions
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseNotificationHandler<TNotification> : INotificationHandler<TNotification> where TNotification : INotification
    {
        public async Task Handle(TNotification notification, CancellationToken cancellationToken)
        {
            try
            {
                await HandleRequest(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "The request to the event handler {handler} failed. Request: {request}", GetType().Name, notification.AsJson());
            }
        }

        public abstract Task HandleRequest(TNotification notification, CancellationToken cancellationToken);
    }
}
