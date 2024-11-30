using PodereBot.Lib.Commands.Abstract;
using PodereBot.Services;

namespace PodereBot.Lib.Commands;

[CommandMetadata(Key = "/openped", Description = "Ti apro il cancello pedonale (forse 😼)")]
internal class OpenPedestrianGateCommand(
    Skin skin,
    GateDriver gateDriver,
    Database db,
    IConfiguration configuration,
    ILogger<OpenPedestrianGateCommand> logger
) : AbstractOpenGateCommand(skin, gateDriver, db, configuration, logger, GateId.pedestrian)
{
    protected override string GateName => "pedonale";

    protected override Asset? Asset => skin.Schema.PedestrianGateOpen;
}
