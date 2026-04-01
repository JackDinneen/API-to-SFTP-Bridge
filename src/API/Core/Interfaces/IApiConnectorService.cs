namespace API.Core.Interfaces;

using API.Core.DTOs;

public interface IApiConnectorService
{
    Task<ApiResponse> SendRequestAsync(ApiRequestConfig config, CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task<PaginatedResponse> FetchPaginatedAsync(ApiRequestConfig config, CancellationToken cancellationToken = default);
}
