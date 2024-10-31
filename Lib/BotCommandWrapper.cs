using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PodereBot.Lib;

internal readonly struct CommandHandlerArguments
{
    public readonly TelegramBotClient Client { get; init; }

    public readonly Message Message { get; init; }

    public readonly IServiceProvider Services { get; init; }
}

internal class BotCommandWrapper(BotCommand cmd, Func<CommandHandlerArguments, Task> handler)
{
    public readonly BotCommand cmd = cmd;
    public readonly Func<CommandHandlerArguments, Task> handler = handler;

    public Task ProtectedHandler(CommandHandlerArguments args, ILogger? logger = null)
    {
        try
        {
            return handler.Invoke(args);
        }
        catch (Exception ex)
        {
            logger?.LogError("error executing command {c}:\n{e}", cmd.Command, ex.Message);
            return Task.CompletedTask;
        }
    }
}