using LinkUpPro.Core.Application.Dtos.Battleship;
using LinkUpPro.Core.Application.Dtos.Friendship;
using LinkUpPro.Core.Application.Results;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IBattleshipGameService
    {
        Task<ServiceResultWithData<List<FriendDto>>> GetAvailableOpponentsAsync(string currentUserId, string? searchText = null);

        Task<ServiceResultWithData<int>> CreateAsync(string currentUserId, string opponentUserId);

        Task<ServiceResultWithData<BattleshipGameListDto>> GetMyGamesAsync(string currentUserId);

        Task<ServiceResultWithData<BattleshipGameDto>> GetGameAsync(int gameId, string currentUserId);

        Task<ServiceResult> ExpireOverdueGamesAsync(string currentUserId);

        Task<ServiceResult> SurrenderAsync(int gameId, string currentUserId);

        Task<ServiceResultWithData<BattleshipResultDto>> GetResultAsync(int gameId, string currentUserId);
    }
}
