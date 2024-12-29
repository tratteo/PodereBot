using PodereBot.Lib.Common;
using PodereBot.Services;

internal class HeatingProgramDaemon(
    ILogger<HeatingProgramDaemon> logger,
    IConfiguration configuration,
    Database db,
    HeatingDriver heatingDriver
) : BackgroundService
{
    private readonly TimeSpan daemonPollInterval = TimeSpan.FromSeconds(configuration.GetValue<int?>("Heating:PollIntervalSeconds") ?? 10);
    private readonly TimeSpan thresholdTolerance = TimeSpan.FromSeconds(configuration.GetValue<int?>("Heating:ToleranceSeconds") ?? 600);
    private TimeSpan actionDelay = TimeSpan.Zero;
    private HeatingInterval? lastInterval;

    public void ToggleHeating(bool enabled)
    {
        heatingDriver.SwitchHeating(enabled);
        actionDelay = TimeSpan.Zero;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(daemonPollInterval);
        do
        {
            try
            {
                // If there is no program, quit
                if (db.Data.HeatingProgram == null || db.Data.HeatingProgram.IsSuspended)
                    continue;

                // If temperature is not available, quit
                var temperature = await heatingDriver.GetOperationalTemperature(stoppingToken);
                if (temperature == null)
                {
                    logger.LogTrace("no temperature data");
                    continue;
                }

                var currentInterval = db.Data.HeatingProgram.GetCurrentInterval();
                //* Entered a new interval
                if (currentInterval != null && lastInterval == null)
                {
                    logger.LogTrace("entered a new interval {i}", currentInterval.ToCodeString());
                    db.Edit(d => d.ManualHeatingActive = false);
                    if (temperature < currentInterval.Temperature - 0.5)
                    {
                        ToggleHeating(true);
                    }
                }
                //* Exited from the current interval
                else if (currentInterval == null && lastInterval != null)
                {
                    logger.LogTrace("exited interval");
                    ToggleHeating(false);
                }
                //* Currently in an interval
                else if (currentInterval != null)
                {
                    var boilerActive = heatingDriver.IsBoilerActive();
                    if (boilerActive)
                    {
                        if (temperature > currentInterval!.Temperature && actionDelay >= thresholdTolerance)
                        {
                            ToggleHeating(false);
                        }
                        else if (temperature <= currentInterval!.Temperature)
                        {
                            actionDelay = TimeSpan.Zero;
                        }
                    }
                    else
                    {
                        if (temperature < currentInterval!.Temperature && actionDelay >= thresholdTolerance)
                        {
                            ToggleHeating(true);
                        }
                        else if (temperature >= currentInterval!.Temperature)
                        {
                            actionDelay = TimeSpan.Zero;
                        }
                    }
                }

                lastInterval = currentInterval;
                actionDelay = actionDelay.Add(daemonPollInterval);
            }
            catch (Exception ex)
            {
                logger.LogWarning("failed with exception message {e}. Good luck next round!", ex.Message);
            }
        } while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }
}
