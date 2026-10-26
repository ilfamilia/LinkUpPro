using LinkUpPro.Core.Application.Dtos.Battleship;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Core.Domain.Enums;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IBattleshipPlacementService
    {
        Task<ServiceResultWithData<BattleshipPlacementDto>> GetPlacementAsync(int gameId, string userId);

        Task<ServiceResult> PlaceShipAsync(int gameId, string userId, int shipNumber, int startRow, int startColumn, BattleshipShipDirection direction);
    }
}
