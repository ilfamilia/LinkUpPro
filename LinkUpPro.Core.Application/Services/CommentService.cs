using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Comment;
using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Core.Application.Services
{

    public class CommentService : BaseService, ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IPostService _postService;
        private readonly IUserAccountStatusService _userAccountStatusService;
        private readonly IMapper _mapper;

        public CommentService(ICommentRepository commentRepository, IPostService postService,
            IUserAccountStatusService userAccountStatusService, IMapper mapper)
        {
            _commentRepository = commentRepository;
            _postService = postService;
            _userAccountStatusService = userAccountStatusService;
            _mapper = mapper;
        }

        public async Task<ServiceResult> AddAsync(CreateCommentDto dto)
        {
            return await ExecuteAsync(async () =>
            {
                if (dto == null)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "Los datos del comentario son inválidos");
                }

                if (string.IsNullOrWhiteSpace(dto.UserId))
                {
                    return ServiceResult.Failure(OperationStatus.Unauthorized,
                        "El usuario no está autenticado");
                }

                if (string.IsNullOrWhiteSpace(dto.Content) ||
                    dto.Content.Trim().Length > 500)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "El comentario debe contener entre 1 y 500 caracteres");
                }

                if (dto.PostId <= 0 ||
                    (dto.ParentCommentId.HasValue &&
                     dto.ParentCommentId.Value <= 0))
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "Los identificadores proporcionados son inválidos");
                }

                bool isUserActive = await _userAccountStatusService.IsUserActiveAsync(dto.UserId);

                if (!isUserActive)
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "La cuenta del usuario no está activa");
                }

                var postResult = await _postService.GetVisiblePostByIdAsync(dto.PostId, dto.UserId);

                if (!postResult.IsSuccess)
                {
                    return ServiceResult.Failure(postResult.Status, postResult.Errors.ToArray());
                }

                if (postResult.Data == null)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "La publicación no está disponible");
                }

                if (!postResult.Data.AllowComments)
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "Los comentarios están desactivados para esta publicación");
                }

                if (dto.ParentCommentId.HasValue)
                {
                    Comment? parentComment = await _commentRepository.GetByIdAsync(dto.ParentCommentId.Value);

                    if (parentComment == null || parentComment.IsDeleted || parentComment.PostId != dto.PostId)
                    {
                        return ServiceResult.Failure(OperationStatus.NotFound,
                            "El comentario al que intenta responder no está disponible");
                    }
                }

                Comment comment = _mapper.Map<Comment>(dto);

                comment.Content = dto.Content.Trim();
                comment.CreatedAt = DateTime.UtcNow;
                comment.UpdatedAt = null;
                comment.IsDeleted = false;

                await _commentRepository.AddAsync(comment);

                return ServiceResult.Success();
            });
        }

        public async Task<ServiceResult> UpdateAsync(UpdateCommentDto dto, int commentId)
        {
            return await ExecuteAsync(async () =>
            {
                if (dto == null)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "Los datos del comentario son inválidos");
                }

                if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Trim().Length > 500)
                {
                    return ServiceResult.Failure(OperationStatus.ValidationError,
                        "El comentario debe contener entre 1 y 500 caracteres");
                }

                var commentResult = await GetMyCommentByIdAsync(commentId, dto.UserId);

                if (!commentResult.IsSuccess)
                {
                    return ServiceResult.Failure(commentResult.Status, commentResult.Errors.ToArray());
                }

                Comment? comment = await _commentRepository.GetByIdAsync(commentId);

                if (comment == null || comment.IsDeleted)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "El comentario no pudo ser encontrado");
                }

                if (comment.UserId != dto.UserId)
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No posee permisos para editar este comentario");
                }

                _mapper.Map(dto, comment);

                comment.Content = dto.Content.Trim();
                comment.UpdatedAt = DateTime.UtcNow;

                Comment? updatedComment = await _commentRepository.UpdateAsync(comment, commentId);

                return updatedComment != null ? ServiceResult.Success()
                    : ServiceResult.Failure(OperationStatus.NotFound,
                        "No se pudo actualizar el comentario");
            });
        }

        public async Task<ServiceResult> DeleteAsync(int commentId, string userId)
        {
            return await ExecuteAsync(async () =>
            {
                var commentResult = await GetMyCommentByIdAsync(commentId, userId);

                if (!commentResult.IsSuccess)
                {
                    return ServiceResult.Failure(commentResult.Status,
                        commentResult.Errors.ToArray());
                }

                Comment? comment = await _commentRepository.GetByIdAsync(commentId);

                if (comment == null || comment.IsDeleted)
                {
                    return ServiceResult.Failure(OperationStatus.NotFound,
                        "El comentario no pudo ser encontrado");
                }

                if (comment.UserId != userId)
                {
                    return ServiceResult.Failure(OperationStatus.Forbidden,
                        "No posee permisos para eliminar este comentario");
                }

                comment.IsDeleted = true;

                Comment? deletedComment = await _commentRepository.UpdateAsync(comment, commentId);

                return deletedComment != null ? ServiceResult.Success()
                    : ServiceResult.Failure(OperationStatus.NotFound,
                        "No se pudo eliminar el comentario");
            });
        }

        public async Task<ServiceResultWithData<List<CommentDto>>> GetCommentsByPostIdAsync(int postId, string userId)
        {
            var result = await GetCommentsByPostIdsAsync([postId], userId);

            if (!result.IsSuccess)
            {
                return ServiceResultWithData<List<CommentDto>>.Failure(result.Status, result.Errors.ToArray());
            }

            if (result.Data == null || !result.Data.TryGetValue(postId, out List<CommentDto>? comments))
            {
                return ServiceResultWithData<List<CommentDto>>.Failure(OperationStatus.Failed,
                    "No se pudieron obtener los comentarios");
            }

            return ServiceResultWithData<List<CommentDto>>.Success(comments);
        }

        public async Task<ServiceResultWithData<CommentDto>> GetMyCommentByIdAsync(int commentId, string userId)
        {
            return await ExecuteAsync<CommentDto>(async () =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ServiceResultWithData<CommentDto>.Failure(OperationStatus.Unauthorized,
                        "El usuario no está autenticado");
                }

                if (commentId <= 0)
                {
                    return ServiceResultWithData<CommentDto>.Failure(OperationStatus.ValidationError,
                        "El ID del comentario es inválido");
                }

                bool isUserActive = await _userAccountStatusService.IsUserActiveAsync(userId);

                if (!isUserActive)
                {
                    return ServiceResultWithData<CommentDto>.Failure(OperationStatus.Forbidden,
                        "La cuenta del usuario no está activa");
                }

                Comment? comment = await _commentRepository.GetByIdAsync(commentId);

                if (comment == null || comment.IsDeleted)
                {
                    return ServiceResultWithData<CommentDto>.Failure(OperationStatus.NotFound,
                        "El comentario no pudo ser encontrado");
                }

                if (comment.UserId != userId)
                {
                    return ServiceResultWithData<CommentDto>.Failure(OperationStatus.Forbidden,
                        "No posee permisos para editar este comentario");
                }

                var postResult = await _postService.GetVisiblePostByIdAsync(comment.PostId, userId);

                if (!postResult.IsSuccess)
                {
                    return ServiceResultWithData<CommentDto>.Failure(postResult.Status,
                        postResult.Errors.ToArray());
                }

                if (postResult.Data == null)
                {
                    return ServiceResultWithData<CommentDto>.Failure(OperationStatus.NotFound,
                        "La publicación no está disponible");
                }

                CommentDto dto = _mapper.Map<CommentDto>(comment);

                return ServiceResultWithData<CommentDto>.Success(dto);
            });
        }

        public async Task<ServiceResultWithData<Dictionary<int, List<CommentDto>>>> GetCommentsByPostIdsAsync(IEnumerable<int> postIds, string userId)
        {
            return await ExecuteAsync<Dictionary<int, List<CommentDto>>>(async () =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return ServiceResultWithData<Dictionary<int, List<CommentDto>>>.Failure(OperationStatus.Unauthorized,
                        "El usuario no está autenticado");
                }

                if (postIds == null)
                {
                    return ServiceResultWithData<Dictionary<int, List<CommentDto>>>.Failure(OperationStatus.ValidationError,
                        "Los identificadores de las publicaciones son inválidos");
                }

                int[] ids = postIds.Distinct().ToArray();

                if (ids.Any(id => id <= 0))
                {
                    return ServiceResultWithData<Dictionary<int, List<CommentDto>>>.Failure(OperationStatus.ValidationError,
                        "Los identificadores de las publicaciones son inválidos");
                }

                var visiblePostsResult = await _postService.GetVisiblePostIdsAsync(ids, userId);

                if (!visiblePostsResult.IsSuccess)
                {
                    return ServiceResultWithData<Dictionary<int, List<CommentDto>>>.Failure(visiblePostsResult.Status,
                        visiblePostsResult.Errors.ToArray());
                }

                if (visiblePostsResult.Data == null || !visiblePostsResult.Data.SetEquals(ids))
                {
                    return ServiceResultWithData<Dictionary<int, List<CommentDto>>>.Failure(OperationStatus.Forbidden,
                        "No posee permisos para visualizar una o más publicaciones");
                }

                Dictionary<int, List<CommentDto>> result = ids.ToDictionary(id => id,
                    id => new List<CommentDto>());

                List<Comment> comments = await _commentRepository.GetAllQuery()
                    .AsNoTracking()
                    .Where(c => ids.Contains(c.PostId))
                    .OrderBy(c => c.CreatedAt)
                    .ThenBy(c => c.Id)
                    .ToListAsync();

                foreach (var group in comments.GroupBy(c => c.PostId))
                {
                    Dictionary<int, CommentDto> commentDtos = group.ToDictionary(
                        c => c.Id,
                        c => _mapper.Map<CommentDto>(c));

                    foreach (CommentDto dto in commentDtos.Values)
                    {
                        dto.Replies = [];

                        if (dto.IsDeleted)
                        {
                            dto.Content = "Este comentario fue eliminado.";
                        }
                    }

                    foreach (Comment comment in group)
                    {
                        CommentDto dto = commentDtos[comment.Id];

                        if (comment.ParentCommentId.HasValue && commentDtos.TryGetValue(
                                comment.ParentCommentId.Value, out CommentDto? parent))
                        {
                            parent.Replies.Add(dto);
                        }
                        else
                        {
                            result[comment.PostId].Add(dto);
                        }
                    }
                }

                foreach (List<CommentDto> postComments in result.Values)
                {
                    postComments.RemoveAll(c => !ShouldShowComment(c));
                }

                return ServiceResultWithData<Dictionary<int, List<CommentDto>>>.Success(result);
            });
        }

        private static bool ShouldShowComment(CommentDto comment)
        {
            comment.Replies.RemoveAll(reply => !ShouldShowComment(reply));

            return !comment.IsDeleted || comment.Replies.Count > 0;
        }

    }
}
