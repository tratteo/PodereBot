using System.Text;
using Microsoft.OpenApi.Extensions;
using Newtonsoft.Json;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace PodereBot.Lib.Commands;

[CommandMetadata(Key = "/stratsnp", Description = "Ti mando lo snapshot della strategia di trading corrente")]
internal class TradingStrategySnapshotCommand(Skin skin, ILogger<StartCommand> logger, IConfiguration configuration, CryptoAlertDaemon alertDaemon) : Command(skin, logger, configuration)
{
    protected override async Task ExecuteInternal()
    {
        logger.LogInformation("kline: {k}", alertDaemon.LastKline);
        var str = new StringBuilder($"""
            <b>🔎 Snapshot Strategia</b>

            Strategy: <b>AtrStochRsiEmaStrategy</b>
            Timeframe: <b>{alertDaemon.Interval.GetDisplayName()}</b>
            Pair: <b>{alertDaemon.Pair}</b>

            <b>Last Kline</b>
            <pre language='json'>{JsonConvert.SerializeObject(alertDaemon.LastKline, Formatting.Indented)}</pre>
            """);

        await Arguments.Client.SendMessage(
            Arguments.Message.Chat.Id,
            str.ToString(),
            parseMode: ParseMode.Html,
            disableNotification: true
        );
    }
}
