using Microsoft.EntityFrameworkCore;
using NotificationAPI.Data;
using NotificationAPI.Models;

namespace NotificationAPI.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly NotificationContext _context;
        private readonly ILogger<NotificationRepository> _logger;
        
        public NotificationRepository(
            NotificationContext context, 
            ILogger<NotificationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<Notification> CreateAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Notification created with ID: {Id}", notification.Id);
            return notification;
        }
        
        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _context.Notifications.FindAsync(id);
        }
        
        public async Task<(List<Notification> notifications, int totalCount)> GetAllAsync(
            bool? isRead = null, 
            int page = 1, 
            int pageSize = 20)
        {
            var query = _context.Notifications.AsQueryable();
            
            // Apply read status filter if provided
            if (isRead.HasValue)
            {
                query = query.Where(n => n.IsRead == isRead.Value);
            }
            
            // Get total count before pagination
            var totalCount = await query.CountAsync();
            
            // Apply pagination and ordering
            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return (notifications, totalCount);
        }
        
        public async Task<Notification?> UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Notification updated with ID: {Id}", notification.Id);
            return notification;
        }
        
        public async Task<bool> MarkAsReadAsync(int id)
        {
            var notification = await GetByIdAsync(id);
            if (notification == null) return false;
            
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Notification marked as read: {Id}", id);
            return true;
        }
        
        public async Task<bool> MarkAsUnreadAsync(int id)
        {
            var notification = await GetByIdAsync(id);
            if (notification == null) return false;
            
            notification.IsRead = false;
            notification.ReadAt = null;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Notification marked as unread: {Id}", id);
            return true;
        }
        
        public async Task<int> BulkMarkAsReadAsync(List<int> ids)
        {
            var notifications = await _context.Notifications
                .Where(n => ids.Contains(n.Id))
                .ToListAsync();
            
            var now = DateTime.UtcNow;
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("Bulk marked {Count} notifications as read", notifications.Count);
            return notifications.Count;
        }
        
        public async Task<int> DeleteOldNotificationsAsync(int olderThanDays, bool onlyRead = true)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
            
            var query = _context.Notifications
                .Where(n => n.CreatedAt < cutoffDate);
            
            if (onlyRead)
            {
                query = query.Where(n => n.IsRead);
            }
            
            var notifications = await query.ToListAsync();
            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted {Count} old notifications", notifications.Count);
            return notifications.Count;
        }
    }
}
