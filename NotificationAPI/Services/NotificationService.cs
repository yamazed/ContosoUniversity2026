using System;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationAPI.Models;
using NotificationAPI.Repositories;
using Newtonsoft.Json;

namespace NotificationAPI.Services
{
    public class NotificationService
    {
        private readonly AmazonSQSClient _sqsClient;
        private readonly string _queueUrl;
        private readonly string _queueName;
        private readonly INotificationRepository _repository;
        private readonly ILogger<NotificationService>? _logger;

        public NotificationService(
            IConfiguration configuration, 
            INotificationRepository repository,
            ILogger<NotificationService>? logger = null)
        {
            _logger = logger;
            _repository = repository;
            
            // Get queue name from configuration for logging/debugging (retained for compatibility)
            _queueName = configuration["NotificationQueuePath"] ?? "ContosoUniversityNotifications";

            // Read AWS configuration
            var region = configuration["AWS:Region"];
            _queueUrl = configuration["AWS:SQS:QueueUrl"] ?? throw new InvalidOperationException("AWS:SQS:QueueUrl configuration is missing");

            if (string.IsNullOrEmpty(region))
            {
                var errorMsg = "AWS:Region configuration is missing";
                _logger?.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            // Initialize SQS client with "default" profile
            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            
            // Load credentials from the "default" profile
            var credentialProfileStoreChain = new CredentialProfileStoreChain();
            if (credentialProfileStoreChain.TryGetAWSCredentials("default", out AWSCredentials credentials))
            {
                _sqsClient = new AmazonSQSClient(credentials, regionEndpoint);
                _logger?.LogInformation("NotificationService initialized with 'default' AWS profile. Queue: {QueueUrl}, Region: {Region}", _queueUrl, region);
            }
            else
            {
                // Fallback to default credential chain (environment variables, instance profile, etc.)
                _sqsClient = new AmazonSQSClient(regionEndpoint);
                _logger?.LogWarning("Could not load 'default' AWS profile, using default credential chain. Queue: {QueueUrl}, Region: {Region}", _queueUrl, region);
            }
        }

        // Enhanced async version that persists to database
        public async Task<Notification> SendNotificationAsync(string entityType, string entityId, string? entityDisplayName, EntityOperation operation, string? userName = null)
        {
            var notification = new Notification
            {
                EntityType = entityType,
                EntityId = entityId,
                Operation = operation.ToString(),
                Message = GenerateMessage(entityType, entityId, entityDisplayName, operation),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userName ?? "System",
                IsRead = false,
                ReadAt = null
            };

            try
            {
                // Primary operation: Save to database
                notification = await _repository.CreateAsync(notification);
                _logger?.LogInformation("Notification persisted to database. Id: {Id}, EntityType: {EntityType}, EntityId: {EntityId}", 
                    notification.Id, entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to persist notification to database. EntityType: {EntityType}, EntityId: {EntityId}", 
                    entityType, entityId);
                throw; // Fail fast if database operation fails
            }

            // Secondary operation: Send to SQS (best-effort)
            try
            {
                var messageBody = JsonConvert.SerializeObject(notification);
                var sendRequest = new SendMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MessageBody = messageBody
                };

                var response = await _sqsClient.SendMessageAsync(sendRequest);
                _logger?.LogInformation("Notification sent to SQS. MessageId: {MessageId}, NotificationId: {Id}", 
                    response.MessageId, notification.Id);
            }
            catch (Amazon.SQS.AmazonSQSException sqsEx)
            {
                _logger?.LogError(sqsEx, "AWS SQS error sending notification (database save succeeded). ErrorCode: {ErrorCode}, NotificationId: {Id}", 
                    sqsEx.ErrorCode, notification.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send notification to SQS (database save succeeded). NotificationId: {Id}", 
                    notification.Id);
            }

            return notification;
        }

        // Legacy sync version for backward compatibility
        public void SendNotification(string entityType, string entityId, EntityOperation operation, string? userName = null)
        {
            SendNotification(entityType, entityId, null, operation, userName);
        }

        public void SendNotification(string entityType, string entityId, string? entityDisplayName, EntityOperation operation, string? userName = null)
        {
            // Call async version synchronously for backward compatibility
            SendNotificationAsync(entityType, entityId, entityDisplayName, operation, userName).GetAwaiter().GetResult();
        }

        // New methods for database operations
        public async Task<(List<Notification>, PaginationMetadata)> GetNotificationsAsync(bool? isRead = null, int page = 1, int pageSize = 20)
        {
            var (notifications, totalCount) = await _repository.GetAllAsync(isRead, page, pageSize);
            
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var metadata = new PaginationMetadata
            {
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
            
            return (notifications, metadata);
        }

        public async Task<Notification?> GetNotificationByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Notification?> MarkAsReadAsync(int id)
        {
            var success = await _repository.MarkAsReadAsync(id);
            if (!success) return null;
            
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Notification?> MarkAsUnreadAsync(int id)
        {
            var success = await _repository.MarkAsUnreadAsync(id);
            if (!success) return null;
            
            return await _repository.GetByIdAsync(id);
        }

        public async Task<int> BulkMarkAsReadAsync(List<int> ids)
        {
            return await _repository.BulkMarkAsReadAsync(ids);
        }

        public async Task<int> CleanupOldNotificationsAsync(int olderThanDays)
        {
            return await _repository.DeleteOldNotificationsAsync(olderThanDays, onlyRead: true);
        }

        // Legacy SQS receive method (kept for backward compatibility but not used)
        public Notification? ReceiveNotification()
        {
            try
            {
                _logger?.LogDebug("Attempting to receive notification from SQS queue: {QueueUrl}", _queueUrl);

                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = 0
                };

                var receiveResponse = _sqsClient.ReceiveMessageAsync(receiveRequest).GetAwaiter().GetResult();

                if (receiveResponse?.Messages?.Count > 0)
                {
                    var message = receiveResponse.Messages[0];
                    var notification = JsonConvert.DeserializeObject<Notification>(message.Body);

                    if (notification == null)
                    {
                        _logger?.LogWarning("Failed to deserialize notification from message body. MessageId: {MessageId}", message.MessageId);
                        return null;
                    }

                    var deleteRequest = new DeleteMessageRequest
                    {
                        QueueUrl = _queueUrl,
                        ReceiptHandle = message.ReceiptHandle
                    };
                    _sqsClient.DeleteMessageAsync(deleteRequest).GetAwaiter().GetResult();

                    _logger?.LogInformation("Notification received and deleted from SQS. MessageId: {MessageId}", message.MessageId);
                    return notification;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to receive notification from SQS");
                return null;
            }
        }

        private string GenerateMessage(string entityType, string entityId, string? entityDisplayName, EntityOperation operation)
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
    }
}
