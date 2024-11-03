using Microsoft.Extensions.Configuration;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace PodereBot.Lib.Commands;

internal class OpenPedestrianGateCommand(
    Skin skin,
    Database db,
    GateDriver gateDriver,
    IConfiguration configuration
) : Command(skin, configuration)
{
    private readonly Database db = db;
    private readonly GateDriver gateDriver = gateDriver;

    protected override async Task ExecuteInternal(CommandArguments arguments)
    {
        var admins = configuration.GetSection("Admins").Get<long[]>()?.ToList() ?? [];
        if (admins.Contains(arguments.Message.From!.Id))
        {
            await gateDriver.Open(GateDriver.GateId.pedestrian);
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
                await gateDriver.Open(GateDriver.GateId.pedestrian);
            }
        }
        await arguments.Client.SendChatActionAsync(arguments.Message.Chat.Id, ChatAction.Typing);
        await arguments.Client.SendTextMessageAsync(
            arguments.Message.Chat.Id,
            "Ho aperto il cancello pedonale 🐱"
        );
    }
}
