internal record TemperatureReading
{
    public required float Temperature { get; init; }

    public required DateTime Timestamp { get; init; }

    public required string Id { get; init; }
    public required string Location { get; init; }
}

internal interface ITemperatureDriver
{
    public Task<float?> GetLocalTemperature(CancellationToken? token = null);
    public List<TemperatureReading> GetExternalTemperatureReadings(TimeSpan? tolerance = null);
    public void PostTemperatureReading(TemperatureReading reading);
}
