using System;
using System.Configuration;
using System.Messaging;
using ContosoUniversity.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace System.Messaging
{
    public enum MessageQueueAccessRights
    {
        FullControl = 0
    }

    public enum MessageQueueErrorCode
    {
        IOTimeout = -2147024891
    }

    public class MessageQueueException : Exception
    {
        public MessageQueueErrorCode MessageQueueErrorCode { get; }

        public MessageQueueException(string message, MessageQueueErrorCode code)
            : base(message) => MessageQueueErrorCode = code;
    }

    public class Message
    {
        public string Body { get; set; }
        public string Label { get; set; }
        public MessagePriority Priority { get; set; }

        public Message(string body) => Body = body;
    }

    public enum MessagePriority
    {
        Normal = 0
    }

    public class MessageQueue : IDisposable
    {
        public object Formatter { get; set; }

        public static bool Exists(string path) => true;

        public static MessageQueue Create(string path) => new MessageQueue();

        public void SetPermissions(string groupOrUserName, MessageQueueAccessRights rights) { }

        public void Send(Message message) { }

        public Message Receive(TimeSpan timeout) => null;

        public void Dispose() { }
    }
}

namespace ContosoUniversity.Services
{
    public class NotificationService
    {
        private readonly string _queuePath;
        private readonly MessageQueue _queue;
        private readonly IConfiguration _configuration;

        public NotificationService(IConfiguration configuration)
        {
            _configuration = configuration;
            // Get queue path from configuration or use default
            _queuePath = _configuration["NotificationQueuePath"] ?? @".\Private$\ContosoUniversityNotifications";

            // Ensure the queue exists
            if (!MessageQueue.Exists(_queuePath))
            {
                _queue = MessageQueue.Create(_queuePath);
                _queue.SetPermissions("Everyone", MessageQueueAccessRights.FullControl);
            }
            else
            {
                _queue = new MessageQueue();
            }

            // Configure queue formatter
            // _queue.Formatter = new XmlMessageFormatter(new Type[] { typeof(string) });
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
                var message = new Message(jsonMessage)
                {
                    Label = $"{entityType} {operation}",
                    Priority = MessagePriority.Normal
                };

                _queue.Send(message);
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
                var message = _queue.Receive(TimeSpan.FromSeconds(1));
                var jsonContent = message.Body.ToString();
                return JsonConvert.DeserializeObject<Notification>(jsonContent);
            }
            catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
            {
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
            _queue?.Dispose();
        }
    }
}
