namespace PodereBot.Services;

internal class MockTemperatureReader(Database db) : ITemperatureReader
{
    private readonly Random random = new();
    private double currentTemp = 18;

    public async Task<float?> GetTemperature(CancellationToken? token = null)
    {
        await Task.Delay(random.Next(250, 1000));
        currentTemp = db.Data.BoilerActive ? currentTemp + random.NextDouble() * 0.2 : currentTemp - random.NextDouble() * 0.2;
        return await Task.FromResult((float?)currentTemp);
    }
}
