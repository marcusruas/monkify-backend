using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Common.Extensions
{
    public static class AspNetExtensions
    {
        public static T GetService<T>(this IServiceScope scope)
            => (T)scope.ServiceProvider.GetService(typeof(T));
    }
}
