using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Comment;
using LinkUpPro.Core.Application.ViewModels.Comment;

namespace LinkUpPro.Core.Application.Mappings.DtosAndViewModels
{
    public class CommentDtoMappingProfile : Profile
    {
        public CommentDtoMappingProfile()
        {
            CreateMap<CreateCommentViewModel, CreateCommentDto>()
                .ForMember(dest => dest.UserId,
                    opt => opt.Ignore());

            CreateMap<UpdateCommentViewModel, UpdateCommentDto>()
                .ForMember(dest => dest.UserId,
                    opt => opt.Ignore());

            CreateMap<CommentDto, CommentViewModel>()
                .ForMember(dest => dest.UserName,
                    opt => opt.Ignore())
                .ForMember(dest => dest.UserProfilePicturePath,
                    opt => opt.Ignore());

            CreateMap<CommentDto, UpdateCommentViewModel>();
        }
    }
}
