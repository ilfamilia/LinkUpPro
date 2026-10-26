using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Battleship;
using LinkUpPro.Core.Application.Dtos.Friendship;
using LinkUpPro.Core.Application.ViewModels.Battleship;

namespace LinkUpPro.Core.Application.Mappings.DtosAndViewModels
{
    public class BattleshipGameDtoMappingProfile : Profile
    {
        public BattleshipGameDtoMappingProfile()
        {
            CreateMap<BattleshipGameDto, BattleshipGameViewModel>();
            CreateMap<BattleshipGameListDto, BattleshipIndexViewModel>();
            CreateMap<BattleshipResultDto, BattleshipResultViewModel>();
            CreateMap<FriendDto, BattleshipOpponentViewModel>();
        }
    }
}
