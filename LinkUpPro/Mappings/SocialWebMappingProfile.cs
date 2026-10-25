using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Social;
using LinkUpPro.Core.Application.Dtos.Battleship;
using LinkUpPro.ViewModels.Social;
using LinkUpPro.ViewModels.Battleship;

namespace LinkUpPro.Mappings;

/// <summary>Maps service DTOs into view-only models and explicit validated filter inputs.</summary>
public sealed class SocialWebMappingProfile : Profile
{
    public SocialWebMappingProfile()
    {
        CreateMap<UserSummaryDto, PublicUserViewModel>();
        CreateMap<FriendDto, FriendItemViewModel>();
        CreateMap<FriendshipRequestDto, RequestItemViewModel>();
        CreateMap<FriendPostFilterViewModel, FriendPostFilterDto>();
        CreateMap<NotificationDto, NotificationItemViewModel>();
        CreateMap<GameSummaryDto, GameSummaryViewModel>();
        CreateMap<ShipCellDto, ShipCellViewModel>();
        CreateMap<AttackResultDto, AttackCellViewModel>();
        CreateMap<GameDetailsDto, GameDetailsViewModel>(MemberList.None);
    }
}
