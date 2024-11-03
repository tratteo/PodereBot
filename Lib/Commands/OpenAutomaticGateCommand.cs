using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PodereBot.Lib.Commands;

internal class OpenAutomaticGateCommand(
    Skin skin,
    GateDriver gateDriver,
    Database db,
    IConfiguration configuration,
    ILogger<OpenAutomaticGateCommand> logger
) : Command(skin, configuration)
{
    private readonly GateDriver gateDriver = gateDriver;
    private readonly Database db = db;
    private readonly ILogger<OpenAutomaticGateCommand> logger = logger;

    protected override async Task ExecuteInternal(CommandArguments arguments)
    {
        var admins = configuration.GetSection("Admins").Get<long[]>()?.ToList() ?? [];
        if (admins.Contains(arguments.Message.From!.Id))
        {
            await gateDriver.Open(GateDriver.GateId.automatic);
        }
        else
        {
            var gatesOpen =
                db.Data.GatesOpenAccessExpirationDate != null
                || db.Data.GatesOpenAccessExpirationDate > DateTime.Now;
            if (!gatesOpen)
            {
                await arguments.Client.SendAssetAsync(
                    arguments.Message,
                    skin.Schema.Forbidden,
                    caption: "I cancelli sono bloccati al momento ❌"
                );
                return;
            }
            else
            {
                await gateDriver.Open(GateDriver.GateId.automatic);
            }
        }

        await arguments.Client.SendChatActionAsync(arguments.Message.Chat.Id, ChatAction.Typing);
        await arguments.Client.SendTextMessageAsync(
            arguments.Message.Chat.Id,
            "Ho aperto il cancello automatico 🐱"
        );
    }
}
