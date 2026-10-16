using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Domain.Entities
{
    public class Reaction
    {
        public required string UserId { get; set; }

        public required ReactionType Type { get; set; }

        public required int PostId { get; set; }

        public Post? Post { get; set; }
    }
}
