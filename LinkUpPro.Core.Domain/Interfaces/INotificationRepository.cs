using LinkUpPro.Core.Domain.Entities;

namespace LinkUpPro.Core.Domain.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<int> MarkAllAsReadAsync(string recipientUserId);
    }
}
