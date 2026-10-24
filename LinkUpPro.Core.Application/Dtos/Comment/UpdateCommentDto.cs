namespace LinkUpPro.Core.Application.Dtos.Comment
{
    public class UpdateCommentDto
    {
        public required string UserId { get; set; }

        public required string Content { get; set; }
    }
}