using System.Collections.Generic;

namespace ContosoUniversity.DTOs
{
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }

        public PaginatedResponse()
        {
            Items = new List<T>();
        }

        public PaginatedResponse(List<T> items, int count, int pageIndex, int pageSize)
        {
            Items = items;
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = count;
            TotalPages = (int)System.Math.Ceiling(count / (double)pageSize);
            HasPreviousPage = pageIndex > 1;
            HasNextPage = pageIndex < TotalPages;
        }
    }
}
