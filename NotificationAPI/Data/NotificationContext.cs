using Microsoft.EntityFrameworkCore;
using NotificationAPI.Models;

namespace NotificationAPI.Data
{
    public class NotificationContext : DbContext
    {
        public NotificationContext(DbContextOptions<NotificationContext> options) 
            : base(options)
        {
        }
        
        public DbSet<Notification> Notifications { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notification");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                // Create indexes for efficient querying
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Notification_CreatedAt");
                entity.HasIndex(e => e.IsRead).HasDatabaseName("IX_Notification_IsRead");
            });
        }
    }
}
