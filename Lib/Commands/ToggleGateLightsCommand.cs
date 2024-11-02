using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PodereBot.Lib.Commands;

internal class ToggleGatesLightCommand(
    GateDriver gateDriver,
    Skin skin,
    Database db,
    IConfiguration configuration,
    ILogger<ToggleGatesLightCommand> logger
) : Command(skin, configuration)
{
    private readonly GateDriver gateDriver = gateDriver;
    private readonly Database db = db;
    private readonly ILogger<ToggleGatesLightCommand> logger = logger;

    protected override async Task ExecuteInternal(CommandArguments arguments)
    {
        await gateDriver.ToggleLights();
        await arguments.Client.SendAssetAsync(arguments.Message.Chat.Id, skin.Schema.GatesLight);
        await arguments.Client.SendChatActionAsync(arguments.Message.Chat.Id, ChatAction.Typing);
        await arguments.Client.SendTextMessageAsync(
            arguments.Message.Chat.Id,
            "Ho acceso o spento le luci del cancello. Io non posso sapere in che stato sono, vai a guardare 😿"
        );
    }
}
