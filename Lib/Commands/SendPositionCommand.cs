using Microsoft.Extensions.Configuration;
using Telegram.Bot;

namespace PodereBot.Lib.Commands;

internal class SendPositionCommand(IConfiguration configuration) : Command(configuration)
{
    protected override async Task ExecuteInternal(CommandArguments arguments)
    {
        await arguments.Client.SendVenueAsync(
            arguments.Message.Chat.Id,
            41.49802515060315,
            12.79789867806239,
            "Podere 739 (canne libere)",
            "Via del Valloncello 16"
        );
    }
}