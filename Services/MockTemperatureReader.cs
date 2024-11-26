using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PodereBot.Services;

internal class MockTemperatureReader(ILogger<MockTemperatureReader> logger, Database db) : ITemperatureReader
{
    private readonly ILogger logger = logger;
    private readonly Database db = db;
    private readonly Random random = new();
    private double currentTemp = 18;

    public Task<float?> GetTemperature(CancellationToken? token = null)
    {
        currentTemp = db.Data.HeatingActive ? currentTemp + random.NextDouble() * 0.2 : currentTemp - random.NextDouble() * 0.2;
        return Task.FromResult((float?)currentTemp);
    }
}
