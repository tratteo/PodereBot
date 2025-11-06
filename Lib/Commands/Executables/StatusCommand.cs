using System.Diagnostics;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace PodereBot.Lib.Commands;

[CommandMetadata(Key = "/status", Description = "Ti mando le statistiche del sistema")]
internal class StatusCommand(Skin skin, ILogger<StartCommand> logger, IConfiguration configuration) : Command(skin, logger, configuration)
{
    protected override async Task ExecuteInternal()
    {
        Process currentProc = Process.GetCurrentProcess();
        var runningTime = DateTime.Now - currentProc.StartTime;
        var memory = currentProc.PrivateMemorySize64 / 1E6;
        await Arguments.Client.SendMessage(
            Arguments.Message.Chat.Id,
            $"""
            <b>📊 Stato del sistema </b> 
            
            👉 Memory footprint: <b>{memory:0.000} MB</b> 
            👉 Running time: <b>{runningTime.Days}g {runningTime.Hours}h {runningTime.Minutes}m {runningTime.Seconds}s</b>        
            """,
            parseMode: ParseMode.Html,
            disableNotification: true
        );
    }
}
