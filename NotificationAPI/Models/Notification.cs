using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationAPI.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public required string EntityType { get; set; }
        
        [Required]
        [StringLength(50)]
        public required string EntityId { get; set; }
        
        [Required]
        [StringLength(20)]
        public required string Operation { get; set; } // CREATE, UPDATE, DELETE
        
        [Required]
        [StringLength(256)]
        public required string Message { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; }
        
        [StringLength(100)]
        public required string CreatedBy { get; set; }
        
        public bool IsRead { get; set; }
        
        public DateTime? ReadAt { get; set; }
    }
    
    public enum EntityOperation
    {
        CREATE,
        UPDATE,
        DELETE
    }
}
