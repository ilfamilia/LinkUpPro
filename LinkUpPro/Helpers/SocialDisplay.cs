using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Helpers;

/// <summary>Provides presentation labels and validates YouTube identifiers before constructing an iframe URL.</summary>
public static class SocialDisplay
{
    /// <summary>Renders the request states required by the functional document.</summary>
    public static string RequestStatus(FriendshipRequestStatus status) => status switch
    {
        FriendshipRequestStatus.Pending => "En espera de respuesta",
        FriendshipRequestStatus.Accepted => "Aceptada",
        FriendshipRequestStatus.Rejected => "Rechazada",
        _ => "Cancelada"
    };

    /// <summary>Renders the activity type shown on each notification.</summary>
    public static string NotificationType(NotificationType type) => type switch
    {
        Core.Domain.Enums.NotificationType.Comment => "Comentario",
        Core.Domain.Enums.NotificationType.Reply => "Respuesta",
        _ => "Reacción"
    };

    /// <summary>Renders the reason a finished game ended.</summary>
    public static string EndReason(GameEndReason? reason) => reason switch
    {
        GameEndReason.Surrender => "Rendición",
        GameEndReason.Abandonment => "Abandono (48 horas sin atacar)",
        GameEndReason.FleetDestroyed => "Flota hundida",
        _ => ""
    };

    /// <summary>Resolves stored relative image paths the same way as Home, with the default profile picture as fallback.</summary>
    public static string ImageUrl(Microsoft.AspNetCore.Mvc.IUrlHelper url, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return url.Content("~/Images/Defaults/default-profile.png");
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase) || path.StartsWith('/')) return path;
        return url.Content($"~/{path}");
    }

    /// <summary>Creates an embed URL only for an allowed host and a valid eleven-character video identifier.</summary>
    public static string? YouTubeEmbed(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")) return null;
        string? id = null;
        if (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
            id = uri.AbsolutePath.Trim('/');
        else if (new[] { "youtube.com", "www.youtube.com", "m.youtube.com" }.Contains(uri.Host.ToLowerInvariant()))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length == 2 && segments[0] is "embed" or "shorts") id = segments[1];
            else if (uri.AbsolutePath == "/watch")
                id = uri.Query.TrimStart('?').Split('&').Select(p => p.Split('=', 2))
                    .FirstOrDefault(p => p.Length == 2 && p[0] == "v")?.Last();
        }
        return id is not null && System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-zA-Z0-9_-]{11}$")
            ? $"https://www.youtube-nocookie.com/embed/{id}" : null;
    }
}
