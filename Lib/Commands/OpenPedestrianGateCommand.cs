using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace PodereBot.Lib.Commands;

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
