using Microsoft.Extensions.Configuration;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PodereBot.Lib.Commands;

internal class StartCommand(Skin skin, IConfiguration configuration) : Command(skin, configuration)
{
    protected override async Task ExecuteInternal(CommandArguments arguments)
    {
        await arguments.Client.SendAssetAsync(
            arguments.Message,
            skin.Schema.Start,
            caption: "Per i comandi usa il pannello accanto alla tastiera 🐈"
        );
    }
}
