namespace API.Core.Interfaces;

public interface ISftpDeliveryService
{
    Task<SftpDeliveryResult> DeliverFileAsync(SftpDeliveryRequest request, CancellationToken cancellationToken = default);
    Task<SftpTestResult> TestConnectionAsync(string host, int port, string username, string credentialLabel, Guid connectionId, CancellationToken cancellationToken = default);
}

public class SftpDeliveryRequest
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string RemotePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public Guid ConnectionId { get; set; }
}

public class SftpDeliveryResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RemoteFilePath { get; set; }
    public long BytesTransferred { get; set; }
    public int RetryCount { get; set; }
}

public class SftpTestResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ServerFingerprint { get; set; }
}
