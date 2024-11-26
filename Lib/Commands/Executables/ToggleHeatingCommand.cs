using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Lib.Commands;

[CommandMetadata(Key = "/heating", Description = "Accendi o spegni il riscaldamento 🔥", Admin = true)]
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
        if (db.Data.HeatingProgram?.IsScheduledActive(out var interval) ?? false)
        {
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"""
                🟡 Non puoi cambiare lo stato del riscaldamento quando è in programmazione.

                 <b>🔥 Il riscaldamento è acceso a {interval!.Temperature}° fino alle {interval.HoursTo:D2}:{interval.MinutesTo:D2}</b>
                """,
                parseMode: ParseMode.Html,
                disableNotification: true
            );
            await DetachEvents();
            return;
        }

        var kbd = new InlineKeyboardMarkup()
            .AddButton("🔥", EncodeCallbackQueryData(1))
            .AddButton("❄️", EncodeCallbackQueryData(0))
            .AddNewRow()
            .AddButton("Annulla", EncodeCallbackQueryData(-1));

        await Arguments
            .Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"Vuoi attivare o disattivare il riscaldamento?",
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
        var nextInterval = db.Data.HeatingProgram?.GetFirstIntervalInProgram();
        if (code == 0)
        {
            await pinDriver.PinLow(heatingPin);
            db.Edit(d => d.HeatingActive = false);
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"""
                Ho spento il riscaldamento ❄️
                Le modifiche verrano mantenute fino alla prossima programmazione.
                <blockquote>{(nextInterval != null ? $"{nextInterval}" : "")}</blockquote>
                """,
                parseMode: ParseMode.Html,
                disableNotification: true
            );
            //db.Edit((d) => d.HeatingActive = false);
        }
        else if (code == 1)
        {
            await pinDriver.PinHigh(heatingPin);
            db.Edit(d => d.HeatingActive = true);
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"""
                Ho acceso il riscaldamento 🔥
                Le modifiche verrano mantenute fino alla prossima programmazione.
                <blockquote>{(nextInterval != null ? $"{nextInterval}" : "")}</blockquote>
                """,
                parseMode: ParseMode.Html,
                disableNotification: true
            );
            //db.Edit((d) => d.HeatingActive = true);
        }
        await DetachEvents();
    }
}
