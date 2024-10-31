using DotNetEnv.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PodereBot.Services;

Console.Title = "Podere Bot";
Console.WriteLine("========== Podere Bot ==========\n");

DotNetEnv.Env.Load();
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.Configure<ConsoleLifetimeOptions>(
            options => options.SuppressStatusMessages = true
        );
        services.AddSingleton<Serial>();
        services.AddSingleton<GateDriver>();
        services.AddHostedService<BotHostedService>();
    })
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddDotNetEnv();
    })
    .Build();
var loggerProvider = host.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerProvider.CreateLogger(string.Empty);

await host.RunAsync();