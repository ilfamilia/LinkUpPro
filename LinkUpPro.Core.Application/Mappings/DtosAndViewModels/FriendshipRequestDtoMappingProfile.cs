using AutoMapper;
using LinkUpPro.Core.Application.Dtos.FriendshipRequest;
using LinkUpPro.Core.Application.ViewModels.FriendshipRequest;

namespace LinkUpPro.Core.Application.Mappings.DtosAndViewModels
{
    public class FriendshipRequestDtoMappingProfile : Profile
    {
        public FriendshipRequestDtoMappingProfile()
        {
            CreateMap<FriendshipUserDto, FriendshipUserViewModel>();
            CreateMap<FriendshipRequestListDto, FriendshipRequestListViewModel>();
            CreateMap<AvailableFriendshipUserDto, AvailableFriendshipUserViewModel>();
        }
    }
}
