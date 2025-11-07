using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.DTOs;

namespace ContosoUniversity.Controllers.Api
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsApiController : ControllerBase
    {
        private readonly SchoolContext _context;

        public NotificationsApiController(SchoolContext context)
        {
            _context = context;
        }

        // GET: api/notifications
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetNotifications()
        {
            try
            {
                var notifications = await _context.Notifications
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        EntityType = n.EntityType,
                        EntityId = n.EntityId,
                        Operation = n.Operation,
                        Message = n.Message,
                        CreatedAt = n.CreatedAt,
                        CreatedBy = n.CreatedBy,
                        IsRead = n.IsRead,
                        ReadAt = n.ReadAt
                    })
                    .ToListAsync();

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

        // GET: api/notifications/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<NotificationDto>>> GetNotification(int id)
        {
            try
            {
                var notification = await _context.Notifications
                    .Where(n => n.Id == id)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        EntityType = n.EntityType,
                        EntityId = n.EntityId,
                        Operation = n.Operation,
                        Message = n.Message,
                        CreatedAt = n.CreatedAt,
                        CreatedBy = n.CreatedBy,
                        IsRead = n.IsRead,
                        ReadAt = n.ReadAt
                    })
                    .FirstOrDefaultAsync();

                if (notification == null)
                {
                    return NotFound(ApiResponse<NotificationDto>.ErrorResponse(
                        $"Notification with ID {id} not found.",
                        new List<string> { "Notification not found" }
                    ));
                }

                return Ok(ApiResponse<NotificationDto>.SuccessResponse(notification));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<NotificationDto>.ErrorResponse(
                    "An error occurred while retrieving the notification.",
                    new List<string> { ex.Message }
                ));
            }
        }

        // POST: api/notifications/5/mark-read
        [HttpPost("{id}/mark-read")]
        public async Task<ActionResult<ApiResponse<NotificationDto>>> MarkNotificationAsRead(int id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                
                if (notification == null)
                {
                    return NotFound(ApiResponse<NotificationDto>.ErrorResponse(
                        $"Notification with ID {id} not found.",
                        new List<string> { "Notification not found" }
                    ));
                }

                // Update notification status
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;

                _context.Entry(notification).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Create response DTO
                var responseDto = new NotificationDto
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
                };

                return Ok(ApiResponse<NotificationDto>.SuccessResponse(
                    responseDto, 
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
