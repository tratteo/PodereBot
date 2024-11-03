using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;

namespace PodereBot.Lib.Commands;

internal class OpenAutomaticGateCommand(
    SkinService skin,
    GateDriverService gateDriver,
    DatabaseService db,
    IConfiguration configuration,
    ILogger<OpenAutomaticGateCommand> logger
)
    : AbstractOpenGateCommand(
        skin,
        gateDriver,
        db,
        configuration,
        logger,
        GateDriverService.GateId.automatic
    )
{
    protected override string GateName => "automatico";

    protected override Asset? Asset => skin.Schema.AutomaticGateOpen;
}
