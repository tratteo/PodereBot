using System.Reflection;
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
    private readonly Skin skin;
    private readonly List<CommandRegistryKey> commandEntries;

    public BotHostedService(ILogger<BotHostedService> logger, IConfiguration configuration, IServiceProvider services, Skin skin)
    {
        this.skin = skin;
        this.logger = logger;
        this.services = services;
        client = new TelegramBotClient(configuration.GetValue<string>("TELEGRAM_API_KEY")!, cancellationToken: cancellationToken.Token);
        commandEntries = Assembly.GetExecutingAssembly().GetCommands();
        logger.LogInformation("registered {c} commands", commandEntries.Count);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var me = await client.GetMe(cancellationToken: cancellationToken);

        await client.SetMyDescription($"Che vuoi?", cancellationToken: cancellationToken);
        await client.SetMyShortDescription($"Che vuoi?", cancellationToken: cancellationToken);
        await client.SetMyCommands(
            commandEntries.ConvertAll(c => new BotCommand() { Command = c.metadata.Key, Description = c.metadata.Description }),
            cancellationToken: cancellationToken
        );
        await client.NotifyOwners("Presente 😼", logger);
        client.OnMessage += OnMessage;
        logger.LogInformation("@{u} is running", me.Username);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await client.NotifyOwners("Torno a dormire 🌙", logger);
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
        var match = commandEntries.FirstOrDefault(c => c.metadata.Key == msg.Text);
        if (match != null)
        {
            try
            {
                var cmdService = (Command?)services.GetService(match.commandType);
                if (cmdService == null)
                {
                    logger.LogWarning("unable to retrieve command service of type {t}", match.commandType);
                    return;
                }

                await cmdService.Execute(
                    new CommandArguments()
                    {
                        Client = client,
                        Message = msg,
                        Admin = match.metadata.Admin
                    }
                );
            }
            catch (Exception ex)
            {
                logger.LogError("error executing command [{c}]: {e}", match.metadata.Key, ex);
            }
        }
        else
        { // This is a normal text message
            var responder = services.GetService<ConversationalResponder>();
            if (responder == null)
                return;
            var _ = responder.Process(client, msg);
        }
    }
}
