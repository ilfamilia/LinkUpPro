using AutoMapper;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.ViewModels.Comment;
using LinkUpPro.Core.Application.ViewModels.Post;
using LinkUpPro.Core.Domain.Exceptions;
using LinkUpPro.ViewModels.Social;

namespace LinkUpPro.Services;

/// <summary>
/// Builds full post cards (multimedia, reactions and comment threads) for the social screens by
/// reusing the publication module's services, so visibility is always rechecked by its owner.
/// </summary>
public sealed class PostFeedBuilder(IPostService posts, ICommentService comments, IUserDirectoryService users,
    IFriendshipService friends, ICurrentUserService currentUser, IMapper mapper)
{
    /// <summary>Loads the visible posts among the given ids, newest first, with authors and comments.</summary>
    public async Task<List<PostCardViewModel>> BuildAsync(IEnumerable<int> postIds)
    {
        string userId = currentUser.GetRequiredUserId();
        int[] ids = postIds.Distinct().ToArray();
        if (ids.Length == 0) return [];

        var postsResult = await posts.GetVisiblePostsByIdsAsync(ids, userId);
        if (!postsResult.IsSuccess || postsResult.Data is null)
            throw new DomainRuleException("No se pudieron cargar las publicaciones.");

        List<PostViewModel> postVms = mapper.Map<List<PostViewModel>>(postsResult.Data);
        if (postVms.Count == 0) return [];

        var commentsResult = await comments.GetCommentsByPostIdsAsync(postVms.Select(p => p.Id), userId);
        if (!commentsResult.IsSuccess || commentsResult.Data is null)
            throw new DomainRuleException("No se pudieron cargar los comentarios.");

        foreach (PostViewModel post in postVms)
        {
            if (commentsResult.Data.TryGetValue(post.Id, out var postComments))
                post.Comments = mapper.Map<List<CommentViewModel>>(postComments);
        }

        List<CommentViewModel> allComments = Flatten(postVms.SelectMany(p => p.Comments)).ToList();

        // One batch lookup for post and comment authors keeps names and photos current.
        var authorIds = postVms.Select(p => p.UserId).Concat(allComments.Select(c => c.UserId));
        Dictionary<string, PublicUserViewModel> authors = mapper
            .Map<List<PublicUserViewModel>>(await users.GetByIdsAsync(authorIds))
            .ToDictionary(u => u.Id);

        foreach (CommentViewModel comment in allComments.Where(c => !c.IsDeleted))
        {
            if (authors.TryGetValue(comment.UserId, out var author))
            {
                comment.UserName = author.UserName;
                comment.UserProfilePicturePath = author.ProfilePicturePath;
            }
        }

        return postVms.Select(post =>
        {
            var author = authors.GetValueOrDefault(post.UserId) ?? new PublicUserViewModel { Id = post.UserId };
            post.UserName = author.UserName;
            post.UserProfilePicturePath = author.ProfilePicturePath;
            return new PostCardViewModel { Post = post, Author = author, IsOwner = post.UserId == userId };
        }).ToList();
    }

    /// <summary>Exposes the trusted current user id so the views can show owner-only actions.</summary>
    public string CurrentUserId => currentUser.GetRequiredUserId();

    /// <summary>Returns the caller's active friend ids so only friends' names link to their profiles.</summary>
    public async Task<HashSet<string>> GetFriendIdsAsync()
        => (await friends.GetActiveFriendIdsAsync(currentUser.GetRequiredUserId())).ToHashSet();

    private static IEnumerable<CommentViewModel> Flatten(IEnumerable<CommentViewModel> source)
    {
        foreach (CommentViewModel comment in source)
        {
            yield return comment;
            foreach (CommentViewModel reply in Flatten(comment.Replies)) yield return reply;
        }
    }
}
