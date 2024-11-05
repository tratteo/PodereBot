﻿using DotNetEnv.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PodereBot.Lib;
using PodereBot.Services;
using PodereBot.Services.Hosted;

Console.Title = "Podere Bot";
Console.WriteLine("========== Podere Bot ==========\n");

DotNetEnv.Env.Load();
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(
        (host, services) =>
        {
            services.Configure<ConsoleLifetimeOptions>(
                options => options.SuppressStatusMessages = true
            );
            services.AddCommands();

#if EMBEDDED_GPIO
            services.AddSingleton<IPinDriver, EmbeddedPinDriver>();
#else
            services.AddSingleton<IPinDriver, SerialPinDriver>();
#endif
            services.AddSingleton<GateDriverService>();
            services.AddSingleton<SkinService>();
            services.AddSingleton<DatabaseService>();

            services.AddHostedService<BotHostedService>();
        }
    )
    .ConfigureAppConfiguration(builder => builder.AddDotNetEnv())
    .Build();
var loggerProvider = host.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerProvider.CreateLogger(string.Empty);

await host.RunAsync();
