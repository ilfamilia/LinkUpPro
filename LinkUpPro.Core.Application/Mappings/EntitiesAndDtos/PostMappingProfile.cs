using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Post;
using LinkUpPro.Core.Domain.Entities;

namespace LinkUpPro.Core.Application.Mappings.EntitiesAndDtos
{
    public class PostMappingProfile : Profile
    {
        public PostMappingProfile()
        {
            CreateMap<Post, PostDto>()
                .ForMember(dest => dest.LikesCount,
                    opt => opt.Ignore())
                .ForMember(dest => dest.DislikesCount,
                    opt => opt.Ignore())
                .ForMember(dest => dest.CommentsCount,
                    opt => opt.Ignore())
                .ForMember(dest => dest.CurrentUserReaction,
                    opt => opt.Ignore())
                .ForMember(dest => dest.YouTubeEmbedUrl,
                    opt => opt.Ignore());

            CreateMap<CreatePostDto, Post>()
                .ForMember(dest => dest.Id,
                    opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt,
                    opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt,
                    opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted,
                    opt => opt.Ignore())
                .ForMember(dest => dest.ImagePath,
                    opt => opt.Ignore())
                .ForMember(dest => dest.Comments,
                    opt => opt.Ignore())
                .ForMember(dest => dest.Reactions,
                    opt => opt.Ignore())
                .ForMember(dest => dest.Notifications,
                    opt => opt.Ignore());

            CreateMap<UpdatePostDto, Post>()
                .ForMember(dest => dest.Id,
                    opt => opt.Ignore())
                .ForMember(dest => dest.UserId,
                    opt => opt.Ignore())
                .ForMember(dest => dest.ImagePath,
                    opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt,
                    opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt,
                    opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted,
                    opt => opt.Ignore())
                .ForMember(dest => dest.Comments,
                    opt => opt.Ignore())
                .ForMember(dest => dest.Reactions,
                    opt => opt.Ignore())
                .ForMember(dest => dest.Notifications,
                    opt => opt.Ignore());
        }
    }
}
