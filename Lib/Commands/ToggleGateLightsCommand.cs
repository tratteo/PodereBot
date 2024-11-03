using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PodereBot.Lib.Commands;

internal class ToggleGatesLightCommand(
    GateDriverService gateDriver,
    SkinService skin,
    DatabaseService db,
    IConfiguration configuration,
    ILogger<ToggleGatesLightCommand> logger
) : Command(skin, configuration)
{
    private readonly GateDriverService gateDriver = gateDriver;
    private readonly DatabaseService db = db;
    private readonly ILogger<ToggleGatesLightCommand> logger = logger;

    protected override async Task ExecuteInternal(CommandArguments arguments)
    {
        await arguments.Client.SendChatActionAsync(arguments.Message.Chat.Id, ChatAction.Typing);
        await gateDriver.ToggleLights();
        await arguments.Client.SendAssetAsync(arguments.Message, skin.Schema.GatesLight);
        await arguments.Client.SendTextMessageAsync(
            arguments.Message.Chat.Id,
            "Ho acceso o spento le luci del cancello. Io non posso sapere in che stato sono, vai a guardare 😿"
        );
    }
}
