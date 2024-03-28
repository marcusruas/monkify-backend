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
            var defaultConnectionString = GetQueueConnectionString("TerminalSessions");

            string hostName = "localhost"; // Ou o endereço do seu servidor RabbitMQ
            var random = new Random();
            string queueName = notification.SessionId.ToString(); // Nome da sua fila

            var factory = new ConnectionFactory() { HostName = hostName, UserName = "guest", Password = "guest" };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Declara a fila, se ela não existir, será criada
                channel.QueueDeclare(queue: queueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                Console.WriteLine($"Fila {queueName} está pronta para uso.");

                for (int i = 1; i <= 26; i++)
                {
                    var letraAtual = random.Next(1, 27) + 96;
                    var letraAtualChar = (char)letraAtual;

                    // Publica uma mensagem na fila
                    var body = Encoding.UTF8.GetBytes(letraAtualChar.ToString());

                    channel.BasicPublish(exchange: "",
                                         routingKey: queueName,
                                         basicProperties: null,
                                         body: body);
                }

                Console.WriteLine("Mensagem enviada com sucesso.");
            }

            return Task.CompletedTask;
        }
    }
}
