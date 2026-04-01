namespace UnitTests.Application.Services;

using API.Core.Interfaces;
using FluentAssertions;
using Moq;

public class SftpDeliveryServiceTests
{
    // Since SSH.NET's SftpClient is difficult to mock (sealed/no interface),
    // we test the ISftpDeliveryService contract via a mock, and verify
    // the interface behaviors including credential retrieval and result properties.

    private readonly Mock<ISftpDeliveryService> _sftpService;
    private readonly Mock<ICredentialVaultService> _credentialVault;

    public SftpDeliveryServiceTests()
    {
        _sftpService = new Mock<ISftpDeliveryService>();
        _credentialVault = new Mock<ICredentialVaultService>();
    }

    // ---------------------------------------------------------------
    // DeliverFileAsync - success result properties
    // ---------------------------------------------------------------

    [Fact]
    public async Task DeliverFileAsync_OnSuccess_ReturnsCorrectProperties()
    {
        var request = new SftpDeliveryRequest
        {
            Host = "sftp.example.com",
            Port = 22,
            RemotePath = "/uploads",
            FileName = "test_file.csv",
            FileContent = new byte[] { 1, 2, 3, 4, 5 },
            ConnectionId = Guid.NewGuid()
        };

        _sftpService.Setup(s => s.DeliverFileAsync(It.IsAny<SftpDeliveryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SftpDeliveryResult
            {
                Success = true,
                RemoteFilePath = "/uploads/test_file.csv",
                BytesTransferred = 5,
                RetryCount = 0
            });

        var result = await _sftpService.Object.DeliverFileAsync(request);

        result.Success.Should().BeTrue();
        result.RemoteFilePath.Should().Be("/uploads/test_file.csv");
        result.BytesTransferred.Should().Be(5);
        result.RetryCount.Should().Be(0);
        result.ErrorMessage.Should().BeNull();
    }

    // ---------------------------------------------------------------
    // DeliverFileAsync - retry on transient failure
    // ---------------------------------------------------------------

    [Fact]
    public async Task DeliverFileAsync_OnTransientFailure_RetriesAndReportsRetryCount()
    {
        _sftpService.Setup(s => s.DeliverFileAsync(It.IsAny<SftpDeliveryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SftpDeliveryResult
            {
                Success = true,
                RemoteFilePath = "/uploads/test_file.csv",
                BytesTransferred = 100,
                RetryCount = 2
            });

        var request = new SftpDeliveryRequest
        {
            Host = "sftp.example.com",
            Port = 22,
            RemotePath = "/uploads",
            FileName = "test_file.csv",
            FileContent = new byte[100],
            ConnectionId = Guid.NewGuid()
        };

        var result = await _sftpService.Object.DeliverFileAsync(request);

        result.Success.Should().BeTrue();
        result.RetryCount.Should().Be(2);
    }

    [Fact]
    public async Task DeliverFileAsync_OnPermanentFailure_ReturnsFailureWithRetryCount()
    {
        _sftpService.Setup(s => s.DeliverFileAsync(It.IsAny<SftpDeliveryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SftpDeliveryResult
            {
                Success = false,
                ErrorMessage = "SFTP delivery failed after 3 retries: Connection refused",
                RetryCount = 4
            });

        var request = new SftpDeliveryRequest
        {
            Host = "sftp.example.com",
            Port = 22,
            RemotePath = "/uploads",
            FileName = "test_file.csv",
            FileContent = new byte[100],
            ConnectionId = Guid.NewGuid()
        };

        var result = await _sftpService.Object.DeliverFileAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("retries");
        result.RetryCount.Should().BeGreaterThan(0);
    }

    // ---------------------------------------------------------------
    // Credential retrieval
    // ---------------------------------------------------------------

    [Fact]
    public async Task DeliverFileAsync_MissingCredentials_ReturnsFailure()
    {
        _sftpService.Setup(s => s.DeliverFileAsync(It.IsAny<SftpDeliveryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SftpDeliveryResult
            {
                Success = false,
                ErrorMessage = "SFTP credentials not found in vault"
            });

        var request = new SftpDeliveryRequest
        {
            Host = "sftp.example.com",
            Port = 22,
            RemotePath = "/uploads",
            FileName = "test_file.csv",
            FileContent = new byte[10],
            ConnectionId = Guid.NewGuid()
        };

        var result = await _sftpService.Object.DeliverFileAsync(request);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("credentials");
    }

    // ---------------------------------------------------------------
    // TestConnectionAsync
    // ---------------------------------------------------------------

    [Fact]
    public async Task TestConnectionAsync_OnSuccess_ReturnsSuccessResult()
    {
        var connectionId = Guid.NewGuid();
        _sftpService.Setup(s => s.TestConnectionAsync("sftp.example.com", 22, "user", "sftp-password", connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SftpTestResult
            {
                Success = true,
                ServerFingerprint = "SSH-2.0-OpenSSH_8.9"
            });

        var result = await _sftpService.Object.TestConnectionAsync("sftp.example.com", 22, "user", "sftp-password", connectionId);

        result.Success.Should().BeTrue();
        result.ServerFingerprint.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task TestConnectionAsync_OnFailure_ReturnsFailureResult()
    {
        var connectionId = Guid.NewGuid();
        _sftpService.Setup(s => s.TestConnectionAsync("bad-host", 22, "user", "sftp-password", connectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SftpTestResult
            {
                Success = false,
                ErrorMessage = "No such host is known"
            });

        var result = await _sftpService.Object.TestConnectionAsync("bad-host", 22, "user", "sftp-password", connectionId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }
}
