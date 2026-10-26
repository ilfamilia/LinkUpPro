using LinkUpPro.Core.Domain.Entities;
using LinkUpPro.Core.Domain.Interfaces;
using LinkUpPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace LinkUpPro.Infrastructure.Persistence.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(LinkUpProAppContext context) : base(context)
        {
        }

        public async Task<int> MarkAllAsReadAsync(string recipientUserId)
        {
            List<Notification> unreadNotifications = await _dbSet
                .Where(n => n.RecipientUserId == recipientUserId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Count == 0)
            {
                return 0;
            }

            foreach (Notification notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _dbContext.SaveChangesAsync();
            return unreadNotifications.Count;
        }
    }
}
