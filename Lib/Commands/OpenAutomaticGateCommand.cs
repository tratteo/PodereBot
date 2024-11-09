using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;

namespace PodereBot.Lib.Commands;

internal class OpenAutomaticGateCommand(
    Skin skin,
    GateDriver gateDriver,
    Database db,
    IConfiguration configuration,
    ILogger<OpenAutomaticGateCommand> logger
) : AbstractOpenGateCommand(skin, gateDriver, db, configuration, logger, GateId.automatic)
{
    protected override string GateName => "automatico";

    protected override Asset? Asset => skin.Schema.AutomaticGateOpen;
}
