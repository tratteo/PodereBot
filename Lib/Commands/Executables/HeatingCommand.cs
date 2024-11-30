using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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
    HeatingDriver heatingDriver,
    Database db
) : Command(skin, logger, configuration)
{
    private readonly System.Timers.Timer refreshTimer = new(TimeSpan.FromSeconds(5));

    protected override Task OnDetach()
    {
        refreshTimer.Stop();
        return Task.CompletedTask;
    }

    private InlineKeyboardMarkup GetInlineKeyboardMarkup()
    {
        var kbd = new InlineKeyboardMarkup();
        var interval = db.Data.HeatingProgram?.GetCurrentInterval();
        if (interval == null)
        {
            kbd.AddButton("🔥 Accendi", EncodeCallbackQueryData("on"));
            kbd.AddButton("❄️ Spegni", EncodeCallbackQueryData("off"));
            kbd.AddNewRow();
        }
        if (db.Data.HeatingProgram != null)
        {
            kbd.AddButton("Rimuovi programma", EncodeCallbackQueryData("delete")).AddNewRow();
        }
        kbd.AddButton("Chiudi", EncodeCallbackQueryData("cancel"));
        return kbd;
    }

    private StringBuilder GetHtmlStatusMessage(string temperatureLine)
    {
        var html = new StringBuilder().AppendLine(temperatureLine);
        var boilerActive = heatingDriver.IsBoilerActive();
        var interval = db.Data.HeatingProgram?.GetCurrentInterval();
        if (interval == null)
        {
            if (boilerActive && db.Data.ManualHeatingActive)
            {
                html.AppendLine($"<b>🥵 Riscaldamento acceso manualmente</b>");
            }
            else
            {
                html.AppendLine($"<b>🥶 Riscaldamento spento</b>");
            }
            var nextInterval = db.Data.HeatingProgram?.GetNextInterval();
            if (nextInterval != null)
            {
                html.AppendLine(
                    $"🕓 Dalle <b>{nextInterval.HoursFrom:D2}:{nextInterval.MinutesFrom:D2}</b> è programmato a <b>{nextInterval!.Temperature}°</b>"
                );
            }
        }
        else
        {
            html.AppendLine(
                $"🕓 Riscaldamento programmato a <b>{interval!.Temperature}°</b> fino alle <b>{interval.HoursTo:D2}:{interval.MinutesTo:D2}</b>"
            );
        }
        if (boilerActive)
        {
            html.AppendLine("🔥 Caldaia <b>accesa</b>");
        }
        else
        {
            html.AppendLine("❄️ Caldaia <b>spenta</b>");
        }
        html.Append(new string('_', 25));
        return html;
    }

    protected override async Task ExecuteInternal()
    {
        AttachEvents();
        await Arguments.Client.SendChatAction(Arguments.Message.Chat.Id, ChatAction.Typing);

        var boilerActive = heatingDriver.IsBoilerActive();
        var interval = db.Data.HeatingProgram?.GetCurrentInterval();
        var html = GetHtmlStatusMessage($"🌡️ Sto calcolando la temperatura...");
        html.AppendLine();
        html.AppendLine();
        if (db.Data.HeatingProgram != null)
        {
            html.AppendLine(
                $"""
                La pianificazione attuale: 
                <blockquote>{db.Data.HeatingProgram}</blockquote>
                <code>{db.Data.HeatingProgram.ToCodeString()}</code>
                """
            );
        }
        else
        {
            html.AppendLine("La pianificazione non è impostata.");
        }

        html.AppendLine();
        html.AppendLine(
            "Inviami la nuova pianificazione per messaggio se vuoi cambiarla. Utilizza il seguente formato per gli intervalli: <code>hh:mm-hh:mm@t/.../hh:mm-hh:mm@t</code>"
        );
        var message = await Arguments
            .Client.SendMessage(
                Arguments.Message.Chat.Id,
                html.ToString(),
                parseMode: ParseMode.Html,
                replyMarkup: GetInlineKeyboardMarkup(),
                disableNotification: true
            )
            .DeleteOnDetach(this);

        UpdateStatusMessage(message, html.ToString());
        refreshTimer.Elapsed += (_, _) => UpdateStatusMessage(message, html.ToString());
        refreshTimer.Start();
    }

    private async void UpdateStatusMessage(Message? message, string msgHtml)
    {
        if (message == null)
            return;

        var temp = await heatingDriver.GetOperationalTemperature();
        var html = GetHtmlStatusMessage($"🌡️ Temperatura: <b>{(temp != null ? $"{temp:F2}°" : "non disponibile 😵")}</b>");
        msgHtml = Regex.Replace(msgHtml, @"^[^_]*_+", html.ToString());
        try
        {
            await Arguments.Client.EditMessageText(
                message.Chat.Id,
                message.Id,
                msgHtml,
                parseMode: ParseMode.Html,
                replyMarkup: GetInlineKeyboardMarkup()
            );
        }
        catch (Exception) { }
    }

    protected override async Task OnCallback(Update update, string callbackData)
    {
        var nextInterval = db.Data.HeatingProgram?.GetNextInterval();
        if (callbackData == "delete")
        {
            var currentInterval = db.Data.HeatingProgram?.GetCurrentInterval();
            db.Edit(d =>
            {
                d.HeatingProgram = null;
            });
            if (currentInterval != null)
                heatingDriver.SwitchHeating(false);
            await Arguments.Client.SendMessage(Arguments.Message.Chat.Id, $"🟢 Ho rimosso la programmazione", disableNotification: true);
        }
        else if (callbackData == "off")
        {
            heatingDriver.SwitchHeating(false);
            db.Edit(d => d.ManualHeatingActive = false);
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"""
                <b>Ho spento il riscaldamento ❄️</b>

                Le modifiche verrano mantenute fino alla prossima programmazione.
                <blockquote>{(nextInterval != null ? $"{nextInterval}" : "")}</blockquote>
                """,
                parseMode: ParseMode.Html,
                disableNotification: true
            );
        }
        else if (callbackData == "on")
        {
            heatingDriver.SwitchHeating(true);
            db.Edit(d => d.ManualHeatingActive = true);
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"""
                <b>Ho acceso il riscaldamento 🔥</b>

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

    private async Task<HeatingProgram?> ParseProgram(string pattern)
    {
        var splits = pattern.Split("/");
        var intervals = new List<HeatingInterval>();
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
                return null;
            }
            if (!float.TryParse(m.Groups.GetValueOrDefault("temp")?.Value, CultureInfo.InvariantCulture, out var temp))
            {
                await UpdateError($"Il valore della temperatura non è corretto");
                return null;
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
                return null;
            }
            if ((hoursFrom == 24 && minFrom > 0) || (hoursTo == 24 && minTo > 0))
            {
                await UpdateError($"I minuti non possono essere maggiori di 0 se il valore delle ore è 24");
                return null;
            }
            if (hoursFrom > hoursTo)
            {
                await UpdateError($"L'ora di inizio non può essere maggiore dell'ora di fine dell'intervallo! {hoursFrom} > {hoursTo}");
                return null;
            }
            else if (hoursFrom == hoursTo && minFrom > minTo)
            {
                await UpdateError(
                    $"I minuti di inizio non possono essere maggiori dei minuti di fine dell'intervallo! {minFrom} > {minTo}"
                );
                return null;
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
            return null;
        }
        return program;
    }

    protected override async Task OnMessage(Message message, UpdateType type)
    {
        try
        {
            if (message.Text == null)
            {
                await UpdateError($"Valore non valido!");
                return;
            }

            var program = await ParseProgram(message.Text);

            if (program == null)
                return;
            db.Edit(d => d.HeatingProgram = program);
            var msg = new StringBuilder();
            var temperature = await heatingDriver.GetOperationalTemperature();

            var interval = db.Data.HeatingProgram?.GetCurrentInterval();
            var nextInterval = db.Data.HeatingProgram?.GetNextInterval();
            if (interval != null)
            {
                if (temperature != null && temperature < interval.Temperature - 1)
                {
                    heatingDriver.SwitchHeating(true);
                }
                msg.AppendLine(
                    $"<b>🕓 Riscaldamento programmato a {interval!.Temperature}° fino alle {interval.HoursTo:D2}:{interval.MinutesTo:D2}</b>"
                );
            }
            else if (nextInterval != null)
            {
                msg.AppendLine(
                    $"<b>🕓 Riscaldamento programmato a {nextInterval!.Temperature}° dalle {nextInterval.HoursFrom:D2}:{nextInterval.MinutesFrom:D2}</b>"
                );
            }

            if (heatingDriver.IsBoilerActive())
            {
                msg.AppendLine("🔥 Caldaia <b>accesa</b>");
            }
            else
            {
                msg.AppendLine("❄️ Caldaia <b>spenta</b>");
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

            await DetachEvents();
        }
        catch (Exception ex)
        {
            logger.LogWarning("{e}", ex);
        }
    }
}
