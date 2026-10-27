using AutoMapper;
using LinkUpPro.Core.Application.Dtos.Battleship;
using LinkUpPro.Core.Application.ViewModels.Battleship;

namespace LinkUpPro.Core.Application.Mappings.DtosAndViewModels
{
    public class BattleshipPlacementDtoMappingProfile : Profile
    {
        public BattleshipPlacementDtoMappingProfile()
        {
            CreateMap<BattleshipPlacementDto, BattleshipPlacementViewModel>();
            CreateMap<BattleshipShipOptionDto, BattleshipShipOptionViewModel>();
            CreateMap<BattleshipCellDto, BattleshipCellViewModel>();
        }
    }
}
