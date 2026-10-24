namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IUserAccountStatusService
    {
        Task<bool> IsUserActiveAsync(string userId);
        Task<HashSet<string>> GetActiveUserIdsAsync(IEnumerable<string> userIds);
    }
}
