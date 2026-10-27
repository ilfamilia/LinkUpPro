using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.ViewModels.FriendshipRequest
{
    public class FriendshipRequestListViewModel
    {
        public int Id { get; set; }
        public FriendshipUserViewModel OtherUser { get; set; } = new();
        public FriendshipRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public int MutualFriendsCount { get; set; }
    }
}
