namespace PodereBot.Services.AiTools;

internal static class AdminAuthorization
{
    public static bool TryAuthorize(IConfiguration configuration, long? userId)
    {
        if (userId == null)
            return false;
        var admins = configuration.GetSection("Admins").Get<long[]>() ?? [];
        return admins.Contains(userId.Value);
    }
}
