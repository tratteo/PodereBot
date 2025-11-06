using CryptoExchange.Net.SharedApis;

namespace PodereBot.Lib.Trading.Strategy;

public readonly struct StrategyActionReport(SharedOrderSide side, float stopLoss, float takeProfit)
{
    public SharedOrderSide Side { get; init; } = side;

    public float StopLoss { get; init; } = stopLoss;
    public float TakeProfit { get; init; } = takeProfit;

}