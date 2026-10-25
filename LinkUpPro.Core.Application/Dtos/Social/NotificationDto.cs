using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Dtos.Social;

/// <summary>Displays an interaction notification belonging to the current recipient.</summary>
public sealed class NotificationDto
{
    public int Id { get; set; }
    public UserSummaryDto Actor { get; set; } = new();
    public NotificationType Type { get; set; }
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
