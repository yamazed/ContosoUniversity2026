namespace NotificationAPI.Models
{
    public class CleanupResponse
    {
        public int DeletedCount { get; set; }
        public required string Message { get; set; }
    }
}
