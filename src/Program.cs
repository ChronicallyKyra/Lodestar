﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

using Mercurius.DBus;
using Mercurius.Configuration;
using Mercurius.API;
using Mercurius.Profiles;

namespace Mercurius {
    public class Program {
        public static async Task Main(string[] args) {
            ILogger logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#elif RELEASE
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();
            Log.Logger = logger;

            SettingsManager.Init(logger);
            APIs apis = new APIs();
            apis.Add(new ModrinthAPI(@"https://api.modrinth.com/v2/", new HttpClient(), logger));
            apis.Add(new CurseforgeAPI(@"https://api.curseforge.com/", new HttpClient(), logger));

            var builder = new HostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) => {
                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureServices((hostContext, services) => {
                services.AddSingleton<IHostedService, DbusHandler>(); 
                services.AddSingleton<APIs>(apis);      
                services.AddSingleton<ProfileManager>();
            })
            .UseSerilog(logger);

            await builder.RunConsoleAsync();
        }
    }
}