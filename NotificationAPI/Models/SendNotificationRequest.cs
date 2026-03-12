namespace NotificationAPI.Models
{
    public class SendNotificationRequest
    {
        public required string EntityType { get; set; }
        public required string EntityId { get; set; }
        public required string EntityDisplayName { get; set; }
        public required string Operation { get; set; }
        public required string UserName { get; set; }
    }
}
