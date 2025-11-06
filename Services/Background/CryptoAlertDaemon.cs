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

internal class CryptoAlertDaemon(ILogger<CryptoAlertDaemon> logger, BotHostedService bot) : IHostedService
{

    readonly BinanceSocketClient client = new();
    private readonly AbstractStrategy strategy = new AtrStochRsiEmaStrategy(new StrategyConstructorParameters([], logger));
    private UpdateSubscription? subscription;
    private readonly KlineInterval interval = KlineInterval.FiveMinutes;
    private readonly string pair = "SOLUSDT";
    public SharedKline? LastKline { get; private set; }

    private async void OnKlineUpdate(DataEvent<IBinanceStreamKlineData> kline)
    {
        if (!kline.Data.Data.Final) return;
        var sharedKline = new SharedKline(kline.Data.Data.OpenTime, kline.Data.Data.ClosePrice, kline.Data.Data.HighPrice, kline.Data.Data.LowPrice, kline.Data.Data.OpenPrice, kline.Data.Data.Volume);
        LastKline = sharedKline;
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

    async Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow.AddSeconds(-(int)interval);
        var start = now.AddSeconds(-(int)interval * 50);
        var preload = await client.SpotApi.ExchangeData.GetKlinesAsync(pair, interval, startTime: start, endTime: now, ct: cancellationToken);
        if (!preload.Success)
        {

            logger.LogWarning("unable to preload to klines,type: {t}, code: {c}, message: {m}, description: {d}", preload.Error?.ErrorType, preload.Error?.ErrorCode, preload.Error?.Message, preload.Error?.ErrorDescription);
        }
        else
        {
            foreach (var kline in preload.Data.Result)
            {
                _ = await strategy.UpdateState(new SharedKline(kline.OpenTime, kline.ClosePrice, kline.HighPrice, kline.LowPrice, kline.OpenPrice, kline.Volume));
            }
            var last = preload.Data.Result.Last();
            LastKline = new SharedKline(last.OpenTime, last.ClosePrice, last.HighPrice, last.LowPrice, last.OpenPrice, last.Volume);
            logger.LogInformation("preloaded strategy with {l} klines, last: {l}", preload.Data.Result.Length, LastKline);
        }

        var sub = await client.SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync(pair, interval, OnKlineUpdate, ct: cancellationToken);
        if (!sub.Success)
        {
            logger.LogWarning("unable to subscribe to kline websocket");
        }
        else
        {
            subscription = sub.Data;
        }

    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (subscription != null)
        {
            return client.SpotApi.UnsubscribeAsync(subscription.Id);
        }
        return Task.CompletedTask;
    }
}
