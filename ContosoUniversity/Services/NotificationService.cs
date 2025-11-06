using System;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ContosoUniversity.Models;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    public class NotificationService
    {
        private readonly AmazonSQSClient _sqsClient;
        private readonly string _queueUrl;
        private readonly string _queueName;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IConfiguration configuration, ILogger<NotificationService> logger = null)
        {
            _logger = logger;
            
            // Get queue name from configuration for logging/debugging (retained for compatibility)
            _queueName = configuration["NotificationQueuePath"] ?? "ContosoUniversityNotifications";

            // Read AWS configuration
            var region = configuration["AWS:Region"];
            _queueUrl = configuration["AWS:SQS:QueueUrl"];

            if (string.IsNullOrEmpty(region))
            {
                var errorMsg = "AWS:Region configuration is missing";
                _logger?.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            if (string.IsNullOrEmpty(_queueUrl))
            {
                var errorMsg = "AWS:SQS:QueueUrl configuration is missing";
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

                // Serialize notification to JSON
                var messageBody = JsonConvert.SerializeObject(notification);

                _logger?.LogDebug("Sending notification to SQS: {EntityType} {EntityId} - {Operation}", entityType, entityId, operation);

                // Create SendMessageRequest
                var sendRequest = new SendMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MessageBody = messageBody
                };

                // Send message to SQS
                var response = _sqsClient.SendMessageAsync(sendRequest).GetAwaiter().GetResult();
                
                _logger?.LogInformation("Notification sent successfully to SQS. MessageId: {MessageId}, EntityType: {EntityType}, EntityId: {EntityId}, Operation: {Operation}", 
                    response.MessageId, entityType, entityId, operation);
            }
            catch (Amazon.SQS.AmazonSQSException sqsEx)
            {
                // Log SQS-specific errors with more detail
                _logger?.LogError(sqsEx, "AWS SQS error sending notification. ErrorCode: {ErrorCode}, StatusCode: {StatusCode}, EntityType: {EntityType}, EntityId: {EntityId}, Operation: {Operation}", 
                    sqsEx.ErrorCode, sqsEx.StatusCode, entityType, entityId, operation);
                System.Diagnostics.Debug.WriteLine($"AWS SQS error sending notification: {sqsEx.ErrorCode} - {sqsEx.Message}");
            }
            catch (Exception ex)
            {
                // Log error but don't break the main operation
                _logger?.LogError(ex, "Failed to send notification. EntityType: {EntityType}, EntityId: {EntityId}, Operation: {Operation}", 
                    entityType, entityId, operation);
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        public Notification ReceiveNotification()
        {
            try
            {
                _logger?.LogDebug("Attempting to receive notification from SQS queue: {QueueUrl}", _queueUrl);

                // Create ReceiveMessageRequest
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = 0
                };

                // Receive message from SQS
                var receiveResponse = _sqsClient.ReceiveMessageAsync(receiveRequest).GetAwaiter().GetResult();

                // Check if any messages were received
                if (receiveResponse?.Messages?.Count > 0)
                {
                    var message = receiveResponse.Messages[0];
                    _logger?.LogDebug("Received message from SQS. MessageId: {MessageId}, Body: {Body}", message.MessageId, message.Body);

                    // Deserialize JSON message body to Notification object
                    var notification = JsonConvert.DeserializeObject<Notification>(message.Body);

                    if (notification == null)
                    {
                        _logger?.LogWarning("Failed to deserialize notification from message body. MessageId: {MessageId}, Body: {Body}", message.MessageId, message.Body);
                        return null;
                    }
                    
                    _logger?.LogDebug("Deserialized notification: EntityType={EntityType}, EntityId={EntityId}, Operation={Operation}, Message={Message}, CreatedBy={CreatedBy}, CreatedAt={CreatedAt}", 
                        notification.EntityType, notification.EntityId, notification.Operation, notification.Message, notification.CreatedBy, notification.CreatedAt);

                    // Delete message from queue after successful retrieval
                    var deleteRequest = new DeleteMessageRequest
                    {
                        QueueUrl = _queueUrl,
                        ReceiptHandle = message.ReceiptHandle
                    };
                    _sqsClient.DeleteMessageAsync(deleteRequest).GetAwaiter().GetResult();

                    _logger?.LogInformation("Notification received and deleted from SQS. MessageId: {MessageId}, EntityType: {EntityType}, EntityId: {EntityId}", 
                        message.MessageId, notification.EntityType, notification.EntityId);

                    return notification;
                }

                // Return null when no messages available
                _logger?.LogDebug("No messages available in SQS queue");
                return null;
            }
            catch (Amazon.SQS.AmazonSQSException sqsEx)
            {
                _logger?.LogError(sqsEx, "AWS SQS error receiving notification. ErrorCode: {ErrorCode}, StatusCode: {StatusCode}", 
                    sqsEx.ErrorCode, sqsEx.StatusCode);
                System.Diagnostics.Debug.WriteLine($"AWS SQS error receiving notification: {sqsEx.ErrorCode} - {sqsEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to receive notification from SQS");
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

    }
}
