using System;
using System.Collections.Generic;

namespace ServerWeb.BLL.DTO
{
    public class PagedListResponse<T>
    {
    public List<T> Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
}
