using System.IO.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PodereBot.Services;

internal class SerialPinDriver : IPinDriver
{
    private const int BAUD_RATE = 115200;

    private readonly ILogger logger;
    private readonly SerialPort? serialPort;

    public SerialPinDriver(ILogger<SerialPinDriver> logger, IConfiguration configuration)
    {
        this.logger = logger;
        string? port = configuration.GetValue<string>("Serial:Port")!;
        try
        {
            serialPort = new SerialPort(port, BAUD_RATE);
            serialPort.Open();
            logger.LogInformation("serial port ({p}) opened", serialPort.PortName);
        }
        catch (Exception ex)
        {
            logger.LogWarning("error opening serial bus: {ex}", ex);
        }
    }

    public Task PinHigh(int? pin)
    {
        if (pin == null)
            return Task.CompletedTask;
        try
        {
            serialPort?.Write($"h{pin}");
            logger.LogTrace("serial pin {p}: high", pin);
        }
        catch (Exception ex)
        {
            logger.LogTrace(ex, "error writing to serial port {p}", serialPort);
        }
        return Task.CompletedTask;
    }

    public Task PinLow(int? pin)
    {
        if (pin == null)
            return Task.CompletedTask;
        try
        {
            serialPort?.Write($"l{pin}");
            logger.LogTrace("serial pin {p}: low", pin);
        }
        catch (Exception ex)
        {
            logger.LogTrace(ex, "error writing to serial port {p}", serialPort);
        }
        return Task.CompletedTask;
    }
}
