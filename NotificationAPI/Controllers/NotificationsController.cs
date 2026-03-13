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
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
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

                // Call NotificationService to persist and send notification
                var notification = await _notificationService.SendNotificationAsync(
                    request.EntityType,
                    request.EntityId,
                    request.EntityDisplayName,
                    operation,
                    request.UserName
                );

                _logger.LogInformation("Notification created successfully: Id={Id}, EntityType={EntityType}, EntityId={EntityId}, Operation={Operation}", 
                    notification.Id, request.EntityType, request.EntityId, request.Operation);

                return CreatedAtAction(nameof(GetNotificationById), new { id = notification.Id }, notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification: EntityType={EntityType}, EntityId={EntityId}, Operation={Operation}", 
                    request?.EntityType, request?.EntityId, request?.Operation);
                return StatusCode(500, new { error = $"Failed to send notification: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] bool? isRead, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate pagination parameters
                if (page < 1)
                {
                    return BadRequest(new { error = "Page must be greater than 0" });
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new { error = "PageSize must be between 1 and 100" });
                }

                _logger.LogDebug("Getting notifications: isRead={IsRead}, page={Page}, pageSize={PageSize}", isRead, page, pageSize);

                var (notifications, pagination) = await _notificationService.GetNotificationsAsync(isRead, page, pageSize);

                var response = new PaginatedResponse<Notification>
                {
                    Data = notifications,
                    Pagination = pagination
                };

                _logger.LogInformation("Retrieved {Count} notifications (page {Page} of {TotalPages})", 
                    notifications.Count, page, pagination.TotalPages);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return StatusCode(500, new { error = $"Failed to retrieve notifications: {ex.Message}" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNotificationById(int id)
        {
            try
            {
                var notification = await _notificationService.GetNotificationByIdAsync(id);
                
                if (notification == null)
                {
                    return NotFound(new { error = $"Notification with ID {id} not found" });
                }

                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification {Id}", id);
                return StatusCode(500, new { error = $"Failed to retrieve notification: {ex.Message}" });
            }
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var notification = await _notificationService.MarkAsReadAsync(id);
                
                if (notification == null)
                {
                    return NotFound(new { error = $"Notification with ID {id} not found" });
                }

                _logger.LogInformation("Notification {Id} marked as read", id);
                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {Id} as read", id);
                return StatusCode(500, new { error = $"Failed to mark notification as read: {ex.Message}" });
            }
        }

        [HttpPut("{id}/unread")]
        public async Task<IActionResult> MarkAsUnread(int id)
        {
            try
            {
                var notification = await _notificationService.MarkAsUnreadAsync(id);
                
                if (notification == null)
                {
                    return NotFound(new { error = $"Notification with ID {id} not found" });
                }

                _logger.LogInformation("Notification {Id} marked as unread", id);
                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {Id} as unread", id);
                return StatusCode(500, new { error = $"Failed to mark notification as unread: {ex.Message}" });
            }
        }

        [HttpPut("mark-read")]
        public async Task<IActionResult> BulkMarkAsRead([FromBody] BulkMarkReadRequest request)
        {
            try
            {
                if (request == null || request.NotificationIds == null || request.NotificationIds.Count == 0)
                {
                    return BadRequest(new { error = "NotificationIds array is required and cannot be empty" });
                }

                var updatedCount = await _notificationService.BulkMarkAsReadAsync(request.NotificationIds);

                var response = new BulkMarkReadResponse
                {
                    UpdatedCount = updatedCount,
                    Message = $"Successfully marked {updatedCount} notification(s) as read"
                };

                _logger.LogInformation("Bulk marked {Count} notifications as read", updatedCount);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk marking notifications as read");
                return StatusCode(500, new { error = $"Failed to bulk mark notifications as read: {ex.Message}" });
            }
        }

        [HttpDelete("cleanup")]
        public async Task<IActionResult> CleanupOldNotifications([FromQuery] int olderThanDays = 90)
        {
            try
            {
                if (olderThanDays < 1)
                {
                    return BadRequest(new { error = "olderThanDays must be greater than 0" });
                }

                var deletedCount = await _notificationService.CleanupOldNotificationsAsync(olderThanDays);

                var response = new CleanupResponse
                {
                    DeletedCount = deletedCount,
                    Message = $"Successfully deleted {deletedCount} old notification(s)"
                };

                _logger.LogInformation("Cleaned up {Count} old notifications (older than {Days} days)", deletedCount, olderThanDays);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old notifications");
                return StatusCode(500, new { error = $"Failed to cleanup old notifications: {ex.Message}" });
            }
        }

        // Legacy endpoint for backward compatibility (kept but not used)
        [HttpGet("receive")]
        public IActionResult ReceiveNotification()
        {
            try
            {
                _logger.LogDebug("Attempting to receive notification (legacy endpoint)");

                var notification = _notificationService.ReceiveNotification();

                if (notification == null)
                {
                    _logger.LogDebug("No notifications available");
                    return NoContent();
                }

                _logger.LogInformation("Notification received: EntityType={EntityType}, EntityId={EntityId}, Operation={Operation}", 
                    notification.EntityType, notification.EntityId, notification.Operation);

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
