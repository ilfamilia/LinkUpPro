namespace LinkUpPro.Core.Domain.Queries
{
    /// <summary>Like and dislike totals of one post.</summary>
    public class PostReactionCount
    {
        public int PostId { get; set; }
        public int LikesCount { get; set; }
        public int DislikesCount { get; set; }
    }
}
