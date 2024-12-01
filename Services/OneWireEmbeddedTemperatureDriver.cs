using System.Text.RegularExpressions;

namespace PodereBot.Services;

internal class OneWireEmbeddedTemperatureDriver(ILogger<OneWireEmbeddedTemperatureDriver> logger, IConfiguration configuration)
    : ITemperatureDriver
{
    private readonly ILogger logger = logger;

    private readonly string oneWireDevicesRoot = configuration.GetValue<string>("OneWire:DevicesRoot") ?? "/sys/bus/w1/devices";
    private readonly Dictionary<string, TemperatureReading> readings = [];

    public void PostTemperatureReading(TemperatureReading reading)
    {
        readings[reading.Id] = reading;
    }

    public async Task<float?> GetLocalTemperature(CancellationToken? token = null)
    {
        if (!Directory.Exists(oneWireDevicesRoot))
        {
            logger.LogWarning("unable to locate one wire devices root: {r}", oneWireDevicesRoot);
            return null;
        }
        var dirs = Directory.GetDirectories(oneWireDevicesRoot);
        string? match = dirs.FirstOrDefault(d => Regex.IsMatch(d, @"28-.*$", RegexOptions.IgnoreCase));
        if (match == null)
        {
            logger.LogWarning("no one wire sensors found");
            return null;
        }
        var source = await File.ReadAllTextAsync(Path.Join(match, "w1_slave"));
        var tempString = Regex.Match(source, @"t=(?<t>[0-9]+)", RegexOptions.IgnoreCase).Groups["t"]?.Value;
        if (!float.TryParse(tempString, out var temperature))
        {
            logger.LogWarning("error parsing w1_slave file");
            return null;
        }
        return temperature / 1000;
    }

    public List<TemperatureReading> GetExternalTemperatureReadings(TimeSpan? tolerance = null)
    {
        if (tolerance == null)
        {
            return [.. readings.Values];
        }
        return readings.Values.Where(r => DateTime.UtcNow.Subtract(r.Timestamp).Duration() < tolerance).ToList();
    }
}
