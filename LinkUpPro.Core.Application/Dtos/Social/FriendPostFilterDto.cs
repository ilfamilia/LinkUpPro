using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Dtos.Social;

/// <summary>Describes combinable filters that never override publication authorization.</summary>
public sealed class FriendPostFilterDto
{
    public string? Search { get; set; }
    public string? FriendId { get; set; }
    public PostMediaType? MediaType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? Until { get; set; }
    public bool? IsEdited { get; set; }
}
