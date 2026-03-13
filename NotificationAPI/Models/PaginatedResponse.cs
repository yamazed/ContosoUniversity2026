namespace NotificationAPI.Models
{
    public class PaginatedResponse<T>
    {
        public required List<T> Data { get; set; }
        public required PaginationMetadata Pagination { get; set; }
    }

    public class PaginationMetadata
    {
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
