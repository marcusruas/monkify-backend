using Microsoft.Extensions.DependencyInjection;
using Monkify.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Infrastructure
{
    public static class DependencyInjection
    {
        public static void AddHandlers(this IServiceCollection services)
        {
            services.AddScoped<IMessaging, Messaging>();
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        }
    }
}
