using System;
using System.Collections.Concurrent;
using System.Configuration;
using ContosoUniversity.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    public class NotificationService
    {
        private readonly string _queuePath;
        private readonly ConcurrentQueue<QueueMessage> _queue;
        private readonly IConfiguration _configuration;

        private class QueueMessage
        {
            public string Body { get; set; }
            public string Label { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public NotificationService(IConfiguration configuration)
        {
            _configuration = configuration;
            // Get queue path from configuration or use default
            _queuePath = _configuration["NotificationQueuePath"] ?? @".\Private$\ContosoUniversityNotifications";

            // Initialize in-memory queue
            _queue = new ConcurrentQueue<QueueMessage>();
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
                var message = new QueueMessage
                {
                    Body = jsonMessage,
                    Label = $"{entityType} {operation}",
                    Timestamp = DateTime.Now
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
                if (_queue.TryDequeue(out var message))
                {
                    var jsonContent = message.Body;
                    return JsonConvert.DeserializeObject<Notification>(jsonContent);
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
            // No resources to dispose for in-memory queue
        }
    }
}
