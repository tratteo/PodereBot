using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PodereBot.Lib.Commands;

internal readonly struct CommandArguments
{
    public readonly TelegramBotClient Client { get; init; }

    public readonly Message Message { get; init; }

    public readonly bool Admin { get; init; }
}

internal abstract class Command(SkinService skin, IConfiguration configuration)
{
    protected readonly Guid instanceId = Guid.NewGuid();
    protected readonly SkinService skin = skin;
    protected readonly IConfiguration configuration = configuration;

    public async Task Execute(CommandArguments arguments)
    {
        if (arguments.Admin)
        {
            var admins = configuration.GetSection("Admins").Get<long[]>()?.ToList() ?? [];
            if (!admins.Contains(arguments.Message.From!.Id))
            {
                await arguments.Client.SendChatAction(
                    arguments.Message.Chat.Id,
                    ChatAction.ChooseSticker
                );
                await arguments.Client.SendAsset(arguments.Message, skin.Schema.Unauthorized);
                await arguments.Client.SendMessage(
                    arguments.Message.Chat.Id,
                    "Non hai abbastanza poteri canide 🐶"
                );
                return;
            }
        }
        await ExecuteInternal(arguments);

        await arguments.Client.SetMessageReaction(
            arguments.Message.Chat.Id,
            arguments.Message.MessageId,
            [arguments.Message.RandomEmoji()]
        );
    }

    protected string EncodeCallbackQueryData(object data) =>
        $"{instanceId}_{JsonConvert.SerializeObject(data)}";

    protected bool DecodeCallbackQueryData(string? data, out dynamic? value)
    {
        value = null;
        if (data == null)
            return false;
        var splits = data.Split("_");
        if (splits.Length <= 1)
            return false;

        var id = splits[0];
        if (id != instanceId.ToString())
            return false;
        var content = string.Join("_", splits.Skip(1));
        value = JsonConvert.DeserializeObject<dynamic>(content);
        return true;
    }

    protected abstract Task ExecuteInternal(CommandArguments arguments);
}

internal class CommandRegistryKey(
    string command,
    string description,
    Type commandType,
    bool admin = false
)
{
    public readonly string command = command;
    public readonly string description = description;
    public readonly bool admin = admin;
    public readonly Type commandType = commandType;
}

internal class CommandRegistryKey<T>(string command, string description, bool admin = false)
    : CommandRegistryKey(command, description, typeof(T), admin)
    where T : Command
{ }