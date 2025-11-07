using System;

namespace ContosoUniversity.DTOs
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string Operation { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
