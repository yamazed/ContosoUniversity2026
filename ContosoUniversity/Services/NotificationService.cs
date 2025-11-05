using System;
using System.Collections.Concurrent;
using System.Configuration;
using ContosoUniversity.Models;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    public class NotificationService
    {
        private readonly ConcurrentQueue<NotificationMessage> _queue;
        private readonly string _queuePath;

        public NotificationService()
        {
            // Get queue path from configuration or use default (for compatibility)
            _queuePath = System.Configuration.ConfigurationManager.AppSettings["NotificationQueuePath"] ?? @".\Private$\ContosoUniversityNotifications";

            // Initialize in-memory queue
            _queue = new ConcurrentQueue<NotificationMessage>();
        }

        public void SendNotification(string entityType, string entityId, EntityOperation operation, string userName = null)
        {
            SendNotification(entityType, entityId, null, operation, userName);
        }

        public void SendNotification(string entityType, string entityId, string entityDisplayName, EntityOperation operation, string userName = null)
        {
            try
            {
                var notification = new Notification
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    Operation = operation.ToString(),
                    Message = GenerateMessage(entityType, entityId, entityDisplayName, operation),
                    CreatedAt = DateTime.Now,
                    CreatedBy = userName ?? "System",
                    IsRead = false
                };

                var jsonMessage = JsonConvert.SerializeObject(notification);
                var message = new NotificationMessage
                {
                    Body = jsonMessage,
                    Label = $"{entityType} {operation}",
                    CreatedAt = DateTime.Now
                };

                _queue.Enqueue(message);
            }
            catch (Exception ex)
            {
                // Log error but don't break the main operation
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        public Notification ReceiveNotification()
        {
            try
            {
                if (_queue.TryDequeue(out NotificationMessage message))
                {
                    return JsonConvert.DeserializeObject<Notification>(message.Body);
                }

                // No messages available
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to receive notification: {ex.Message}");
                return null;
            }
        }

        public void MarkAsRead(int notificationId)
        {
            // In a real implementation, you might want to store notifications in database as well
            // for persistence and tracking read status
        }

        private string GenerateMessage(string entityType, string entityId, string entityDisplayName, EntityOperation operation)
        {
            var displayText = !string.IsNullOrWhiteSpace(entityDisplayName)
                ? $"{entityType} '{entityDisplayName}'"
                : $"{entityType} (ID: {entityId})";

            switch (operation)
            {
                case EntityOperation.CREATE:
                    return $"New {displayText} has been created";
                case EntityOperation.UPDATE:
                    return $"{displayText} has been updated";
                case EntityOperation.DELETE:
                    return $"{displayText} has been deleted";
                default:
                    return $"{displayText} operation: {operation}";
            }
        }

        public void Dispose()
        {
            // Clear the queue
            while (_queue.TryDequeue(out _))
            {
                // Empty the queue
            }
        }

        private class NotificationMessage
        {
            public string Body { get; set; }
            public string Label { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
