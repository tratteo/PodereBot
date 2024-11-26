internal interface ITemperatureReader
{
    public Task<float?> GetTemperature(CancellationToken? token = null);
}
