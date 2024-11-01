using Microsoft.Extensions.DependencyInjection;

namespace PodereBot.Lib;
public static class Extensions
{
    public static void AddCommands(this IServiceCollection services)
    {
        foreach (var c in Registry.commands)
        {
            services.AddTransient(c.commandType);
        }
    }
}
