using CryptoExchange.Net.SharedApis;

namespace PodereBot.Lib.Trading.Strategy;

/// <summary>
/// Basic interface for a strategy. It is highly recommende to inherit the already implemented <see cref="AbstractStrategy"/> 
/// unless strictly necessary for completely new strategies
/// </summary>
public interface IStrategy
{

    /// <summary>
    ///   Update the internal state of the strategy
    /// </summary>
    /// <returns> </returns>
    Task<List<StrategyActionReport>> UpdateState(SharedKline kline);

    /// <summary>
    ///   Reset the status of all conditions of the strategy
    /// </summary>
    void Reset();
}