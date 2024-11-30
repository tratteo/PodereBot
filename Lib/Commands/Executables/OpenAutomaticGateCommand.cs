using PodereBot.Lib.Commands.Abstract;
using PodereBot.Services;

namespace PodereBot.Lib.Commands;

[CommandMetadata(Key = "/openauto", Description = "Ti apro il cancello automatico (forse 😼)")]
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
