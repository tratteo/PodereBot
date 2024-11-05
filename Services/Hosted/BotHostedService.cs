using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PodereBot.Lib;
using PodereBot.Lib.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PodereBot.Services.Hosted;

internal class BotHostedService : IHostedService
{
    private readonly ILogger logger;
    private readonly IServiceProvider services;
    private readonly CancellationTokenSource cancellationToken = new();
    private readonly TelegramBotClient client;
    private readonly GateDriver gate;
    private readonly Skin skin;

    public BotHostedService(
        ILogger<BotHostedService> logger,
        IConfiguration configuration,
        IServiceProvider services,
        GateDriver gate,
        Skin skin
    )
    {
        this.gate = gate;
        this.skin = skin;
        this.logger = logger;
        this.services = services;
        client = new TelegramBotClient(
            configuration.GetValue<string>("TELEGRAM_API_KEY")!,
            cancellationToken: cancellationToken.Token
        );
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var me = await client.GetMe(cancellationToken: cancellationToken);
        var skinData =
            $"Skin caricata: {skin.Schema.Metadata.Name} by {skin.Schema.Metadata.Author}";
        await client.SetMyDescription(
            $"Che vuoi? \n\n{skinData}",
            cancellationToken: cancellationToken
        );
        await client.SetMyShortDescription(
            $"Che vuoi? \n\n{skinData}",
            cancellationToken: cancellationToken
        );
        await client.SetMyCommands(
            Registry.commands.ConvertAll(
                c => new BotCommand() { Command = c.command, Description = c.description }
            ),
            cancellationToken: cancellationToken
        );
        await client.SendMessage(962154266, "Presente 😼", cancellationToken: cancellationToken);
        client.OnMessage += OnMessage;
        logger.LogInformation("@{u} is running", me.Username);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await client.SendMessage(
            962154266,
            "Torno a dormire 🌙",
            cancellationToken: cancellationToken
        );
        this.cancellationToken.Cancel();
        await Task.CompletedTask;
    }

    private async Task OnMessage(Message msg, UpdateType type)
    {
        if (DateTime.Now.ToUniversalTime() - msg.Date.ToUniversalTime() > TimeSpan.FromSeconds(30))
        {
            return;
        }
        logger.LogInformation("Received {type} '{t}' in {c}", type, msg.Text, msg.Chat);
        // logger.LogDebug("raw message: {r}", JsonConvert.SerializeObject(msg));
        var match = Registry.commands.FirstOrDefault(c => c.command == msg.Text);
        if (match == null)
        {
            return;
        }
        try
        {
            var cmdService = (Command?)services.GetService(match.commandType);
            if (cmdService == null)
            {
                logger.LogWarning(
                    "unable to retrieve command service of type {t}",
                    match.commandType
                );
                return;
            }

            await cmdService.Execute(
                new CommandArguments()
                {
                    Client = client,
                    Message = msg,
                    Admin = match.admin
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError("error executing command [{c}]: {e}", match.command, ex);
        }
    }
}
