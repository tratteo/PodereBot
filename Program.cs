using System.Globalization;
using DotNetEnv.Configuration;
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
    .ConfigureAppConfiguration(builder => builder.AddDotNetEnv())
    .ConfigureServices(
        (host, services) =>
        {
            services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);
            services.AddCommands();

            if (string.IsNullOrEmpty(host.Configuration.GetValue<string>("SerialPort")))
            {
                services.AddSingleton<IPinDriver, EmbeddedPinDriver>();
            }
            else
            {
                services.AddSingleton<IPinDriver, SerialPinDriver>();
            }
            services.AddSingleton<ITemperatureReader, MockTemperatureReader>();
            services.AddSingleton<GateDriver>();
            services.AddSingleton<Skin>();
            services.AddTransient<ConversationalResponder>();
            services.AddSingleton<Database>();
            services.AddHostedService<HeatingDaemon>();
            services.AddHostedService<BotHostedService>();
        }
    )
    .Build();
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
var loggerProvider = host.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerProvider.CreateLogger(string.Empty);
await host.RunAsync();
