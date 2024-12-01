using System.Text;
using Humanizer;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Lib.Commands;

[CommandMetadata(Key = "/temperatures", Description = "Ti mando tutte le temperature dei sensori della casa 🌡️")]
internal class TemperaturesCommand(
    Skin skin,
    ILogger<StartCommand> logger,
    IConfiguration configuration,
    ITemperatureDriver temperatureDriver
) : Command(skin, logger, configuration)
{
    private readonly System.Timers.Timer refreshTimer = new(TimeSpan.FromSeconds(5));
    private readonly InlineKeyboardMarkup kbd = new();

    protected override Task OnDetach()
    {
        refreshTimer.Stop();
        return Task.CompletedTask;
    }

    private async Task<string> GetHtmlStatusMessage(bool computeTemperatures)
    {
        var msg = new StringBuilder();
        if (computeTemperatures)
        {
            var temp = await temperatureDriver.GetLocalTemperature();
            var externalReadings = temperatureDriver.GetExternalTemperatureReadings();
            msg.AppendLine($"🌡️ Temperature");
            var tempString = $"<b>{(temp != null ? $"{temp:F2}°" : "non disponibile 😵")}</b>".PadLeft(40);
            msg.AppendLine($"Host: {tempString}");
            foreach (var r in externalReadings)
            {
                tempString = $"<b>{r.Temperature:F2}° ({r.Timestamp.Humanize()})</b>".PadLeft(40);
                msg.AppendLine($"{r.Location.Trim()}: {tempString}");
            }
            return msg.ToString();
        }
        else
        {
            return $"🌡️ Sto calcolando le temperature...";
        }
    }

    protected override async Task ExecuteInternal()
    {
        AttachEvents();
        await Arguments.Client.SendChatAction(Arguments.Message.Chat.Id, ChatAction.Typing);
        var msg = await GetHtmlStatusMessage(false);
        kbd.AddButton("Chiudi", EncodeCallbackQueryData("cancel"));
        var message = await Arguments
            .Client.SendMessage(Arguments.Message.Chat.Id, msg, replyMarkup: kbd, parseMode: ParseMode.Html, disableNotification: true)
            .DeleteOnDetach(this);
        await UpdateStatusMessage(message);
        refreshTimer.Elapsed += (_, _) => _ = UpdateStatusMessage(message);
        refreshTimer.Start();
    }

    private async Task UpdateStatusMessage(Message? message)
    {
        if (message == null)
            return;

        try
        {
            await Arguments.Client.EditMessageText(
                message.Chat.Id,
                message.Id,
                await GetHtmlStatusMessage(true),
                replyMarkup: kbd,
                parseMode: ParseMode.Html
            );
        }
        catch (Exception) { }
    }

    protected override async Task OnCallback(Update update, string callbackData)
    {
        if (callbackData == "cancel")
        {
            await DetachEvents();
        }
    }
}
