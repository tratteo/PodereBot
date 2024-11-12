using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

internal class ConversationalResponder(ILogger<ConversationalResponder> logger)
{
    private class Trigger
    {
        public required Regex Regex { get; init; }
        public required Func<TelegramBotClient, Message, Task> Callback { get; init; }
    }

    private static readonly Trigger[] triggers = [];
    private readonly ILogger<ConversationalResponder> logger = logger;

    public async Task Process(TelegramBotClient client, Message message)
    {
        if (message.Text == null)
            return;
        var match = triggers.FirstOrDefault(t => t.Regex.Match(message.Text).Success);
        if (match != null)
        {
            await match.Callback.Invoke(client, message);
        }
    }
}
