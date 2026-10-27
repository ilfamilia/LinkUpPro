using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Battleship;
using LinkUpPro.Core.Application.ViewModels.Battleship;

namespace LinkUpPro.Core.Application.Mappings.DtosAndViewModels
{
    public class BattleshipAttackDtoMappingProfile : Profile
    {
        public BattleshipAttackDtoMappingProfile()
        {
            CreateMap<BattleshipAttackCellDto, BattleshipAttackCellViewModel>();
            CreateMap<BattleshipAttackBoardDto, BattleshipAttackBoardViewModel>();
            CreateMap<BattleshipOwnBoardDto, BattleshipOwnBoardViewModel>();
        }
    }
}
