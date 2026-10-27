using LinkUpPro.Core.Application.Dtos.Battleship;
using LinkUpPro.Core.Application.Results;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IBattleshipAttackService
    {
        Task<ServiceResultWithData<BattleshipAttackBoardDto>> GetAttackBoardAsync(int gameId, string userId);

        Task<ServiceResultWithData<BattleshipOwnBoardDto>> GetOwnBoardAsync(int gameId, string userId);

        Task<ServiceResult> AttackAsync(int gameId, string attackerUserId, int row, int column);
    }
}
