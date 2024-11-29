using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;

internal class HeatingDriver(
    ILogger<HeatingDriver> logger,
    IPinDriver pinDriver,
    IConfiguration configuration,
    ITemperatureReader temperatureReader,
    Database db
)
{
    private readonly ILogger<HeatingDriver> logger = logger;
    private readonly IPinDriver pinDriver = pinDriver;
    private readonly ITemperatureReader temperatureReader = temperatureReader;
    private readonly int heatingPin = configuration.GetValue<int>("Pins:Heating");

    public bool IsBoilerActive()
    {
        return pinDriver.DigitalRead(heatingPin) == 1;
    }

    public Task<float?> GetRoomTemperature(CancellationToken? cancellationToken = null)
    {
        return temperatureReader.GetTemperature(cancellationToken);
    }

    public void SwitchHeating(bool status)
    {
        if (status)
        {
            pinDriver.PinHigh(heatingPin);
            db.Edit(d => d.BoilerActive = true);
        }
        else
        {
            pinDriver.PinLow(heatingPin);
            db.Edit(d => d.BoilerActive = false);
        }
    }
}
