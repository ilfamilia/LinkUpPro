using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Comment;
using LinkUpPro.Core.Domain.Entities;

namespace LinkUpPro.Core.Application.Mappings.EntitiesAndDtos
{
    public class CommentMappingProfile : Profile
    {
        public CommentMappingProfile()
        {
            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.Replies,
                    opt => opt.Ignore());

            CreateMap<CreateCommentDto, Comment>()
                .ForMember(dest => dest.Id,
                    opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt,
                    opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt,
                    opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted,
                    opt => opt.Ignore())
                .ForMember(dest => dest.Post,
                    opt => opt.Ignore())
                .ForMember(dest => dest.ParentComment,
                    opt => opt.Ignore())
                .ForMember(dest => dest.Replies,
                    opt => opt.Ignore());

            CreateMap<UpdateCommentDto, Comment>()
                .ForMember(dest => dest.Id,
                    opt => opt.Ignore())
                .ForMember(dest => dest.UserId,
                    opt => opt.Ignore())
                .ForMember(dest => dest.PostId,
                    opt => opt.Ignore())
                .ForMember(dest => dest.ParentCommentId,
                    opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt,
                    opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt,
                    opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted,
                    opt => opt.Ignore())
                .ForMember(dest => dest.Post,
                    opt => opt.Ignore())
                .ForMember(dest => dest.ParentComment,
                    opt => opt.Ignore())
                .ForMember(dest => dest.Replies,
                    opt => opt.Ignore());
        }
    }
}