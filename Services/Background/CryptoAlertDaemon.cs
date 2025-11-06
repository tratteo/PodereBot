using System.Text;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.SharedApis;
using PodereBot.Lib;
using PodereBot.Lib.Trading.Strategy;
using PodereBot.Lib.Trading.Strategy.Implemented;
using PodereBot.Services.Hosted;

internal class CryptoAlertDaemon(ILogger<CryptoAlertDaemon> logger, BotHostedService bot) : BackgroundService
{

    readonly BinanceSocketClient client = new();
    private readonly AbstractStrategy strategy = new AtrStochRsiEmaStrategy(new StrategyConstructorParameters([], logger));

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return client.SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync("SOLUSDT", KlineInterval.OneMinute, OnKlineUpdate, ct: stoppingToken);
    }

    private async void OnKlineUpdate(DataEvent<IBinanceStreamKlineData> kline)
    {
        logger.LogDebug("kline received, timestamp: {t}", kline.ReceiveTime);
        if (!kline.Data.Data.Final) return;
        var sharedKline = new SharedKline(kline.Data.Data.OpenTime, kline.Data.Data.ClosePrice, kline.Data.Data.HighPrice, kline.Data.Data.LowPrice, kline.Data.Data.OpenPrice, kline.Data.Data.Volume);
        var reports = await strategy.UpdateState(sharedKline);
        if (reports.Count > 0)
        {
            var str = new StringBuilder($"""
            <b>💸 Crypto Position Alert</b>

            Strategy: <b>AtrStochRsiEmaStrategy</b>
            Timeframe: <b>1m</b>
            Pair: <b>SOL/USDT</b>
            """);
            foreach (var action in reports)
            {
                if (action.Side == SharedOrderSide.Buy)
                {
                    str.AppendLine($"\nSignal: <b>🔼 Buy</b>");
                }
                else
                {
                    str.AppendLine($"\nSignal: <b>🔽 Sell</b>");
                }
                str.AppendLine($"SL: <b>{action.StopLoss}</b>");
                str.AppendLine($"TP: <b>{action.TakeProfit}</b>\n");
            }
            await bot.Client.NotifyOwners(str.ToString(), logger);
        }
    }

}
