using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PodereBot.Lib.Common;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Lib.Commands;

[CommandMetadata(
    Key = "/programheating",
    Description = "Imposta il programma di riscaldamento 🔥",
    Admin = true
)]
internal class ProgramHeatingCommand(
    ILogger<ProgramHeatingCommand> logger,
    Skin skin,
    IConfiguration configuration,
    Database db
) : Command(skin, logger, configuration)
{
    private readonly ILogger<ProgramHeatingCommand> logger = logger;
    private readonly Database db = db;

    protected override async Task ExecuteInternal()
    {
        AttachEvents();
        var kbd = new InlineKeyboardMarkup().AddButton(
            "Annulla",
            EncodeCallbackQueryData("cancel")
        );
        if (db.Data.HeatingProgram != null)
        {
            kbd.AddButton("Cancella", EncodeCallbackQueryData("delete"));
        }
        var programData =
            db.Data.HeatingProgram != null
                ? $"""
                La pianificazione attuale è la seguente: 
                <b>{db.Data.HeatingProgram}</b>
                """
                : "La pianificazione non è impostata";
        await Arguments
            .Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"""
            {programData}.

            Per impostare la pianificazione, fornisci gli intervalli in cui voi che il riscaldamento si accenda.
            Ogni intervallo è identificato con il seguente formato: <code>hh:mm-hh:mm</code>.
            Per specificare più intervalli, separali con il carattere <code>/</code>.
            <i>Esempi:
            - 06:30-12:30/15:15-22
            - 10-12:30/15-18/22-23:30
            </i>
            Inviami la pianificazione per messaggio quando sei pronto
            """,
                parseMode: ParseMode.Html,
                replyMarkup: kbd,
                disableNotification: true
            )
            .DeleteOnDetach(this);
    }

    protected override async Task OnCallback(Update update, string callbackData)
    {
        if (callbackData == "delete")
        {
            db.Edit(d => d.HeatingProgram = null);
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"🟢 Ho rimosso la programmazione",
                disableNotification: true
            );
        }
        await DetachEvents();
    }

    private async Task UpdateError(string text)
    {
        await Arguments
            .Client.SendMessage(Arguments.Message.Chat.Id, $"🔴 {text}", disableNotification: true)
            .DeleteOnDetach(this);
    }

    protected override async Task OnMessage(Message message, UpdateType type)
    {
        if (message.Text == null)
        {
            await UpdateError($"Valore non valido!");
            return;
        }
        var splits = message.Text.Split("/");
        var intervals = new List<DayInterval>();
        var matcher = @"(?<hfrom>[0-2]?\d):?(?<mfrom>[0-5]\d)?-(?<hto>[0-2]?\d):?(?<mto>[0-5]\d)?";
        foreach (var s in splits)
        {
            var m = Regex.Match(s, matcher);
            if (
                !int.TryParse(m.Groups.GetValueOrDefault("hfrom")?.Value, out var hoursFrom)
                || !int.TryParse(m.Groups.GetValueOrDefault("hto")?.Value, out var hoursTo)
            )
            {
                await UpdateError($"I valori delle ore e dei minuti non sono corretti 😭");
                return;
            }
            if (!int.TryParse(m.Groups.GetValueOrDefault("mfrom")?.Value, out var minFrom))
            {
                minFrom = 0;
            }
            if (!int.TryParse(m.Groups.GetValueOrDefault("mto")?.Value, out var minTo))
            {
                minTo = 0;
            }

            if (hoursFrom > 23 || hoursTo > 23)
            {
                await UpdateError($"I valori delle ore non possono essere maggiori di 23");
                return;
            }
            if (hoursFrom > hoursTo)
            {
                await UpdateError(
                    $"L'ora di inizio non può essere maggiore dell'ora di fine dell'intervallo! {hoursFrom} > {hoursTo}"
                );
                return;
            }
            else if (hoursFrom == hoursTo && minFrom > minTo)
            {
                await UpdateError(
                    $"I minuti di inizio non possono essere maggiori dei minuti di fine dell'intervallo! {minFrom} > {minTo}"
                );
                return;
            }
            intervals.Add(
                new DayInterval()
                {
                    HoursFrom = hoursFrom,
                    MinutesFrom = minFrom,
                    HoursTo = hoursTo,
                    MinutesTo = minTo
                }
            );
        }

        if (!HeatingProgram.TryBuild(intervals, out var program))
        {
            await UpdateError($"Gli intervalli non sono in ordine!");
            return;
        }
        db.Edit(d => d.HeatingProgram = program);
        await Arguments.Client.SendMessage(
            Arguments.Message.Chat.Id,
            $"""
            🟢 Ho aggiornato la programmazione:
            {program}            
            """,
            parseMode: ParseMode.Html,
            disableNotification: true
        );
        logger.LogInformation("detaching events");

        await DetachEvents();
    }
}
