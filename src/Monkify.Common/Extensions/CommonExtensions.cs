using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monkify.Common.Extensions
{
    public static class CommonExtensions
    {
        public static T GetService<T>(this IServiceScope scope)
            => (T)scope.ServiceProvider.GetService(typeof(T));

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> value)
            => value == null || !value.Any();

        public static bool IsNullOrEmpty<T>(this ICollection<T> value)
            => value == null || value.Count == 0;
    }
}
