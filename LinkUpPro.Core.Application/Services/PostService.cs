using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Post;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Enums;
using LinkUpPro.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Core.Application.Services
{
    public class PostService : BaseService, IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IReactionRepository _reactionRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly IMapper _mapper;

        public PostService(IPostRepository postRepository, IMapper mapper, IFriendshipRepository friendshipRepository, 
            IReactionRepository reactionRepository, ICommentRepository commentRepository)
        {
            _postRepository = postRepository;
            _friendshipRepository = friendshipRepository;
            _reactionRepository = reactionRepository;
            _commentRepository = commentRepository;
            _mapper = mapper;
        }

        public async Task<ServiceResult> AddAsync(CreatePostDto dto)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(dto.UserId))
                {
                    return ServiceResult.Failure(OperationStatus.Unauthorized,
                        "El usuario no está autenticado");
                }

                if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Trim().Length > 1000)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "El contenido debe contener entre 1 y 1000 caracteres");
                }

                if (!Enum.IsDefined(dto.Privacy) || !Enum.IsDefined(dto.MediaType))
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "Tipo de privacidad o media inválidos");
                }

                if (dto.MediaType == PostMediaType.Image)
                {
                    if (string.IsNullOrWhiteSpace(dto.ImagePath) || !string.IsNullOrWhiteSpace(dto.YouTubeUrl))
                    {
                        return ServiceResult.Failure(OperationStatus.ValidationError,
                            "La publicación de una imagen solo debe contener la imagen sin video");
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(dto.ImagePath) || GetYouTubeVideoId(dto.YouTubeUrl) is null)
                    {
                        return ServiceResult.Failure(OperationStatus.ValidationError,
                            "La publicación de un video debe tener una URL de YouTube válida y no debe incluir una imagen");
                    }
                }

                Post post = _mapper.Map<Post>(dto);

                post.Content = dto.Content.Trim();
                post.CreatedAt = DateTime.UtcNow;
                post.UpdatedAt = null;
                post.IsDeleted = false;
                post.ImagePath = dto.MediaType == PostMediaType.Image ? dto.ImagePath : null;
                post.YouTubeUrl = dto.MediaType == PostMediaType.Image ? null : dto.YouTubeUrl?.Trim();

                Post createdPost = await _postRepository.AddAsync(post);

                return ServiceResult.Success();
            });
        }

        public async Task<ServiceResult> UpdateAsync(UpdatePostDto dto, int postId)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(dto.UserId))
                {
                    return ServiceResult.Failure(OperationStatus.Unauthorized,
                        "El usuario no está autenticado");
                }

                if (postId <= 0)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "El ID de la publicación es inválido");
                }

                Post? post = await _postRepository.GetByIdAsync(postId);

                if (post == null || post.IsDeleted)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "La publicación no fue encontrada");
                }

                if (post.UserId != dto.UserId)
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No puedes editar las publicaciones de otras personas");
                }

                if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Trim().Length > 1000)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "El contenido debe contener entre 1 y 1000 caracteres");
                }

                if (!Enum.IsDefined(dto.Privacy) || !Enum.IsDefined(dto.MediaType))
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "Tipo de privacidad o media inválidos");
                }

                string? imagePath = null;

                if (dto.MediaType == PostMediaType.Image)
                {
                    imagePath = !string.IsNullOrWhiteSpace(dto.ImagePath) ? dto.ImagePath
                        : post.MediaType == PostMediaType.Image ? post.ImagePath : null;

                    if (string.IsNullOrWhiteSpace(imagePath) || !string.IsNullOrWhiteSpace(dto.YouTubeUrl))
                    {
                        return ServiceResult.Failure(OperationStatus.ValidationError,
                            "La publicación de una imagen solo debe contener la imagen sin video");
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(dto.ImagePath) || GetYouTubeVideoId(dto.YouTubeUrl) is null)
                    {
                        return ServiceResult.Failure(OperationStatus.ValidationError,
                            "La publicación de un video debe tener una URL de YouTube válida y no debe incluir una imagen");
                    }
                }

                _mapper.Map(dto, post);

                post.Content = dto.Content.Trim();
                post.ImagePath = imagePath;
                post.UpdatedAt = DateTime.UtcNow;
                post.YouTubeUrl = dto.MediaType == PostMediaType.Image ? null : dto.YouTubeUrl?.Trim();

                Post? updatedPost = await _postRepository.UpdateAsync(post, postId);

                if (updatedPost == null)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "La publicación no fue encontrada");
                }

                return ServiceResult.Success();
            });
        }

        public async Task<ServiceResult> DeleteAsync(int postId, string userId)
        {
            return await ExecuteAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ServiceResult.Failure(OperationStatus.Unauthorized,
                        "El usuario no está autenticado");
                }

                if (postId <= 0)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "El ID de la publicación es inválido");
                }

                Post? post = await _postRepository.GetByIdAsync(postId);

                if (post == null || post.IsDeleted)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "La publicación no fue encontrada");
                }

                if (post.UserId != userId)
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No puedes eliminar publicaciones de otras personas");
                }

                post.IsDeleted = true;
                post.UpdatedAt = DateTime.UtcNow;

                Post? updatedPost = await _postRepository.UpdateAsync(post, postId);

                if (updatedPost == null)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "La publicación no fue encontrada");
                }

                return ServiceResult.Success();
            });
        }

        public async Task<ServiceResultWithData<PostDto>> GetMyPostByIdAsync(int postId, string userId)
        {
            return await ExecuteAsync<PostDto>(async () =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ServiceResultWithData<PostDto>.Failure(OperationStatus.Unauthorized,
                        "El usuario no está autenticado");
                }

                if (postId <= 0)
                {
                    return ServiceResultWithData<PostDto>.Failure(OperationStatus.ValidationError,
                        "El ID de la publicación es inválido");
                }

                Post? post = await _postRepository.GetAllQuery()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

                if (post == null)
                {
                    return ServiceResultWithData<PostDto>.Failure(OperationStatus.NotFound,
                        "La publicación no fue encontrada");
                }

                if (post.UserId != userId)
                {
                    return ServiceResultWithData<PostDto>.Failure(OperationStatus.Forbidden,
                        "No puedes editar publicaciones de otras personas");
                }

                PostDto postDto = _mapper.Map<PostDto>(post);

                return ServiceResultWithData<PostDto>.Success(postDto);
            });
        }

        public async Task<ServiceResultWithData<PostDto>> GetVisiblePostByIdAsync(int postId, string userId)
        {
            return await ExecuteAsync<PostDto>(async () => 
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ServiceResultWithData<PostDto>.Failure(OperationStatus.Unauthorized,
                        "El usuario no está autenticado");
                }

                if (postId <= 0)
                {
                    return ServiceResultWithData<PostDto>.Failure(OperationStatus.ValidationError,
                        "El ID de la publicación es inválido");
                }

                var friendshipsQuery = _friendshipRepository.GetAllQuery()
                    .Where(f => f.Status == FriendshipStatus.Active &&
                        (f.User1Id == userId || f.User2Id == userId));

                Post? post = await _postRepository.GetAllQuery()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted &&
                        (
                            p.UserId == userId ||
                            (
                                p.Privacy == PostPrivacy.FriendsOnly &&
                                friendshipsQuery.Any(f =>
                                    (f.User1Id == userId && f.User2Id == p.UserId) ||
                                    (f.User2Id == userId && f.User1Id == p.UserId))
                            )
                        ));

                if (post == null)
                {
                    return ServiceResultWithData<PostDto>.Failure(OperationStatus.NotFound,
                        "La publicación no pudo ser encontrada");
                }

                PostDto postDto = _mapper.Map<PostDto>(post);

                List<PostDto> postDtos = [postDto];

                await PopulatePostMetricsAsync(postDtos, userId);

                PopulateYouTubeEmbedUrls(postDtos);

                return ServiceResultWithData<PostDto>.Success(postDto);
            });
        }


        public async Task<ServiceResultWithData<List<PostDto>>> GetMyPostsAsync(string userId, PostFilterDto? filter = null)
        {
            return await ExecuteAsync<List<PostDto>>(async () =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ServiceResultWithData<List<PostDto>>.Failure(OperationStatus.Unauthorized,
                        "El usuario no está autenticado");
                }

                IQueryable<Post> query = _postRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(p => p.UserId == userId && !p.IsDeleted);

                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter.SearchText))
                    {
                        string searchText = filter.SearchText.Trim();

                        query = query.Where(p => p.Content.Contains(searchText));
                    }

                    if (filter.MediaType.HasValue)
                    {
                        query = query.Where(p => p.MediaType == filter.MediaType.Value);
                    }

                    if (filter.StartDate.HasValue)
                    {
                        DateTime startDate = filter.StartDate.Value
                            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

                        query = query.Where(p => p.CreatedAt >= startDate);
                    }

                    if (filter.EndDate.HasValue)
                    {
                        DateTime endDate = filter.EndDate.Value
                            .ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

                        query = query.Where(p => p.CreatedAt <= endDate);
                    }

                    if (filter.IsEdited.HasValue)
                    {
                        bool isEdited = filter.IsEdited.Value;

                        query = query.Where(p => (p.UpdatedAt != null) == isEdited);
                    }
                }

                List<Post> posts = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

                List<PostDto> postDtos = _mapper.Map<List<PostDto>>(posts);

                await PopulatePostMetricsAsync(postDtos, userId);

                PopulateYouTubeEmbedUrls(postDtos);

                return ServiceResultWithData<List<PostDto>>.Success(postDtos);
            });
        }


        public async Task<ServiceResultWithData<List<PostDto>>> GetFeedAsync(string userId)
        {
            return await ExecuteAsync<List<PostDto>>(async () => 
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ServiceResultWithData<List<PostDto>>.Failure(OperationStatus.Unauthorized,
                        "El usuario no está autenticado");
                }

                var friendshipsQuery = _friendshipRepository.GetAllQuery()
                    .Where(f => f.Status == FriendshipStatus.Active &&
                        (f.User1Id == userId || f.User2Id == userId));

                List<Post> posts = await _postRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted &&
                        (
                            p.UserId == userId ||
                            (
                                p.Privacy == PostPrivacy.FriendsOnly &&
                                friendshipsQuery.Any(f =>
                                    (f.User1Id == userId && f.User2Id == p.UserId) ||
                                    (f.User2Id == userId && f.User1Id == p.UserId))
                            )
                        ))
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                List<PostDto> postDtos = _mapper.Map<List<PostDto>>(posts);

                await PopulatePostMetricsAsync(postDtos, userId);

                PopulateYouTubeEmbedUrls(postDtos);

                return ServiceResultWithData<List<PostDto>>.Success(postDtos);
            });
        }

        private async Task PopulatePostMetricsAsync(List<PostDto> postDtos, string userId)
        {
            if (postDtos.Count == 0)
            {
                return;
            }

            List<int> postIds = postDtos.Select(p => p.Id).ToList();

            var reactionCounts = await _reactionRepository.GetAllQuery()
                .Where(r => postIds.Contains(r.PostId))
                .GroupBy(r => r.PostId)
                .Select(g => new
                {
                    PostId = g.Key,
                    LikesCount = g.Count(r => r.Type == ReactionType.Like),
                    DislikesCount = g.Count(r => r.Type == ReactionType.Dislike)
                })
                .ToDictionaryAsync(r => r.PostId);

            var commentCounts = await _commentRepository.GetAllQuery()
                .Where(c => postIds.Contains(c.PostId) && !c.IsDeleted)
                .GroupBy(c => c.PostId)
                .Select(g => new
                {
                    PostId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(c => c.PostId, c => c.Count);

            var userReactions = await _reactionRepository.GetAllQuery()
                .Where(r => r.UserId == userId && postIds.Contains(r.PostId))
                .ToDictionaryAsync(r => r.PostId, r => r.Type);

            foreach (PostDto dto in postDtos)
            {
                if (reactionCounts.TryGetValue(dto.Id, out var counts))
                {
                    dto.LikesCount = counts.LikesCount;
                    dto.DislikesCount = counts.DislikesCount;
                }

                if (commentCounts.TryGetValue(dto.Id, out int comments))
                {
                    dto.CommentsCount = comments;
                }

                dto.CurrentUserReaction = userReactions.TryGetValue(dto.Id, out ReactionType reaction)
                    ? reaction : null;
            }
        }

        private static string? GetYouTubeVideoId(string? url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
                uri.Scheme != Uri.UriSchemeHttps)
            {
                return null;
            }

            string host = uri.Host.ToLowerInvariant();
            string path = uri.AbsolutePath.Trim('/');

            if (host is "youtu.be" or "www.youtu.be")
            {
                return IsValidYouTubeVideoId(path) ? path : null;
            }

            if (host is "youtube.com" or "www.youtube.com" or "m.youtube.com")
            {
                if (path == "watch")
                {
                    return uri.Query.TrimStart('?')
                        .Split('&')
                        .Where(parameter => parameter.StartsWith("v=", StringComparison.Ordinal))
                        .Select(parameter => parameter[2..])
                        .FirstOrDefault(IsValidYouTubeVideoId);
                }

                string[] segments = path.Split('/');

                if (segments.Length == 2 &&
                    (segments[0] is "shorts" or "embed" or "live") &&
                    IsValidYouTubeVideoId(segments[1]))
                {
                    return segments[1];
                }
            }

            return null;
        }

        private static bool IsValidYouTubeVideoId(string videoId)
        {
            return videoId.Length == 11 && videoId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
        }

        private static void PopulateYouTubeEmbedUrls(List<PostDto> postDtos)
        {
            foreach (PostDto dto in postDtos)
            {
                if (dto.MediaType != PostMediaType.YouTubeVideo)
                {
                    continue;
                }

                string? videoId = GetYouTubeVideoId(dto.YouTubeUrl);

                if (videoId != null)
                {
                    dto.YouTubeEmbedUrl = $"https://www.youtube.com/embed/{videoId}";
                }
            }
        }
    }
}