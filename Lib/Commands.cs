using Microsoft.Extensions.DependencyInjection;
using PodereBot.Lib;
using Telegram.Bot;
using Telegram.Bot.Types;

static class Commands
{
    public static readonly List<BotCommandWrapper> commands =
    [
        new BotCommandWrapper(
            new BotCommand() { Command = "/openauto", Description = "Apri il cancello automatico" },
            async (args) =>
            {
                var gate = args.Services.GetRequiredService<GateDriver>();
                await gate.Open(GateDriver.GateId.automatic);
            }
        ),
        new BotCommandWrapper(
            new BotCommand() { Command = "/openped", Description = "Apri il cancello pedonale" },
            async (args) =>
            {
                var gate = args.Services.GetRequiredService<GateDriver>();
                await gate.Open(GateDriver.GateId.pedestrian);
            }
        ),
        new BotCommandWrapper(
            new BotCommand() { Command = "/pos", Description = "Ti mando la pos di casa" },
            async (args) =>
            {
                await args.Client.SendVenueAsync(
                    args.Message.Chat.Id,
                    41.49802515060315,
                    12.79789867806239,
                    "Podere 739 (canne libere)",
                    "Via del Valloncello 16"
                );
            }
        )
    ];
}
