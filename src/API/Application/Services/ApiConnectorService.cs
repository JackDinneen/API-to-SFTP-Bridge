namespace API.Application.Services;

using System.Net;
using System.Text.Json;
using API.Core.DTOs;
using API.Core.Interfaces;
using Microsoft.Extensions.Logging;

public class ApiConnectorService : IApiConnectorService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiConnectorService> _logger;

    private static readonly HashSet<HttpStatusCode> RetryableStatusCodes = new()
    {
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout,
        HttpStatusCode.TooManyRequests
    };

    public ApiConnectorService(IHttpClientFactory httpClientFactory, ILogger<ApiConnectorService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ApiResponse> SendRequestAsync(ApiRequestConfig config, CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        var maxRetries = config.MaxRetries;

        while (true)
        {
            attempt++;
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

                var url = BuildUrl(config.Url, config.QueryParameters);
                using var request = new HttpRequestMessage(config.Method, url);

                foreach (var header in config.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (config.Body != null)
                {
                    request.Content = new StringContent(config.Body, System.Text.Encoding.UTF8, "application/json");
                }

                var response = await client.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return new ApiResponse
                    {
                        IsSuccess = true,
                        StatusCode = (int)response.StatusCode,
                        Body = body
                    };
                }

                if (RetryableStatusCodes.Contains(response.StatusCode) && attempt <= maxRetries)
                {
                    var delay = CalculateBackoff(attempt, response.StatusCode);
                    _logger.LogWarning("Request to {Url} failed with {StatusCode}, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})",
                        config.Url, (int)response.StatusCode, delay, attempt, maxRetries);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                return new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = (int)response.StatusCode,
                    Body = body,
                    ErrorMessage = $"Request failed with status code {(int)response.StatusCode}"
                };
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt <= maxRetries)
                {
                    var delay = CalculateBackoff(attempt, null);
                    _logger.LogWarning("Request to {Url} timed out, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})",
                        config.Url, delay, attempt, maxRetries);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                return new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = 408,
                    ErrorMessage = "Request timed out"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending request to {Url}", config.Url);
                return new ApiResponse
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    ErrorMessage = ex.Message
                };
            }
        }
    }

    public async Task<bool> TestConnectionAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var config = new ApiRequestConfig
        {
            Url = url,
            Method = HttpMethod.Get,
            Headers = headers ?? new Dictionary<string, string>(),
            MaxRetries = 0,
            TimeoutSeconds = 10
        };

        var response = await SendRequestAsync(config, cancellationToken);
        return response.IsSuccess;
    }

    public async Task<PaginatedResponse> FetchPaginatedAsync(ApiRequestConfig config, CancellationToken cancellationToken = default)
    {
        var pagination = config.Pagination ?? throw new ArgumentNullException(nameof(config.Pagination));
        var pages = new List<string>();
        var currentPage = 0;
        string? cursor = null;

        while (currentPage < pagination.MaxPages)
        {
            var pageConfig = CloneConfig(config);
            ApplyPaginationParams(pageConfig, pagination, currentPage, cursor);

            var response = await SendRequestAsync(pageConfig, cancellationToken);
            if (!response.IsSuccess)
            {
                return new PaginatedResponse
                {
                    IsSuccess = false,
                    ErrorMessage = response.ErrorMessage,
                    Pages = pages,
                    TotalPages = pages.Count
                };
            }

            var body = response.Body ?? "{}";
            pages.Add(body);

            if (!HasMoreData(body, pagination, out cursor))
            {
                break;
            }

            currentPage++;
        }

        return new PaginatedResponse
        {
            IsSuccess = true,
            Pages = pages,
            TotalPages = pages.Count
        };
    }

    private static string BuildUrl(string baseUrl, Dictionary<string, string> queryParameters)
    {
        if (queryParameters.Count == 0) return baseUrl;

        var separator = baseUrl.Contains('?') ? "&" : "?";
        var queryString = string.Join("&", queryParameters.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return $"{baseUrl}{separator}{queryString}";
    }

    private static int CalculateBackoff(int attempt, HttpStatusCode? statusCode)
    {
        var baseDelay = statusCode == HttpStatusCode.TooManyRequests ? 2000 : 1000;
        return baseDelay * (int)Math.Pow(2, attempt - 1);
    }

    private static ApiRequestConfig CloneConfig(ApiRequestConfig config)
    {
        return new ApiRequestConfig
        {
            Url = config.Url,
            Method = config.Method,
            Headers = new Dictionary<string, string>(config.Headers),
            QueryParameters = new Dictionary<string, string>(config.QueryParameters),
            Body = config.Body,
            TimeoutSeconds = config.TimeoutSeconds,
            MaxRetries = config.MaxRetries
        };
    }

    private static void ApplyPaginationParams(ApiRequestConfig config, PaginationConfig pagination, int currentPage, string? cursor)
    {
        switch (pagination.Type)
        {
            case PaginationType.Offset:
                config.QueryParameters[pagination.OffsetParameterName ?? "offset"] = (currentPage * pagination.PageSize).ToString();
                config.QueryParameters[pagination.LimitParameterName ?? "limit"] = pagination.PageSize.ToString();
                break;
            case PaginationType.PageNumber:
                config.QueryParameters[pagination.PageParameterName ?? "page"] = (currentPage + 1).ToString();
                config.QueryParameters[pagination.LimitParameterName ?? "limit"] = pagination.PageSize.ToString();
                break;
            case PaginationType.Cursor:
                if (cursor != null)
                {
                    config.QueryParameters[pagination.CursorParameterName ?? "cursor"] = cursor;
                }
                config.QueryParameters[pagination.LimitParameterName ?? "limit"] = pagination.PageSize.ToString();
                break;
        }
    }

    private static bool HasMoreData(string body, PaginationConfig pagination, out string? nextCursor)
    {
        nextCursor = null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Check if data array is empty
            if (pagination.DataJsonPath != null && root.TryGetProperty(pagination.DataJsonPath, out var dataElement))
            {
                if (dataElement.ValueKind == JsonValueKind.Array && dataElement.GetArrayLength() == 0)
                {
                    return false;
                }
            }

            // Check hasMore flag
            if (pagination.HasMoreJsonPath != null && root.TryGetProperty(pagination.HasMoreJsonPath, out var hasMoreElement))
            {
                if (hasMoreElement.ValueKind == JsonValueKind.False)
                {
                    return false;
                }
            }

            // Extract cursor for cursor-based pagination
            if (pagination.Type == PaginationType.Cursor && pagination.CursorJsonPath != null)
            {
                if (root.TryGetProperty(pagination.CursorJsonPath, out var cursorElement))
                {
                    nextCursor = cursorElement.GetString();
                    return nextCursor != null;
                }
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
