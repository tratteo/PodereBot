using PodereBot.Lib.Commands;

namespace PodereBot.Lib;

internal static class Registry
{
    public static readonly List<CommandRegistryKey> commands =
    [
        new CommandRegistryKey<StartCommand>("/start", "Letsgo"),
        new CommandRegistryKey<ToggleGatesLightCommand>(
            "/gatelight",
            "Accendo/spengo le luci dei cancelli 💡",
            admin: true
        ),
        new CommandRegistryKey<OpenAutomaticGateCommand>(
            "/openauto",
            "Ti apro il cancello automatico (forse 😼)"
        ),
        new CommandRegistryKey<OpenPedestrianGateCommand>(
            "/openped",
            "Ti apro il cancello pedonale (forse 😼)"
        ),
        new CommandRegistryKey<SendPositionCommand>("/sendpos", "Ti mando la posizione di casa 📍"),
        new CommandRegistryKey<UnlockGatesCommand>(
            "/gates",
            "Abilito o disabilito l'apertura dei cancelli agli utenti 🔐",
            admin: true
        )
    ];
}
