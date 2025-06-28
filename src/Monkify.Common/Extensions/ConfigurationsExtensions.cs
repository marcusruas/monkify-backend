using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Monkify.Common.Extensions
{
    public static class ConfigurationsExtensions
    {
        public static ConfigurationManager AddMonkifySettings(this ConfigurationManager configuration)
        {
            var settingsPath = Path.Combine(AppContext.BaseDirectory, "monkifysettings.json");

            configuration
                .AddJsonFile(settingsPath, optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            return configuration;
        }
    }
}
