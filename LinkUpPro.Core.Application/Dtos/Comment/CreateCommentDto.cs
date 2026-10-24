namespace LinkUpPro.Core.Application.Dtos.Comment
{
    public class CreateCommentDto
    {
        public required string UserId { get; set; }

        public required string Content { get; set; }

        public int PostId { get; set; }

        public int? ParentCommentId { get; set; }
    }
}