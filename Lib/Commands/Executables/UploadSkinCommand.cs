using Newtonsoft.Json;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Lib.Commands;

//[CommandMetadata(Key = "/uploadskin", Description = "Carica una nuova skin 🆕")]
internal class UploadSkinCommand(ILogger<UploadSkinCommand> logger, Skin skin, IConfiguration configuration)
    : Command(skin, logger, configuration)
{
    private readonly ILogger<UploadSkinCommand> logger = logger;

    protected override async Task ExecuteInternal()
    {
        AttachEvents();
        await Arguments
            .Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"""
            Per impostare una nuova skin, devi caricare un file JSON con il seguente formato: 
            <pre langauge='json'>{JsonConvert.SerializeObject(typeof(SkinSchema).ReflectSchema(), Formatting.Indented)}</pre>
            I campi <code>Source</code> devono essere url disponibili pubblicamente.
            Carica il file in chat appena sei pronto.
            """,
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup().AddButton("Chiudi", EncodeCallbackQueryData("cancel")),
                disableNotification: true
            )
            .DeleteOnDetach(this);
    }

    protected override async Task OnCallback(Update update, string callbackData)
    {
        if (callbackData == "cancel")
        {
            await DetachEvents();
        }
    }

    protected override async Task OnMessage(Message message, UpdateType type)
    {
        if (type != UpdateType.Message || message.Document == null)
            return;

        var tmpPath = Path.Join(AppContext.BaseDirectory, "tmp", $"{Guid.NewGuid()}_{message.Document.FileName}");
        var destPath = Path.Join(AppContext.BaseDirectory, Globals.SKINS_PATH, message.Document.FileName);
        Directory.CreateDirectory(Path.GetDirectoryName(tmpPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        try
        {
            await Arguments.Client.SendChatAction(Arguments.Message.Chat.Id, ChatAction.UploadDocument);
            var file = await Arguments.Client.GetFile(message.Document!.FileId);
            using (var fileStream = new FileStream(tmpPath, FileMode.Create))
            {
                await Arguments.Client.DownloadFile(file.FilePath!, fileStream);
            }
            // await Arguments.Client.SendMessage(Arguments.Message.Chat.Id, "File scaricato");
            var content = await System.IO.File.ReadAllTextAsync(tmpPath);
            var deserialized = JsonConvert.DeserializeObject<SkinSchema>(content);
            System.IO.File.Move(tmpPath, destPath);
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"""
                🟢 Ho caricato e salvato la skin {deserialized!.Metadata.Name}
                """
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning("error processing file: {e}", ex.Message);
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"""
                🔴 Non sono riuscito a processare il file, sta tutto buggato.
                <code>{ex.Message}</code>
                """,
                parseMode: ParseMode.Html
            );
        }

        System.IO.File.Delete(tmpPath);
        await DetachEvents();
    }
}
