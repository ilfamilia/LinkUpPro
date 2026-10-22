using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Post;
using LinkUpPro.Core.Application.ViewModels.Post;

namespace LinkUpPro.Core.Application.Mappings.DtosAndViewModels
{
    public class PostDtoMappingProfile : Profile
    {
        public PostDtoMappingProfile()
        {
            CreateMap<CreatePostViewModel, CreatePostDto>()
                .ForMember(dest => dest.UserId,
                    opt => opt.Ignore())
                .ForMember(dest => dest.ImagePath,
                    opt => opt.Ignore());

            CreateMap<PostDto, PostViewModel>()
                .ForMember(dest => dest.UserName,
                    opt => opt.Ignore())
                .ForMember(dest => dest.UserProfilePicturePath,
                    opt => opt.Ignore());

            CreateMap<PostDto, UpdatePostViewModel>()
                .ForMember(dest => dest.CurrentImagePath,
                    opt => opt.MapFrom(src => src.ImagePath))
                .ForMember(dest => dest.ImageFile,
                    opt => opt.Ignore());

            CreateMap<UpdatePostViewModel, UpdatePostDto>()
                .ForMember(dest => dest.UserId,
                    opt => opt.Ignore())
                .ForMember(dest => dest.ImagePath,
                    opt => opt.Ignore());

            CreateMap<PostFilterViewModel, PostFilterDto>();
        }
    }
}
