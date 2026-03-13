namespace NotificationAPI.Models
{
    public class BulkMarkReadRequest
    {
        public required List<int> NotificationIds { get; set; }
    }
}
