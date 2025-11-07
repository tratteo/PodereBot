using CryptoExchange.Net.SharedApis;

namespace PodereBot.Lib.Trading.Strategy;

public readonly struct StrategyActionReport()
{
    public required SharedOrderSide Side { get; init; }

    public required SharedKline ClosedKline { get; init; }
    public required float StopLoss { get; init; }
    public required float TakeProfit { get; init; }

}