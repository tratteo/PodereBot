using CryptoExchange.Net.SharedApis;
using PodereBot.Lib.Trading.Indicators;

namespace PodereBot.Lib.Trading.Strategy;

/// <summary>
///   Base class defining the behaviour of a strategy. All implemented strategy are encouraged to inherit from this class and to not
///   implement directly the interface <see cref="IStrategy"/>, unless extremely necessary, for example in order to define completely
///   different strategy logic
/// </summary>
public abstract class AbstractStrategy(StrategyConstructorParameters parameters) : IStrategy
{
    private readonly List<AbstractCondition> shortConditions = [];

    private readonly List<AbstractCondition> longConditions = [];
    private IEnumerable<Indicator>? indicators = null;

    protected ILogger? Logger { get; private set; } = parameters.Logger;

    /// <summary>
    ///   Reset the status of all conditions of the strategy
    /// </summary>
    public void Reset()
    {
        ResetState();
        indicators ??= GetIndicators();
        foreach (var i in indicators)
        {
            i.Reset();
        }
        foreach (var c in longConditions)
        {
            c.Reset();
        }
        foreach (var c in shortConditions)
        {
            c.Reset();
        }
    }

    /// <summary>
    ///   Update the internal state of the strategy, the kline MUST be closed, that is the final kline for the interval.
    /// </summary>
    /// <returns> </returns>
    public async Task<List<StrategyActionReport>> UpdateState(SharedKline kline)
    {
        List<StrategyActionReport> reports = [];
        await Tick(kline);
        foreach (var c in shortConditions)
        {
            c.Tick(kline);
        }
        foreach (var c in longConditions)
        {
            c.Tick(kline);
        }
        Logger?.LogInformation("kline ticked: {t}", kline.ToString());
        if (longConditions.Count > 0 && longConditions.All(c => c.IsSatisfied))
        {
            var side = SharedOrderSide.Buy;
            var stopLoss = GetStopLoss(side, kline);
            var takeProfit = GetTakeProfit(side, kline);
            reports.Add(new StrategyActionReport() { Side = SharedOrderSide.Buy, StopLoss = stopLoss, TakeProfit = takeProfit, ClosedKline = kline });
            longConditions.ForEach(c => c.Reset());
        }

        if (shortConditions.Count > 0 && shortConditions.All(c => c.IsSatisfied))
        {
            var side = SharedOrderSide.Sell;
            var stopLoss = GetStopLoss(side, kline);
            var takeProfit = GetTakeProfit(side, kline);
            reports.Add(new StrategyActionReport() { Side = SharedOrderSide.Sell, StopLoss = stopLoss, TakeProfit = takeProfit, ClosedKline = kline });
            shortConditions.ForEach(c => c.Reset());
        }
        return reports;
    }

    /// <summary>
    ///   Add short <see cref="AbstractCondition"/>
    /// </summary>
    protected void InjectShortConditions(params AbstractCondition[] conditions)
    {
        foreach (var c in conditions)
        {
            if (c is not null && !shortConditions.Contains(c))
            {
                shortConditions.Add(c);
            }
        }
    }

    /// <summary>
    ///   Reset the internal state of the strategy <i> (nullables, cached values ...) </i>
    /// </summary>
    protected abstract void ResetState();

    /// <summary>
    ///   Return the indicators of the strategy, so that they can be automatically reset in <see cref="Reset"/>
    /// </summary>
    /// <returns> </returns>
    protected abstract IEnumerable<Indicator> GetIndicators();

    /// <summary>
    ///   Get the value for the stop loss for a given side order
    /// </summary>
    /// <returns> </returns>
    protected abstract float GetStopLoss(SharedOrderSide side, SharedKline frame);

    /// <summary>
    ///   Get the value for the take profit for a given side order
    /// </summary>
    /// <returns> </returns>
    protected abstract float GetTakeProfit(SharedOrderSide side, SharedKline frame);

    /// <summary>
    ///   Add long <see cref="AbstractCondition"/>
    /// </summary>
    protected void InjectLongConditions(params AbstractCondition[] conditions)
    {
        foreach (var c in conditions)
        {
            if (c is not null && !longConditions.Contains(c))
            {
                longConditions.Add(c);
            }
        }
    }

    /// <summary>
    ///   Called each time a closed candle is received
    /// </summary>
    /// <param name="frame"> </param>
    protected virtual Task Tick(SharedKline frame) => Task.CompletedTask;

}