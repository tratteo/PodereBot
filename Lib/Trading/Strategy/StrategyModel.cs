
namespace PodereBot.Lib.Trading.Strategy;

/// <summary>
/// Model representing a strategy
/// </summary>
[Serializable]
public class StrategyModel
{
    public string Strategy { get; init; } = null!;

    public string Timeframe { get; init; } = null!;

    public Dictionary<string, object> Parameters { get; init; } = [];

}