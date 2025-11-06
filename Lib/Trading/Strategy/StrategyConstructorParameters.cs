namespace PodereBot.Lib.Trading.Strategy;

/// <summary>
/// Struct encapsulating, for comodity, all the parameters required to instantiate an <see cref="AbstractStrategy"/>
/// </summary>
public readonly struct StrategyConstructorParameters(Dictionary<string, float> parameters, ILogger? logger)
{
    public Dictionary<string, float> Parameters { get; init; } = parameters;


    public ILogger? Logger { get; init; } = logger;
}