using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PodereBot.Lib.Commands;

internal class StartCommand(IConfiguration configuration) : Command(configuration)
{
    protected override async Task ExecuteInternal(CommandArguments arguments)
    {
        await arguments.Client.SendAnimationAsync(
            arguments.Message.Chat.Id,
            InputFile.FromString(
                "https://media1.tenor.com/m/w8kAoMlhgjQAAAAd/so-it-begins-raining.gif"
            ),
            caption: "Per i comandi usa il pannello accanto alla tastiera.\n"
        );
    }
}
