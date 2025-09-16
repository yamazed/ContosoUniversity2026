using System;
// System.Messaging is not available in .NET Core/8
// Using alternative implementation
using Microsoft.Extensions.Configuration;
using ContosoUniversity.Models;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace ContosoUniversity.Services
{
    public class NotificationService
    {
        private readonly string _queuePath;
        private readonly ConcurrentQueue<Message> _messageQueue = new ConcurrentQueue<Message>();

        public NotificationService(IConfiguration configuration = null)
        {
            // Get queue path from configuration or use default
            _queuePath = configuration?.GetValue<string>("NotificationQueuePath") ?? @".\Private$\ContosoUniversityNotifications";

            // In .NET 8, System.Messaging is not available
            // Using in-memory queue implementation instead
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
                var message = new Message
                {
                    Body = jsonMessage,
                    Label = $"{entityType} {operation}",
                    Priority = MessagePriority.Normal
                };

                _messageQueue.Enqueue(message);
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
                if (_messageQueue.TryDequeue(out var message))
                {
                    var jsonContent = message.Body.ToString();
                    return JsonConvert.DeserializeObject<Notification>(jsonContent);
                }
                return null; // No messages available
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
            // Nothing to dispose in this implementation
        }

        // Custom Message class to replace System.Messaging.Message
        private class Message
        {
            public string Body { get; set; }
            public string Label { get; set; }
            public MessagePriority Priority { get; set; } = MessagePriority.Normal;
        }

        // Custom enum to replace System.Messaging.MessagePriority
        private enum MessagePriority
        {
            Lowest = 0,
            VeryLow = 1,
            Low = 2,
            Normal = 3,
            AboveNormal = 4,
            High = 5,
            VeryHigh = 6,
            Highest = 7
        }
    }
}
