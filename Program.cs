using System.Globalization;
using PodereBot.Lib;
using PodereBot.Lib.Api;
using PodereBot.Services;
using PodereBot.Services.Hosted;

Console.Title = "Podere Bot";
Console.WriteLine("========== Podere Bot ==========\n");

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5050");
builder.Configuration.AddEnvironmentVariables();
builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);
builder.Services.AddCommands();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<ITemperatureDriver, MockTemperatureDriver>();
    builder.Services.AddSingleton<IPinDriver, MockPinDriver>();
}
else
{
    builder.Services.AddSingleton<ITemperatureDriver, OneWireEmbeddedTemperatureDriver>();
    if (string.IsNullOrEmpty(builder.Configuration.GetValue<string>("SerialPort")))
    {
        builder.Services.AddSingleton<IPinDriver, EmbeddedPinDriver>();
    }
    else
    {
        builder.Services.AddSingleton<IPinDriver, SerialPinDriver>();
    }
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<GateDriver>();
builder.Services.AddSingleton<Skin>();
builder.Services.AddSingleton<HeatingDriver>();
builder.Services.AddTransient<ConversationalResponder>();
builder.Services.AddSingleton<Database>();
builder.Services.AddHostedService<HeatingProgramDaemon>();
builder.Services.AddHostedService<BotHostedService>();

var app = builder.Build();

app.Use(Middleware.RestrictToLocalNetwork);
app.UseApiEndpoints();
app.UseSwagger();
app.UseSwaggerUI();

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
var loggerProvider = app.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerProvider.CreateLogger(string.Empty);
logger.LogInformation("env: [{env}]", app.Services.GetService<IHostEnvironment>()?.EnvironmentName);
await app.RunAsync();
