using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Friendship;
using LinkUpPro.Core.Application.Dtos.Post;
using LinkUpPro.Core.Application.ViewModels.Friendship;

namespace LinkUpPro.Core.Application.Mappings.DtosAndViewModels
{
    public class FriendshipDtoMappingProfile : Profile
    {
        public FriendshipDtoMappingProfile()
        {
            CreateMap<FriendDto, FriendViewModel>();
            CreateMap<FriendshipSummaryDto, FriendshipSummaryViewModel>();
            CreateMap<FriendshipPostFilterViewModel, PostFilterDto>();
        }
    }
}
