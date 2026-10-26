using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Dtos.FriendshipRequest
{
    public class FriendshipRequestListDto
    {
        public int Id { get; set; }
        public required FriendshipUserDto OtherUser { get; set; }
        public FriendshipRequestStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public int MutualFriendsCount { get; set; }
    }
}
