namespace API.Application.Services;

using API.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

public class SftpDeliveryService : ISftpDeliveryService
{
    private readonly ICredentialVaultService _credentialVault;
    private readonly ILogger<SftpDeliveryService> _logger;
    private const int MaxRetries = 3;

    public SftpDeliveryService(ICredentialVaultService credentialVault, ILogger<SftpDeliveryService> logger)
    {
        _credentialVault = credentialVault;
        _logger = logger;
    }

    public async Task<SftpDeliveryResult> DeliverFileAsync(SftpDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        var username = await _credentialVault.GetCredentialAsync(request.ConnectionId, "sftp-username", cancellationToken);
        var password = await _credentialVault.GetCredentialAsync(request.ConnectionId, "sftp-password", cancellationToken);

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return new SftpDeliveryResult
            {
                Success = false,
                ErrorMessage = "SFTP credentials not found in vault"
            };
        }

        var retryCount = 0;
        while (retryCount <= MaxRetries)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var client = new SftpClient(request.Host, request.Port, username, password);
                client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
                client.Connect();

                var remotePath = request.RemotePath.TrimEnd('/') + "/" + request.FileName;

                using var stream = new MemoryStream(request.FileContent);
                client.UploadFile(stream, remotePath, true);

                client.Disconnect();

                _logger.LogInformation("SFTP delivery succeeded: {RemotePath}", remotePath);

                return new SftpDeliveryResult
                {
                    Success = true,
                    RemoteFilePath = remotePath,
                    BytesTransferred = request.FileContent.Length,
                    RetryCount = retryCount
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "SFTP delivery attempt {Attempt} failed", retryCount);

                if (retryCount > MaxRetries)
                {
                    return new SftpDeliveryResult
                    {
                        Success = false,
                        ErrorMessage = $"SFTP delivery failed after {MaxRetries} retries: {ex.Message}",
                        RetryCount = retryCount
                    };
                }

                // Exponential backoff
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
            }
        }

        return new SftpDeliveryResult { Success = false, ErrorMessage = "Unexpected error" };
    }

    public async Task<SftpTestResult> TestConnectionAsync(string host, int port, string username, string credentialLabel, Guid connectionId, CancellationToken cancellationToken = default)
    {
        var password = await _credentialVault.GetCredentialAsync(connectionId, credentialLabel, cancellationToken);

        if (string.IsNullOrEmpty(password))
        {
            return new SftpTestResult
            {
                Success = false,
                ErrorMessage = "SFTP password not found in vault"
            };
        }

        try
        {
            using var client = new SftpClient(host, port, username, password);
            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(30);
            client.Connect();
            var fingerprint = client.ConnectionInfo.ServerVersion;
            client.Disconnect();

            return new SftpTestResult
            {
                Success = true,
                ServerFingerprint = fingerprint
            };
        }
        catch (Exception ex)
        {
            return new SftpTestResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
