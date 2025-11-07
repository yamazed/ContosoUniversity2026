using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.DTOs;
using ContosoUniversity.Services;

namespace ContosoUniversity.Controllers.Api
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsApiController : ControllerBase
    {
        private readonly SchoolContext _context;
        private readonly NotificationService _notificationService;

        public NotificationsApiController(SchoolContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: api/notifications
        [HttpGet]
        public ActionResult<ApiResponse<List<NotificationDto>>> GetNotifications()
        {
            try
            {
                var notifications = new List<NotificationDto>();

                // Read all available notifications from the queue using NotificationService
                Notification notification;
                while ((notification = _notificationService.ReceiveNotification()) != null)
                {
                    notifications.Add(new NotificationDto
                    {
                        Id = notification.Id,
                        EntityType = notification.EntityType,
                        EntityId = notification.EntityId,
                        Operation = notification.Operation,
                        Message = notification.Message,
                        CreatedAt = notification.CreatedAt,
                        CreatedBy = notification.CreatedBy,
                        IsRead = notification.IsRead,
                        ReadAt = notification.ReadAt
                    });

                    // Limit to prevent overwhelming the UI
                    if (notifications.Count >= 10)
                        break;
                }

                return Ok(ApiResponse<List<NotificationDto>>.SuccessResponse(notifications));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<List<NotificationDto>>.ErrorResponse(
                    "An error occurred while retrieving notifications.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // POST: api/notifications/5/mark-read
        [HttpPost("{id}/mark-read")]
        public ActionResult<ApiResponse<NotificationDto>> MarkNotificationAsRead(int id)
        {
            try
            {
                // Use NotificationService to mark as read
                _notificationService.MarkAsRead(id);

                return Ok(ApiResponse<NotificationDto>.SuccessResponse(
                    null, 
                    "Notification marked as read."
                ));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<NotificationDto>.ErrorResponse(
                    "An error occurred while marking the notification as read.",
                    new List<string> { ex.Message }
                ));
            }
        }
    }
}
