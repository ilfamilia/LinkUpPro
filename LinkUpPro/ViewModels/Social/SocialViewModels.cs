using System.ComponentModel.DataAnnotations;
using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.ViewModels.Social;

/// <summary>Contains public profile fields rendered by the social screens.</summary>
public sealed class PublicUserViewModel
{
    public string Id { get; set; } = "";
    public string UserName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string ProfilePicturePath { get; set; } = "";
}

/// <summary>Displays one active friend and the mutual-friend navigation count.</summary>
public sealed class FriendItemViewModel
{
    public PublicUserViewModel User { get; set; } = new();
    public int MutualFriendCount { get; set; }
}

/// <summary>Displays an owned request without binding its security-sensitive persistence fields.</summary>
public sealed class RequestItemViewModel
{
    public int Id { get; set; }
    public PublicUserViewModel OtherUser { get; set; } = new();
    public int MutualFriendCount { get; set; }
    public FriendshipRequestStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

/// <summary>Groups received and sent requests on the main request screen.</summary>
public sealed class RequestListViewModel
{
    public List<RequestItemViewModel> Incoming { get; set; } = [];
    public List<RequestItemViewModel> Sent { get; set; } = [];
}

/// <summary>Validates a single eligible user selected by a radio button.</summary>
public sealed class NewRequestViewModel
{
    [Required(ErrorMessage = "Debe seleccionar un usuario para enviar la solicitud de amistad.")]
    [StringLength(36)]
    public string? SelectedUserId { get; set; }
    public string? Search { get; set; }
    public List<FriendItemViewModel> Candidates { get; set; } = [];
}

/// <summary>Restricts request confirmation submissions to the supported transitions.</summary>
public enum RequestOperation { Accept, Reject, Cancel, Hide }

/// <summary>Binds only the request identifier and requested action; services validate ownership and state.</summary>
public sealed class RequestActionViewModel
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
    [EnumDataType(typeof(RequestOperation))]
    public RequestOperation Operation { get; set; }
    public string OtherUserName { get; set; } = "";
}

/// <summary>Validates publication filters before sending them to the visibility service.</summary>
public sealed class FriendPostFilterViewModel : IValidatableObject
{
    public string? Search { get; set; }
    public string? FriendId { get; set; }
    [EnumDataType(typeof(PostMediaType))]
    public PostMediaType? MediaType { get; set; }
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }
    [DataType(DataType.Date)]
    public DateTime? Until { get; set; }
    public bool? IsEdited { get; set; }

    /// <summary>Rejects reversed date ranges while allowing either endpoint independently.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From?.Date > Until?.Date)
            yield return new("La fecha inicial no puede ser posterior a la fecha final.", [nameof(From), nameof(Until)]);
    }
}

/// <summary>Wraps a publication module post with its public author for the shared post card.</summary>
public sealed class PostCardViewModel
{
    public LinkUpPro.Core.Application.ViewModels.Post.PostViewModel Post { get; set; } = new();
    public PublicUserViewModel Author { get; set; } = new();
    public bool IsOwner { get; set; }
}

/// <summary>Combines friendship management and authorized publication filters in one screen.</summary>
public sealed class FriendsPageViewModel
{
    public List<FriendItemViewModel> Friends { get; set; } = [];
    public List<FriendItemViewModel> AllFriends { get; set; } = [];
    public List<PostCardViewModel> Posts { get; set; } = [];
    public FriendPostFilterViewModel Filter { get; set; } = new();
    public string? FriendSearch { get; set; }
    public int AvailablePostCount { get; set; }
    public PublicUserViewModel? Profile { get; set; }
    public int MutualFriendCount { get; set; }
}

/// <summary>Displays one notification without exposing the target resource until authorization is rechecked.</summary>
public sealed class NotificationItemViewModel
{
    public int Id { get; set; }
    public PublicUserViewModel Actor { get; set; } = new();
    public NotificationType Type { get; set; }
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

/// <summary>Supplies a generic local confirmation screen with a server-selected target.</summary>
public sealed class SocialConfirmationViewModel
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
}

/// <summary>Displays a safe error without stack traces, SQL, or private resource data.</summary>
public sealed class SocialErrorViewModel
{
    public string Message { get; set; } = "";
}

/// <summary>Supplies per-request navigation counts without automatic polling.</summary>
public sealed class SocialNavigationViewModel
{
    public int PendingRequests { get; set; }
    public int UnreadNotifications { get; set; }
}
