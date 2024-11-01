using Microsoft.Extensions.Configuration;

namespace PodereBot.Lib.Commands;

internal class OpenPedestrianGateCommand : Command
{
    private readonly GateDriver gateDriver;

    public OpenPedestrianGateCommand(GateDriver gateDriver, IConfiguration configuration) : base(configuration)
    {
        this.gateDriver = gateDriver;
    }

    protected override async Task ExecuteInternal(CommandArguments arguments)
    {
        await gateDriver.Open(GateDriver.GateId.pedestrian);
    }
}