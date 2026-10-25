using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Dtos.Social;

/// <summary>Transfers a visible friend publication to the shared publication integration surface.</summary>
public sealed class FriendPostDto
{
    public int Id { get; set; }
    public UserSummaryDto Author { get; set; } = new();
    public string Content { get; set; } = "";
    public PostMediaType MediaType { get; set; }
    public string? ImagePath { get; set; }
    public string? YouTubeUrl { get; set; }
    public bool AllowComments { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int LikeCount { get; set; }
    public int DislikeCount { get; set; }
}
