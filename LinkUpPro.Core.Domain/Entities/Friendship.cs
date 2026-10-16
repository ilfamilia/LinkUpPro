using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Domain.Entities
{
    public class Friendship
    {
        public required string User1Id { get; set; }

        public required string User2Id { get; set; }

        public required FriendshipStatus Status { get; set; }
    }
}
