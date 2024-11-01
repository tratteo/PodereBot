using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PodereBot.Lib.Commands;

internal class OpenAutomaticGateCommand : Command
{
    private readonly GateDriver gateDriver;
    private readonly Database db;
    private readonly ILogger<OpenAutomaticGateCommand> logger;

    public OpenAutomaticGateCommand(GateDriver gateDriver, Database db, IConfiguration configuration, ILogger<OpenAutomaticGateCommand> logger) : base(configuration)
    {
        this.gateDriver = gateDriver;
        this.db = db;
        this.logger = logger;
    }

    protected override async Task ExecuteInternal(CommandArguments arguments)
    {
        var admins = configuration.GetSection("Admins").Get<long[]>()?.ToList() ?? [];
        if (admins.Contains(arguments.Message.From!.Id))
        {
            await gateDriver.Open(GateDriver.GateId.automatic);
        }
        else
        {
            var gatesOpen = db.Data.GatesOpenAccessExpirationDate != null || db.Data.GatesOpenAccessExpirationDate > DateTime.Now;
            if (!gatesOpen)
            {
                await arguments.Client.SendAnimationAsync(arguments.Message.Chat.Id, InputFile.FromString("https://media1.tenor.com/m/dz-seLKqRe4AAAAd/out-the-office.gif"), caption: "I cancelli sono bloccati al momento");
            }
            else
            {
                await gateDriver.Open(GateDriver.GateId.automatic);
            }
        }
    }
}