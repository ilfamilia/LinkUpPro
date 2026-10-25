using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Social;
using LinkUpPro.Core.Application.Dtos.Battleship;
using LinkUpPro.Core.Domain.Entities;

namespace LinkUpPro.Core.Application.Mappings;

/// <summary>Maps stored data to read DTOs; services separately supply authorized public profiles.</summary>
public sealed class SocialMappingProfile : Profile
{
    public SocialMappingProfile()
    {
        CreateMap<FriendshipRequest, FriendshipRequestDto>(MemberList.None);
        CreateMap<Notification, NotificationDto>(MemberList.None);
        CreateMap<Post, FriendPostDto>(MemberList.None);
        CreateMap<BattleshipGame, GameSummaryDto>(MemberList.None);
        CreateMap<BattleshipGame, GameDetailsDto>(MemberList.None);
        CreateMap<Attack, AttackResultDto>();
    }
}
