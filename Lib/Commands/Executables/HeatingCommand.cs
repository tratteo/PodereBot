using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Lib.Common;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Lib.Commands;

[CommandMetadata(Key = "/heating", Description = "Gestisci il riscaldamento della casa 🔥", Admin = true)]
internal class HeatingCommand(
    ILogger<HeatingCommand> logger,
    Skin skin,
    IConfiguration configuration,
    ITemperatureReader temperatureReader,
    IPinDriver pinDriver,
    Database db
) : Command(skin, logger, configuration)
{
    private readonly ILogger<HeatingCommand> logger = logger;
    private readonly ITemperatureReader temperatureReader = temperatureReader;
    private readonly Database db = db;
    private readonly IPinDriver pinDriver = pinDriver;
    private readonly int heatingPin = configuration.GetValue<int>("Pins:Heating");

    protected override async Task ExecuteInternal()
    {
        AttachEvents();
        var kbd = new InlineKeyboardMarkup();
        var msg = new StringBuilder();
        var temperature = await temperatureReader.GetTemperature();
        var interval = db.Data.HeatingProgram?.GetActiveInterval();
        msg.AppendLine($"🌡️ Temperatura: <b>{(temperature != null ? $"{temperature:F2}°" : "Non disponibile")}</b>\n");
        if (interval == null)
        {
            kbd.AddButton("🔥 Accendi", EncodeCallbackQueryData("on")).AddButton("❄️ Spegni", EncodeCallbackQueryData("off")).AddNewRow();
            if (db.Data.HeatingActive)
            {
                msg.AppendLine($"<b>🔥 Riscaldamento acceso manualmente</b>");
            }
            else
            {
                msg.AppendLine($"<b>❄️ Riscaldamento spento</b>");
            }
        }
        else
        {
            msg.AppendLine(
                $"<b>🔥 Riscaldamento acceso a {interval!.Temperature}° fino alle {interval.HoursTo:D2}:{interval.MinutesTo:D2}</b>"
            );
        }

        if (db.Data.HeatingProgram != null)
        {
            kbd.AddButton("Rimuovi programma", EncodeCallbackQueryData("delete")).AddNewRow();
            msg.AppendLine(
                $"""
                La pianificazione attuale: 
                <blockquote>{db.Data.HeatingProgram}</blockquote>
                <code>{db.Data.HeatingProgram.ToCodeString()}</code>
                """
            );
        }
        else
        {
            msg.AppendLine("La pianificazione non è impostata.");
        }
        kbd.AddButton("Chiudi", EncodeCallbackQueryData("cancel"));
        await Arguments
            .Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"""
            {msg}
            Inviami la nuova pianificazione per messaggio se vuoi cambiarla. Utilizza il seguente formato per gli intervalli: <code>hh:mm-hh:mm@t/.../hh:mm-hh:mm@t</code>
            """,
                parseMode: ParseMode.Html,
                replyMarkup: kbd,
                disableNotification: true
            )
            .DeleteOnDetach(this);
    }

    protected override async Task OnCallback(Update update, string callbackData)
    {
        var nextInterval = db.Data.HeatingProgram?.GetFirstIntervalInProgram();
        if (callbackData == "delete")
        {
            var currentInterval = db.Data.HeatingProgram?.GetActiveInterval();
            db.Edit(d =>
            {
                d.HeatingProgram = null;
                if (currentInterval != null)
                    d.HeatingActive = false;
            });
            if (currentInterval != null)
                await pinDriver.PinLow(heatingPin);
            await Arguments.Client.SendMessage(Arguments.Message.Chat.Id, $"🟢 Ho rimosso la programmazione", disableNotification: true);
        }
        else if (callbackData == "off")
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
        }
        else if (callbackData == "on")
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
        }
        await DetachEvents();
    }

    private async Task UpdateError(string text)
    {
        await Arguments.Client.SendMessage(Arguments.Message.Chat.Id, $"🔴 {text}", disableNotification: true).CleanupOnDetach(this);
        await DetachEvents();
    }

    protected override async Task OnMessage(Message message, UpdateType type)
    {
        if (message.Text == null)
        {
            await UpdateError($"Valore non valido!");
            return;
        }
        var splits = message.Text.Split("/");
        var intervals = new List<HeatingInterval>();
        var temperature = await temperatureReader.GetTemperature();
        var matcher = @"(?<hfrom>[0-2]?\d):?(?<mfrom>[0-5]\d)?-(?<hto>[0-2]?\d):?(?<mto>[0-5]\d)?@(?<temp>[0-9]+(\.[0-9]+)?)";
        foreach (var s in splits)
        {
            var m = Regex.Match(s, matcher);
            if (
                !int.TryParse(m.Groups.GetValueOrDefault("hfrom")?.Value, out var hoursFrom)
                || !int.TryParse(m.Groups.GetValueOrDefault("hto")?.Value, out var hoursTo)
            )
            {
                await UpdateError($"I valori forniti non sono corretti 😭");
                return;
            }
            if (!float.TryParse(m.Groups.GetValueOrDefault("temp")?.Value, CultureInfo.InvariantCulture, out var temp))
            {
                await UpdateError($"Il valore della temperatura non è corretto");
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

            if (hoursFrom > 24 || hoursTo > 24)
            {
                await UpdateError($"I valori delle ore non possono essere maggiori di 24");
                return;
            }
            if ((hoursFrom == 24 && minFrom > 0) || (hoursTo == 24 && minTo > 0))
            {
                await UpdateError($"I minuti non possono essere maggiori di 0 se il valore delle ore è 24");
                return;
            }
            if (hoursFrom > hoursTo)
            {
                await UpdateError($"L'ora di inizio non può essere maggiore dell'ora di fine dell'intervallo! {hoursFrom} > {hoursTo}");
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
                new HeatingInterval()
                {
                    HoursFrom = hoursFrom,
                    MinutesFrom = minFrom,
                    HoursTo = hoursTo,
                    MinutesTo = minTo,
                    Temperature = temp
                }
            );
        }

        if (!HeatingProgram.TryBuild(intervals, out var program))
        {
            await UpdateError($"Gli intervalli non sono in ordine!");
            return;
        }
        db.Edit(d => d.HeatingProgram = program);
        var msg = new StringBuilder();
        var interval = db.Data.HeatingProgram?.GetActiveInterval();
        if (interval != null)
        {
            if (temperature != null && temperature < interval.Temperature - 1)
            {
                db.Edit(d => d.HeatingActive = true);
                await pinDriver.PinHigh(heatingPin);
            }
            msg.AppendLine(
                $"<b>🔥 Riscaldamento programmato a {interval!.Temperature}° fino alle {interval.HoursTo:D2}:{interval.MinutesTo:D2}</b>"
            );
        }
        else
        {
            msg.AppendLine($"<b>❄️ Riscaldamento spento</b>");
        }

        await Arguments.Client.SendMessage(
            Arguments.Message.Chat.Id,
            $"""
            🟢 Ho aggiornato la programmazione:
            <blockquote>{program}</blockquote>            
            {msg}
            """,
            parseMode: ParseMode.Html,
            disableNotification: true
        );

        logger.LogInformation("detaching events");

        await DetachEvents();
    }
}
