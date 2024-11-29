using System.Globalization;
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
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddEnvironmentVariables();
    })
    .ConfigureServices(
        (host, services) =>
        {
            services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);
            services.AddCommands();
            if (host.HostingEnvironment.IsDevelopment())
            {
                services.AddSingleton<ITemperatureReader, MockTemperatureReader>();
                services.AddSingleton<IPinDriver, MockPinDriver>();
            }
            else
            {
                services.AddSingleton<ITemperatureReader, OneWireEmbeddedTemperatureReader>();
                if (string.IsNullOrEmpty(host.Configuration.GetValue<string>("SerialPort")))
                {
                    services.AddSingleton<IPinDriver, EmbeddedPinDriver>();
                }
                else
                {
                    services.AddSingleton<IPinDriver, SerialPinDriver>();
                }
            }

            services.AddSingleton<GateDriver>();
            services.AddSingleton<Skin>();
            services.AddSingleton<HeatingDriver>();
            services.AddTransient<ConversationalResponder>();
            services.AddSingleton<Database>();
            services.AddHostedService<HeatingProgramDaemon>();
            services.AddHostedService<BotHostedService>();
        }
    )
    .Build();
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
var loggerProvider = host.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerProvider.CreateLogger(string.Empty);
logger.LogInformation("env: [{env}]", host.Services.GetService<IHostEnvironment>()?.EnvironmentName);
await host.RunAsync();
