using PodereBot.Lib.Commands;

namespace PodereBot.Lib;

internal static class Registry
{
    public static readonly List<CommandRegistryKey> commands =
    [
        new CommandRegistryKey<StartCommand>("/start", "Letsgo"),
        new CommandRegistryKey<OpenAutomaticGateCommand>(
            "/openauto",
            "Apri il cancello automatico"
        ),
        new CommandRegistryKey<OpenPedestrianGateCommand>(
            "/openped",
            "Apri il cancello pedonale",
            admin: true
        ),
        new CommandRegistryKey<SendPositionCommand>("/sendpos", "Ti mando la posizione di casa"),
        new CommandRegistryKey<UnlockGatesCommand>(
            "/gates",
            "Abilita o disabilita l'apertura dei cancelli agli utenti",
            admin: true
        )
    ];
}
