namespace API.Core.DTOs;

public class ApiRequestConfig
{
    public string Url { get; set; } = string.Empty;
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> QueryParameters { get; set; } = new();
    public string? Body { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public PaginationConfig? Pagination { get; set; }
}

public class ApiResponse
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? Body { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> ResponseHeaders { get; set; } = new();
}

public class PaginationConfig
{
    public PaginationType Type { get; set; }
    public int PageSize { get; set; } = 100;
    public int MaxPages { get; set; } = 100;
    public string? PageParameterName { get; set; } = "page";
    public string? OffsetParameterName { get; set; } = "offset";
    public string? LimitParameterName { get; set; } = "limit";
    public string? CursorParameterName { get; set; } = "cursor";
    public string? CursorJsonPath { get; set; } = "nextCursor";
    public string? HasMoreJsonPath { get; set; } = "hasMore";
    public string? DataJsonPath { get; set; } = "data";
}

public enum PaginationType
{
    Offset,
    PageNumber,
    Cursor
}

public class PaginatedResponse
{
    public List<string> Pages { get; set; } = new();
    public int TotalPages { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
