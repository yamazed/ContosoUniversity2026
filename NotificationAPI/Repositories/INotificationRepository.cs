using NotificationAPI.Models;

namespace NotificationAPI.Repositories
{
    public interface INotificationRepository
    {
        Task<Notification> CreateAsync(Notification notification);
        Task<Notification?> GetByIdAsync(int id);
        Task<(List<Notification> notifications, int totalCount)> GetAllAsync(
            bool? isRead = null, 
            int page = 1, 
            int pageSize = 20);
        Task<Notification?> UpdateAsync(Notification notification);
        Task<bool> MarkAsReadAsync(int id);
        Task<bool> MarkAsUnreadAsync(int id);
        Task<int> BulkMarkAsReadAsync(List<int> ids);
        Task<int> DeleteOldNotificationsAsync(int olderThanDays, bool onlyRead = true);
    }
}
