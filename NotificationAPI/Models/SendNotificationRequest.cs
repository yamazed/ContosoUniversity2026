namespace NotificationAPI.Models
{
    public class SendNotificationRequest
    {
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string EntityDisplayName { get; set; }
        public string Operation { get; set; }
        public string UserName { get; set; }
    }
}
