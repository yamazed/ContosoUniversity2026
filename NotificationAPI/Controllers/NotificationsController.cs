using Microsoft.AspNetCore.Mvc;
using NotificationAPI.Models;
using NotificationAPI.Services;

namespace NotificationAPI.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(NotificationService notificationService, ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpPost]
        public IActionResult SendNotification([FromBody] SendNotificationRequest request)
        {
            try
            {
                // Validate request body
                if (request == null)
                {
                    _logger.LogWarning("SendNotification called with null request body");
                    return BadRequest(new { error = "Request body is required" });
                }

                if (string.IsNullOrWhiteSpace(request.EntityType))
                {
                    _logger.LogWarning("SendNotification called with missing EntityType");
                    return BadRequest(new { error = "EntityType is required" });
                }

                if (string.IsNullOrWhiteSpace(request.EntityId))
                {
                    _logger.LogWarning("SendNotification called with missing EntityId");
                    return BadRequest(new { error = "EntityId is required" });
                }

                if (string.IsNullOrWhiteSpace(request.Operation))
                {
                    _logger.LogWarning("SendNotification called with missing Operation");
                    return BadRequest(new { error = "Operation is required" });
                }

                // Parse operation string to EntityOperation enum
                if (!Enum.TryParse<EntityOperation>(request.Operation, true, out var operation))
                {
                    _logger.LogWarning("SendNotification called with invalid Operation: {Operation}", request.Operation);
                    return BadRequest(new { error = $"Invalid operation: {request.Operation}. Valid values are: CREATE, UPDATE, DELETE" });
                }

                _logger.LogInformation("Sending notification: EntityType={EntityType}, EntityId={EntityId}, Operation={Operation}", 
                    request.EntityType, request.EntityId, request.Operation);

                // Call NotificationService to send the notification to AWS SQS
                _notificationService.SendNotification(
                    request.EntityType,
                    request.EntityId,
                    request.EntityDisplayName,
                    operation,
                    request.UserName
                );

                _logger.LogInformation("Notification sent successfully: EntityType={EntityType}, EntityId={EntityId}, Operation={Operation}", 
                    request.EntityType, request.EntityId, request.Operation);

                return Ok(new { message = "Notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification: EntityType={EntityType}, EntityId={EntityId}, Operation={Operation}", 
                    request?.EntityType, request?.EntityId, request?.Operation);
                return StatusCode(500, new { error = $"Failed to send notification: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult ReceiveNotification()
        {
            try
            {
                _logger.LogDebug("Attempting to receive notification");

                // Call NotificationService to receive a notification from AWS SQS
                var notification = _notificationService.ReceiveNotification();

                // Return 204 No Content when no notifications are available
                if (notification == null)
                {
                    _logger.LogDebug("No notifications available");
                    return NoContent();
                }

                _logger.LogInformation("Notification received: EntityType={EntityType}, EntityId={EntityId}, Operation={Operation}", 
                    notification.EntityType, notification.EntityId, notification.Operation);

                // Return 200 OK with the Notification object as JSON
                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving notification");
                return StatusCode(500, new { error = $"Failed to receive notification: {ex.Message}" });
            }
        }
    }
}
