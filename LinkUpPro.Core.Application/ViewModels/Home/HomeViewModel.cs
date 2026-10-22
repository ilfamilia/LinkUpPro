using LinkUpPro.Core.Application.ViewModels.Post;

namespace LinkUpPro.Core.Application.ViewModels.Home
{
    public class HomeViewModel
    {
        public CreatePostViewModel CreatePost { get; set; } = new();

        public List<PostViewModel> Posts { get; set; } = [];
    }
}