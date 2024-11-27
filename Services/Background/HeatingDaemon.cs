using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PodereBot.Services;

internal class HeatingDaemon(
    ILogger<HeatingDaemon> logger,
    IPinDriver pinDriver,
    ITemperatureReader temperatureReader,
    IConfiguration configuration,
    Database db
) : BackgroundService
{
    private readonly ILogger<HeatingDaemon> logger = logger;
    private readonly Database db = db;
    private readonly IPinDriver pinDriver = pinDriver;
    private readonly ITemperatureReader temperatureReader = temperatureReader;
    private readonly int heatingPin = configuration.GetValue<int>("Pins:Heating");
    private readonly TimeSpan daemonPollInterval = TimeSpan.FromSeconds(configuration.GetValue<int?>("Heating:PollIntervalSeconds") ?? 10);
    private readonly TimeSpan thresholdTolerance = TimeSpan.FromSeconds(configuration.GetValue<int?>("Heating:ToleranceSeconds") ?? 600);
    private TimeSpan actionDelay = TimeSpan.Zero;
    private bool heatingActive = false;

    public async Task ToggleHeating(bool enabled)
    {
        if (enabled == heatingActive)
            return;

        actionDelay = TimeSpan.Zero;
        heatingActive = enabled;
        db.Edit(d => d.HeatingActive = enabled);
        logger.LogInformation("heating: {h}", enabled);
        if (enabled)
        {
            await pinDriver.PinHigh(heatingPin);
        }
        else
        {
            await pinDriver.PinLow(heatingPin);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(daemonPollInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var program = db.Data.HeatingProgram;
                if (program == null)
                {
                    continue;
                }
                var temperature = await temperatureReader.GetTemperature(stoppingToken);
                if (temperature == null)
                {
                    logger.LogWarning("no temperature data");
                    continue;
                }
                var interval = program.GetActiveInterval();
                if (interval == null)
                {
                    await ToggleHeating(false);
                    continue;
                }

                if (heatingActive)
                {
                    if (temperature > interval!.Temperature && actionDelay >= thresholdTolerance)
                    {
                        await ToggleHeating(false);
                    }
                    else if (temperature <= interval!.Temperature)
                    {
                        actionDelay = TimeSpan.Zero;
                    }
                }
                else
                {
                    if (temperature < interval!.Temperature && actionDelay >= thresholdTolerance)
                    {
                        await ToggleHeating(true);
                    }
                    else if (temperature >= interval!.Temperature)
                    {
                        actionDelay = TimeSpan.Zero;
                    }
                }

                actionDelay = actionDelay.Add(daemonPollInterval);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed with exception message {e}. Good luck next round!", ex.Message);
            }
        }
    }
}
