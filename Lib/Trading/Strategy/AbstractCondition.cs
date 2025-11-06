using CryptoExchange.Net.SharedApis;

namespace PodereBot.Lib.Trading.Strategy;

public abstract class AbstractCondition
{
    public bool IsSatisfied { get; protected set; }

    protected AbstractCondition()
    {
    }

    /// <summary>
    ///   Called every closed candle to update the state of the condition
    /// </summary>
    /// <param name="frame"> </param>
    public abstract void Tick(SharedKline frame);

    public virtual void Reset() => IsSatisfied = false;
}