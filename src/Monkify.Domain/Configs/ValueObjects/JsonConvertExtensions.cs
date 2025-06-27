using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Monkify.Domain.Configs.ValueObjects
{
    public static class JsonConvertExtensions
    {
        /// <summary>
        /// Converts an object to its JSON representation using the <see cref="JsonConvert.SerializeObject(object?, JsonSerializerSettings)"/> method with the application's default configuration
        /// </summary>
        /// /// <param name="value">The object to serialize.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public static string AsJson(this object? obj)
            => JsonConvert.SerializeObject(obj, JsonSettings);

        public static JsonSerializerSettings JsonSettings =>
            new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ContractResolver = new DefaultContractResolver()
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            };
    }
}
