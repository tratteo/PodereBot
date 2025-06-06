﻿using PodereBot.Services;
using Telegram.Bot;

namespace PodereBot.Lib.Commands;

[CommandMetadata(Key = "/gatelight", Description = "Accendo/spengo le luci dei cancelli 💡", Admin = true)]
internal class ToggleGatesLightCommand(
    GateDriver gateDriver,
    Skin skin,
    Database db,
    IConfiguration configuration,
    ILogger<ToggleGatesLightCommand> logger
) : Command(skin, logger, configuration)
{
    private readonly GateDriver gateDriver = gateDriver;
    private readonly Database db = db;

    protected override async Task ExecuteInternal()
    {
        await gateDriver.ToggleLights();
        await Arguments.Client.SendAsset(Arguments.Message, skin.Schema.GatesLight);
        await Arguments.Client.SendMessage(
            Arguments.Message.Chat.Id,
            "Ho acceso o spento le luci del cancello. Io non posso sapere in che stato sono, vai a guardare 😿",
            disableNotification: true
        );
    }
}
