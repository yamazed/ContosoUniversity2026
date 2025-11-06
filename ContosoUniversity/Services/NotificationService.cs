using System;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ContosoUniversity.Models;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    public class NotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _queueName;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<NotificationService> logger = null)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            
            // Get queue name from configuration for logging/debugging (retained for compatibility)
            _queueName = configuration["NotificationQueuePath"] ?? "ContosoUniversityNotifications";

            // Read NotificationAPI BaseUrl from configuration with default
            _baseUrl = configuration["NotificationAPI:BaseUrl"] ?? "http://localhost:5001";
            
            _logger?.LogInformation("NotificationService initialized with NotificationAPI BaseUrl: {BaseUrl}", _baseUrl);
        }

        public void SendNotification(string entityType, string entityId, EntityOperation operation, string userName = null)
        {
            SendNotification(entityType, entityId, null, operation, userName);
        }

        public void SendNotification(string entityType, string entityId, string entityDisplayName, EntityOperation operation, string userName = null)
        {
            try
            {
                // Create SendNotificationRequest object
                var request = new
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    EntityDisplayName = entityDisplayName,
                    Operation = operation.ToString(),
                    UserName = userName
                };

                // Serialize to JSON
                var jsonContent = JsonConvert.SerializeObject(request);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger?.LogDebug("Sending notification to NotificationAPI: {EntityType} {EntityId} - {Operation}", entityType, entityId, operation);

                // POST to {BaseUrl}/api/notifications
                var response = _httpClient.PostAsync($"{_baseUrl}/api/notifications", httpContent).GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("Notification sent successfully to NotificationAPI. EntityType: {EntityType}, EntityId: {EntityId}, Operation: {Operation}", 
                        entityType, entityId, operation);
                }
                else
                {
                    _logger?.LogWarning("NotificationAPI returned non-success status code: {StatusCode}. EntityType: {EntityType}, EntityId: {EntityId}, Operation: {Operation}", 
                        response.StatusCode, entityType, entityId, operation);
                    System.Diagnostics.Debug.WriteLine($"NotificationAPI returned status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't break the main operation
                _logger?.LogError(ex, "Failed to send notification to NotificationAPI. EntityType: {EntityType}, EntityId: {EntityId}, Operation: {Operation}", 
                    entityType, entityId, operation);
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        public Notification ReceiveNotification()
        {
            try
            {
                _logger?.LogDebug("Attempting to receive notification from NotificationAPI: {BaseUrl}/api/notifications", _baseUrl);

                // GET from {BaseUrl}/api/notifications
                var response = _httpClient.GetAsync($"{_baseUrl}/api/notifications").GetAwaiter().GetResult();

                // Return notification on 200 status
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var jsonContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var notification = JsonConvert.DeserializeObject<Notification>(jsonContent);
                    
                    _logger?.LogInformation("Notification received from NotificationAPI. EntityType: {EntityType}, EntityId: {EntityId}", 
                        notification?.EntityType, notification?.EntityId);
                    
                    return notification;
                }
                // Return null on 204 No Content status
                else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger?.LogDebug("No notifications available from NotificationAPI");
                    return null;
                }
                else
                {
                    _logger?.LogWarning("NotificationAPI returned unexpected status code: {StatusCode}", response.StatusCode);
                    System.Diagnostics.Debug.WriteLine($"NotificationAPI returned status code: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to receive notification from NotificationAPI");
                System.Diagnostics.Debug.WriteLine($"Failed to receive notification: {ex.Message}");
                return null;
            }
        }

        public void MarkAsRead(int notificationId)
        {
            // In a real implementation, you might want to store notifications in database as well
            // for persistence and tracking read status
        }
    }
}
