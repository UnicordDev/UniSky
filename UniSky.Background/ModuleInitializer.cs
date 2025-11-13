using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniSky.Services;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class ModuleInitializerAttribute : Attribute { }
}

namespace UniSky.Background
{
    internal class Static
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            var collection = new ServiceCollection();
            collection.AddLogging(c => c.AddDebug()
                .SetMinimumLevel(LogLevel.Trace));

            collection.AddSingleton<IProtocolService, ProtocolService>();
            collection.AddSingleton<ISettingsService, SettingsService>();
            collection.AddSingleton<ITypedSettings, TypedSettingsService>();

            collection.AddTransient<ILoginService, LoginService>();
            collection.AddTransient<ISessionService, SessionService>();
            collection.AddTransient<IBadgeService, BadgeService>();

            ServiceContainer.Default.ConfigureServices(collection.BuildServiceProvider());
        }
    }
}
