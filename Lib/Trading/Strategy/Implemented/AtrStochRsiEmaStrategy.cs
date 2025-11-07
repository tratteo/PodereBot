using CryptoExchange.Net.SharedApis;
using PodereBot.Lib.Trading.Indicators;

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
        riskRewardRatio = parameters.Parameters.GetValueOrDefault("riskRewardRatio", 2F);
        atrFactor = parameters.Parameters.GetValueOrDefault("atrFactor", 2F);
        intervalTolerance = (int)parameters.Parameters.GetValueOrDefault("intervalTolerance", 2);
        atr = new Atr();
        ema1 = new Ema((int)parameters.Parameters.GetValueOrDefault("ema1Period", 9));
        ema2 = new Ema((int)parameters.Parameters.GetValueOrDefault("ema2Period", 21));
        ema3 = new Ema((int)parameters.Parameters.GetValueOrDefault("ema3Period", 55));
        lowStochRsi = (int)parameters.Parameters.GetValueOrDefault("lowStochRsi", 25);
        highStochRsi = (int)parameters.Parameters.GetValueOrDefault("highStochRsi", 75);
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
                var (stochK, stochD) = stochRsi.Last;
                return stochRsi.Last is not (null, null) && lastStochK is not null && lastStochD is not null &&
                lastStochK < lastStochD && stochK > stochD && stochD < lowStochRsi;
            }, f =>
            {
                var (stochK, stochD) = stochRsi.Last;
                return stochK < stochD || stochD > lowStochRsi;
            }, intervalTolerance));

        InjectShortConditions(
            new PerpetualCondition(f =>
                ema1.Last is not null && ema2.Last is not null && ema3.Last is not null &&
                ema3.Last > ema2.Last && ema2.Last > ema1.Last && ema1.Last > (float)f.ClosePrice),
            new EventCondition(f =>
            {
                var (stochK, stochD) = stochRsi.Last;
                return stochRsi.Last is not (null, null) && lastStochK is not null && lastStochD is not null &&
                lastStochK > lastStochD && stochK < stochD && stochD > highStochRsi;
            }, f =>
            {
                var (stochK, stochD) = stochRsi.Last;
                return stochK > stochD || stochD < highStochRsi;
            }, intervalTolerance));
    }
}