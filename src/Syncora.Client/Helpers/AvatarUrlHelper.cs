using System;

namespace Syncora.Client.Helpers;

public static class AvatarUrlHelper
{
    private const string ApiBaseUrl = "http://localhost:5131";

    public static string? ToAbsolute(string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
            return null;

        if (avatarUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || avatarUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return avatarUrl;

        return $"{ApiBaseUrl.TrimEnd('/')}/{avatarUrl.TrimStart('/')}";
    }
}
