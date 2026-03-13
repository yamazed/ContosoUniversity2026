namespace NotificationAPI.Models
{
    public class BulkMarkReadResponse
    {
        public int UpdatedCount { get; set; }
        public required string Message { get; set; }
    }
}
