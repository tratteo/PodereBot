using CryptoExchange.Net.SharedApis;
using PodereBot.Lib.Trading.Indicators;

namespace PodereBot.Lib.Trading.Strategy.Implemented;

public class MacdBasedStrategy : AbstractStrategy
{
    private readonly Macd macd;
    private readonly Ema ema;
    private readonly float riskRewardRatio;
    private readonly float stopLossRatio;
    private readonly int intervalTolerance;
    private float? lastHist;

    public MacdBasedStrategy(StrategyConstructorParameters parameters) : base(parameters)
    {
        var fastPeriod = parameters.Parameters.GetValueOrDefault("fastPeriod", 12);
        var slowPeriod = parameters.Parameters.GetValueOrDefault("slowPeriod", 26);
        var signalPeriod = parameters.Parameters.GetValueOrDefault("signalPeriod", 9);
        var emaPeriod = parameters.Parameters.GetValueOrDefault("emaPeriod", 200);
        intervalTolerance = (int)parameters.Parameters.GetValueOrDefault("intervalTolerance", 2);
        stopLossRatio = parameters.Parameters.GetValueOrDefault("stopLossRatio", 1.1F);
        riskRewardRatio = parameters.Parameters.GetValueOrDefault("riskRewardRatio", 1.5F);
        macd = new Macd((int)fastPeriod, (int)slowPeriod, (int)signalPeriod);
        ema = new Ema((int)emaPeriod);
        InjectConditions();
    }

    protected override Task Tick(SharedKline frame)
    {
        ema.ComputeNext((float)frame.ClosePrice);
        (lastHist, _, _) = macd.Last;
        macd.ComputeNext((float)frame.ClosePrice);
        return Task.CompletedTask;
    }


    protected override float GetStopLoss(SharedOrderSide side, SharedKline frame)
    {
        var emaV = (float)ema.Last!;
        var diff = Math.Abs((float)frame.ClosePrice - emaV);
        return side switch
        {
            SharedOrderSide.Buy => (float)frame.ClosePrice - (diff * stopLossRatio),
            SharedOrderSide.Sell => (float)frame.ClosePrice + (diff * stopLossRatio),
            _ => (float)frame.ClosePrice,
        };
    }

    protected override float GetTakeProfit(SharedOrderSide side, SharedKline frame)
    {
        var emaV = (float)ema.Last!;
        var diff = Math.Abs((float)frame.ClosePrice - emaV);
        return side switch
        {
            SharedOrderSide.Buy => (float)frame.ClosePrice + (diff * stopLossRatio * riskRewardRatio),
            SharedOrderSide.Sell => (float)frame.ClosePrice - (diff * stopLossRatio * riskRewardRatio),
            _ => (float)frame.ClosePrice,
        };
    }

    protected override void ResetState()
    {
        lastHist = null;
    }

    protected override IEnumerable<Indicator> GetIndicators() => [macd, ema];

    private void InjectConditions()
    {
        InjectLongConditions(
            new PerpetualCondition(c => ema.Last is not null && ema.Last < (float)c.ClosePrice),
            new EventCondition(c =>
                ema.Last is not null &&
                macd.Last is not (null, null, null) &&
                lastHist is not null &&
                // Cross up below the zero line
                lastHist < 0 && macd.Last.Item1 > 0 && macd.Last.Item2 < 0 && macd.Last.Item3 < 0,
                f => macd.Last.Item2 > 0 && macd.Last.Item3 > 0,
                intervalTolerance));

        InjectShortConditions(
            new PerpetualCondition(c => ema.Last is not null && ema.Last > (float)c.ClosePrice),
            new EventCondition(c =>
                ema.Last is not null &&
                macd.Last is not (null, null, null) &&
                lastHist is not null &&
                // Cross down above the zero line
                lastHist > 0 && macd.Last.Item1 < 0 && macd.Last.Item2 > 0 && macd.Last.Item3 > 0,
                f => macd.Last.Item2 < 0 && macd.Last.Item3 < 0,
                intervalTolerance));
    }
}