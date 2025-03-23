using PodereBot.Lib;
using PodereBot.Services;
using PodereBot.Services.Hosted;

internal class HeatingDriver(
    ILogger<HeatingDriver> logger,
    IPinDriver pinDriver,
    IConfiguration configuration,
    ITemperatureDriver temperatureReader,
    BotHostedService bot,
    Database db
)
{
    private readonly ILogger<HeatingDriver> logger = logger;
    private readonly IPinDriver pinDriver = pinDriver;
    private readonly ITemperatureDriver temperatureReader = temperatureReader;
    private readonly int heatingPin = configuration.GetValue<int>("Pins:Heating");
    private bool temperatureUnavailableNotified = false;
    private readonly TimeSpan readingsTimespan = TimeSpan.FromSeconds(
        configuration.GetValue<int?>("Heating:TempSensorsToleranceSeconds") ?? 60
    );

    public bool IsBoilerActive()
    {
        return pinDriver.DigitalRead(heatingPin) == 1;
    }

    public async Task<float?> GetOperationalTemperature(CancellationToken? cancellationToken = null)
    {
        var readings = temperatureReader.GetExternalTemperatureReadings(readingsTimespan);
        var temperatures = readings.ConvertAll(r => r.Temperature);
        var localTemperature = await temperatureReader.GetLocalTemperature(cancellationToken);
        if (localTemperature != null)
        {
            if (temperatureUnavailableNotified)
            {
                _ = bot.Client.NotifyOwners("✅ Temperatura host tornata disponbile!");
                temperatureUnavailableNotified = false;
            }
            temperatures.Add((float)localTemperature);
        }
        else if (!temperatureUnavailableNotified)
        {
            _ = bot.Client.NotifyOwners("⚠️ Temperatura host non disponibile!");
            temperatureUnavailableNotified = true;
        }
        if (temperatures.Count > 0)
        {
            logger.LogDebug(
                "{c} external temp readings [avg: {r}, min: {m}, max: {min}]",
                readings.Count,
                temperatures.Average(),
                temperatures.Min(),
                temperatures.Max()
            );
            return temperatures.Average();
        }
        return null;
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
