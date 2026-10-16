using LinkUpPro.Core.Domain.Common;
using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Domain.Entities
{
    public class FriendshipRequest : BasicEntity<int>
    {
        public required string SenderUserId { get; set; }

        public required string ReceiverUserId { get; set; }

        public required FriendshipRequestStatus Status { get; set; }

        public required DateTime RequestedAt { get; set; }

        public DateTime? RespondedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public bool IsVisibleToSender { get; set; }
    }
}
