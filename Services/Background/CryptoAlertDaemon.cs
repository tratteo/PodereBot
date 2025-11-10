using System.Text;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.SharedApis;
using PodereBot.Lib;
using PodereBot.Lib.Trading.Strategy;
using PodereBot.Lib.Trading.Strategy.Implemented;
using PodereBot.Services;
using PodereBot.Services.Hosted;

internal class CryptoAlertDaemon(ILogger<CryptoAlertDaemon> logger, Database db, BotHostedService bot) : BackgroundService
{
    readonly BinanceSocketClient client = new((opt) => opt.RequestTimeout = TimeSpan.FromSeconds(30));
    private readonly AbstractStrategy strategy = new AtrStochRsiEmaStrategy(new StrategyConstructorParameters([], logger));
    private UpdateSubscription? subscription;
    public KlineInterval Interval { get; init; } = KlineInterval.OneHour;
    public string Pair { get; init; } = "SOLUSDC";
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
            Timeframe: <b>{Interval.GetMapName()}</b>
            Pair: <b>{Pair}</b>
            """);
            str.AppendLine("");
            foreach (var action in reports)
            {
                float tpPct;
                float slPct;
                if (action.Side == SharedOrderSide.Buy)
                {
                    tpPct = 100 * ((action.TakeProfit / (float)action.ClosedKline.ClosePrice) - 1);
                    slPct = 100 * (1 - (action.StopLoss / (float)action.ClosedKline.ClosePrice));
                }
                else
                {
                    tpPct = 100 * (1 - (action.TakeProfit / (float)action.ClosedKline.ClosePrice));
                    slPct = 100 * ((action.StopLoss / (float)action.ClosedKline.ClosePrice) - 1);
                }
                str.AppendLine($"""

                <b>{(action.Side == SharedOrderSide.Buy ? "🟢 Buy" : "🔴 Sell")} Signal</b>
                <b>Entry Kline </b>
                Price: <b>{action.ClosedKline.ClosePrice:0.000}</b>
                Opened at: <b>{action.ClosedKline.OpenTime} UTC</b>
                Closed at: <b>{action.ClosedKline.OpenTime.AddSeconds((int)Interval)} UTC</b>
                
                Take profit: <b>{action.TakeProfit:0.000} ({tpPct:0.00}%)</b>
                Stop loss: <b>{action.StopLoss:0.000} ({slPct:0.00}%)</b>
                """);
            }
            await bot.Client.NotifyUsers(str.ToString(), db.Data.TradingAlertsSubscriptions, logger);
        }
    }

    private async void ConnectRoutine(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow.AddSeconds(-(int)Interval);
        var start = now.AddSeconds(-(int)Interval * 50);
        var attemptMs = 5000;
        while (true)
        {
            var preload = await client.SpotApi.ExchangeData.GetKlinesAsync(Pair, Interval, startTime: start, endTime: now, ct: cancellationToken);
            if (!preload.Success)
            {
                logger.LogWarning("unable to preload to klines, error: {t}, code: {c}, mesage: {m}, description: {d}, ex: {e}", preload.Error?.ErrorType, preload.Error?.ErrorCode, preload.Error?.Message, preload.Error?.ErrorDescription, preload.Error?.Exception);
                await Task.Delay(attemptMs, cancellationToken);
                continue;
            }

            foreach (var kline in preload.Data.Result)
            {
                _ = await strategy.UpdateState(new SharedKline(kline.OpenTime, kline.ClosePrice, kline.HighPrice, kline.LowPrice, kline.OpenPrice, kline.Volume));
            }
            var last = preload.Data.Result.Last();
            LastKline = new SharedKline(last.OpenTime, last.ClosePrice, last.HighPrice, last.LowPrice, last.OpenPrice, last.Volume);
            logger.LogInformation("preloaded strategy with {l} klines, last: {l}", preload.Data.Result.Length, LastKline);
            break;
        }

        while (true)
        {
            var sub = await client.SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync(Pair, Interval, OnKlineUpdate, ct: cancellationToken);
            if (!sub.Success)
            {
                logger.LogWarning("unable to subscribe to kline websocket, error: {t}, code: {c}, mesage: {m}, description: {d}, ex: {e}", sub.Error?.ErrorType, sub.Error?.ErrorCode, sub.Error?.Message, sub.Error?.ErrorDescription, sub.Error?.Exception);
                await Task.Delay(attemptMs, cancellationToken);
                continue;
            }
            subscription = sub.Data;
            logger.LogInformation("subscribed to klines websocket");
            break;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        ConnectRoutine(cancellationToken);
    }


    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (subscription != null)
        {
            return client.SpotApi.UnsubscribeAsync(subscription.Id);
        }
        return Task.CompletedTask;
    }
}
