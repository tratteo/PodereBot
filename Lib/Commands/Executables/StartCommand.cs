using PodereBot.Services;
using Telegram.Bot;

namespace PodereBot.Lib.Commands;

[CommandMetadata(Key = "/start", Description = "❓")]
internal class StartCommand(Skin skin, ILogger<StartCommand> logger, IConfiguration configuration) : Command(skin, logger, configuration)
{
    protected override async Task ExecuteInternal()
    {
        await Arguments.Client.SendAsset(Arguments.Message, skin.Schema.Start);
        await Arguments.Client.SendMessage(
            Arguments.Message.Chat.Id,
            "Per i comandi usa il pannello accanto alla tastiera 🐈",
            disableNotification: true
        );
    }
}
