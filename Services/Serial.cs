using System.IO.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PodereBot.Services;

internal class Serial
{
    private const int BAUD_RATE = 115200;

    private readonly ILogger logger;
    private readonly SerialPort? serialPort;

    public Serial(ILogger<Serial> logger, IConfiguration configuration)
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

    public bool Write(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return false;
        }

        try
        {
            serialPort?.Write(message);
            logger.LogTrace("message written to {p}: {m}", serialPort, message);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogTrace(ex, "error writing to serial port {p}", serialPort);
            return false;
        }
    }
}
