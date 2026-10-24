using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Infrastructure.Identity.Services
{

    public class UserAccountStatusService : IUserAccountStatusService
    {
        private readonly UserManager<AppUser> _userManager;

        public UserAccountStatusService(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> IsUserActiveAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            AppUser? user = await _userManager.FindByIdAsync(userId);

            return user != null && user.EmailConfirmed;
        }

        public async Task<HashSet<string>> GetActiveUserIdsAsync(IEnumerable<string> userIds)
        {
            string[] ids = userIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
            {
                return [];
            }

            List<string> activeUserIds = await _userManager.Users
                .AsNoTracking()
                .Where(user => user.EmailConfirmed && ids.Contains(user.Id))
                .Select(user => user.Id)
                .ToListAsync();

            return activeUserIds.ToHashSet();
        }
    }
}