using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Lib.Commands;

// [CommandMetadata(
//     Key = "/heating",
//     Description = "Attiva o disattiva il riscaldamento",
//     Admin = true
// )]
internal class ToggleHeatingCommand(
    ILogger<ToggleHeatingCommand> logger,
    Skin skin,
    Database db,
    IPinDriver pinDriver,
    IConfiguration configuration
) : Command(skin, logger, configuration)
{
    private readonly ILogger<ToggleHeatingCommand> logger = logger;
    private readonly Database db = db;
    private readonly IPinDriver pinDriver = pinDriver;
    private readonly int heatingPin = configuration.GetValue<int>("Pins:Heating");

    protected override async Task ExecuteInternal()
    {
        AttachEvents();
        var kbd = new InlineKeyboardMarkup()
            .AddButton("🔥", EncodeCallbackQueryData(1))
            .AddButton("❄️", EncodeCallbackQueryData(0))
            .AddNewRow()
            .AddButton("Annulla", EncodeCallbackQueryData(-1));
        var heatingActive = db.Data.HeatingProgram?.IsActive() ?? false;
        await Arguments
            .Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"""
            Il riscaldamento è <b>{(heatingActive ? "acceso" : "spento")}</b>.
            Vuoi attivare o disattivare il riscaldamento?
            """,
                parseMode: ParseMode.Html,
                replyMarkup: kbd,
                disableNotification: true
            )
            .DeleteOnDetach(this);
    }

    protected override async Task OnCallback(Update update, string callbackData)
    {
        if (!int.TryParse(callbackData, out var code))
        {
            return;
        }
        if (code == 0)
        {
            await pinDriver.PinLow(heatingPin);
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"Ho spento il riscaldamento ❄️",
                parseMode: ParseMode.Html,
                disableNotification: true
            );
            //db.Edit((d) => d.HeatingActive = false);
        }
        else if (code == 1)
        {
            await pinDriver.PinHigh(heatingPin);
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"Ho acceso il riscaldamento 🔥",
                parseMode: ParseMode.Html,
                disableNotification: true
            );
            //db.Edit((d) => d.HeatingActive = true);
        }
        await DetachEvents();
    }
}
