using CryptoExchange.Net.SharedApis;

namespace PodereBot.Lib.Trading.Strategy;

/// <summary>
/// A condition that is perpetually true until the <see cref="callback"/> is true
/// </summary>
public class PerpetualCondition(Predicate<SharedKline> callback) : AbstractCondition
{
    private readonly Predicate<SharedKline> callback = callback;

    public override void Tick(SharedKline frame)
    {
        IsSatisfied = callback(frame);
    }
}