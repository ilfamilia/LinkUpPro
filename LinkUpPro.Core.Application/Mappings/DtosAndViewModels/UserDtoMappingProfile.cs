using AutoMapper;
using LinkUpPro.Core.Application.Dtos.User;
using LinkUpPro.Core.Application.ViewModels.User;
namespace LinkUpPro.Core.Application.Mappings.DtosAndViewModels.User
{
    public class UserDtoMappingProfile : Profile
    {
        public UserDtoMappingProfile()
        {
            CreateMap<LoginViewModel, LoginDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.Password))
                .ForMember(dest => dest.RememberMe, opt => opt.MapFrom(src => src.RememberMe));

            CreateMap<RegisterUserViewModel, RegisterUserDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ProfileImage, opt => opt.Ignore());

            CreateMap<EmailConfirmationViewModel, EmailConfirmationDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName));

            CreateMap<ResetPasswordRequestViewModel, ResetPasswordRequestDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Token, opt => opt.MapFrom(src => src.Token))
                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.Password));

            CreateMap<EditUserViewModel, EditUserDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.ProfileImage, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentPassword, opt => opt.MapFrom(src => src.CurrentPassword))
                .ForMember(dest => dest.NewPassword, opt => opt.MapFrom(src => src.NewPassword));

            CreateMap<UserDto, EditUserViewModel>()
                .ForMember(dest => dest.ProfileImagePath,
                    opt => opt.MapFrom(src => src.ProfileImage))
                .ForMember(dest => dest.ProfileImageFile,
                    opt => opt.Ignore())
                .ForMember(dest => dest.CurrentPassword,
                    opt => opt.Ignore())
                .ForMember(dest => dest.NewPassword,
                    opt => opt.Ignore())
                .ForMember(dest => dest.ConfirmPassword,
                    opt => opt.Ignore());
        }
    }    
}
