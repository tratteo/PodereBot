using Microsoft.Extensions.Configuration;
using PodereBot.Services;
using Telegram.Bot;

namespace PodereBot.Lib.Commands;

internal class SendPositionCommand(SkinService skin, IConfiguration configuration)
    : Command(skin, configuration)
{
    protected override async Task ExecuteInternal(CommandArguments arguments)
    {
        await arguments.Client.SendVenue(
            arguments.Message.Chat.Id,
            41.49802515060315,
            12.79789867806239,
            "Podere 739 (canne libere 🪴)",
            "Via del Valloncello 16"
        );
    }
}
