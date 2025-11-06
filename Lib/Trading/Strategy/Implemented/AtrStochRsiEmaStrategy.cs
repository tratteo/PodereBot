using CryptoExchange.Net.SharedApis;
using Microsoft.Extensions.Logging;
using PodereBot.Lib.Trading.Indicators;
using PodereBot.Lib.Trading.Strategy;

namespace PodereBot.Lib.Trading.Strategy.Implemented;

public class AtrStochRsiEmaStrategy : AbstractStrategy
{
    private readonly Atr atr;
    private readonly Ema ema1;
    private readonly Ema ema2;
    private readonly Ema ema3;
    private readonly StochRsi stochRsi;
    private readonly float riskRewardRatio;
    private readonly float atrFactor;
    private readonly int intervalTolerance;
    private readonly int lowStochRsi;
    private readonly int highStochRsi;
    private float? lastStochK = null;
    private float? lastStochD = null;

    public AtrStochRsiEmaStrategy(StrategyConstructorParameters parameters) :
        base(parameters)
    {
        riskRewardRatio = parameters.Parameters.GetValueOrDefault("riskRewardRatio", 1F);
        atrFactor = parameters.Parameters.GetValueOrDefault("atrFactor", 1F);
        intervalTolerance = (int)parameters.Parameters.GetValueOrDefault("intervalTolerance", 2);
        atr = new Atr();
        ema1 = new Ema((int)parameters.Parameters.GetValueOrDefault("ema1Period", 15));
        ema2 = new Ema((int)parameters.Parameters.GetValueOrDefault("ema2Period", 50));
        ema3 = new Ema((int)parameters.Parameters.GetValueOrDefault("ema3Period", 125));
        lowStochRsi = (int)parameters.Parameters.GetValueOrDefault("lowStochRsi", 30);
        highStochRsi = (int)parameters.Parameters.GetValueOrDefault("highStochRsi", 70);
        stochRsi = new StochRsi();
        InjectConditions();
    }

    protected override IEnumerable<Indicator> GetIndicators() => [atr, ema1, ema2, ema3, stochRsi];

    protected override float GetStopLoss(SharedOrderSide side, SharedKline frame)
    {
        var ratio = (float)(atrFactor * atr.Last)!;
        return side switch
        {
            SharedOrderSide.Buy => (float)frame.ClosePrice - ratio,
            SharedOrderSide.Sell => (float)frame.ClosePrice + ratio,
            _ => (float)frame.ClosePrice,
        };
    }

    protected override float GetTakeProfit(SharedOrderSide side, SharedKline frame)
    {
        var ratio = (float)(atrFactor * atr.Last)!;
        return side switch
        {
            SharedOrderSide.Buy => (float)frame.ClosePrice + (ratio * riskRewardRatio),
            SharedOrderSide.Sell => (float)frame.ClosePrice - (ratio * riskRewardRatio),
            _ => (float)frame.ClosePrice,
        };
    }

    protected override void ResetState()
    {
        lastStochK = null;
        lastStochD = null;
    }

    protected override Task Tick(SharedKline frame)
    {
        atr.ComputeNext((float)frame.HighPrice, (float)frame.LowPrice, (float)frame.ClosePrice);
        ema1.ComputeNext((float)frame.ClosePrice);
        ema2.ComputeNext((float)frame.ClosePrice);
        ema3.ComputeNext((float)frame.ClosePrice);
        (lastStochK, lastStochD) = stochRsi.Last;
        stochRsi.ComputeNext((float)frame.ClosePrice);
        return Task.CompletedTask;
    }

    private void InjectConditions()
    {
        InjectLongConditions(
            new PerpetualCondition(f =>
                ema1.Last is not null && ema2.Last is not null && ema3.Last is not null &&
                ema3.Last < ema2.Last && ema2.Last < ema1.Last && ema1.Last < (float)f.ClosePrice),
            new EventCondition(f =>
            {
                var stoch = stochRsi.Last;
                return stoch is not (null, null) && lastStochK is not null && lastStochD is not null &&
                lastStochK < lastStochD && stoch.Item1 > stoch.Item2 && stoch.Item2 < lowStochRsi;
            }, f =>
            {
                var stoch = stochRsi.Last;
                return stoch.Item1 < stoch.Item2 || stoch.Item2 > lowStochRsi;
            }, intervalTolerance));

        InjectShortConditions(
            new PerpetualCondition(f =>
                ema1.Last is not null && ema2.Last is not null && ema3.Last is not null &&
                ema3.Last > ema2.Last && ema2.Last > ema1.Last && ema1.Last > (float)f.ClosePrice),
            new EventCondition(f =>
            {
                var stoch = stochRsi.Last;
                return stoch is not (null, null) && lastStochK is not null && lastStochD is not null &&
                lastStochK > lastStochD && stoch.Item1 < stoch.Item2 && stoch.Item2 > highStochRsi;
            }, f =>
            {
                var stoch = stochRsi.Last;
                return stoch.Item1 > stoch.Item2 || stoch.Item2 < highStochRsi;
            }, intervalTolerance));
    }
}