using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using MediatR;
using Microsoft.Azure.Amqp;
using Microsoft.Extensions.Configuration;
using Monkify.Common.Messaging;
using Monkify.Domain.Monkey.Events;
using Monkify.Infrastructure.Abstractions;
using Monkify.Infrastructure.Context;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure.Handlers.Sessions.Events
{
    public class StartMessageFlowForActiveSession : BaseNotificationHandler<SessionCreated>
    {
        public StartMessageFlowForActiveSession(MonkifyDbContext context, IMessaging messaging, IMediator mediator, IConfiguration configuration) : base(context, messaging, mediator, configuration)
        {
        }

        public override Task HandleRequest(SessionCreated notification, CancellationToken cancellationToken)
        {
            //Primeiro passo: Esperar x segundos

            //Segundo passo: validar se a aposta tem x jogadores

            //Terceiro passo: Se não tiver, mandar um objeto
            /*
                {
                    QueueStatus: 1 = Started, 2 - stopped due to players, 3 = ended
                    Message: string //not started due to aids
                }
             */

            //Quarto passo: mandar as letras
            ConnectToQueueChannel("Monkify", channel =>
            {
                CreateQueue(channel, notification.SessionId.ToString());

                for(int i = 1; i < 10000; i++)
                {
                    PublishMessage(channel, notification.SessionId.ToString(), i.ToString());
                }
            });

            //Terceiro passo: no fim, mandar um evento pra encerrar a fila
            /*
                {
                    QueueStatus: 1 = Started, 2 - stopped due to players, 3 = ended
                    Message: string //not started due to aids
                }
             */

            return Task.CompletedTask;
        }
    }
}
