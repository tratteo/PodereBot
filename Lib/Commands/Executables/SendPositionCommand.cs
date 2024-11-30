using PodereBot.Services;
using Telegram.Bot;

namespace PodereBot.Lib.Commands;

[CommandMetadata(Key = "/sendpos", Description = "Ti mando la posizione di casa 📍")]
internal class SendPositionCommand(Skin skin, ILogger<SendPositionCommand> logger, IConfiguration configuration)
    : Command(skin, logger, configuration)
{
    protected override async Task ExecuteInternal()
    {
        await Arguments.Client.SendVenue(
            Arguments.Message.Chat.Id,
            41.49802515060315,
            12.79789867806239,
            "Podere 739 (canne libere 🪴)",
            "Via del Valloncello 16"
        );
    }
}
