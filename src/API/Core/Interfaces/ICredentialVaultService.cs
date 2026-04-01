namespace API.Core.Interfaces;

using API.Core.DTOs;
using API.Core.Models;

public interface ICredentialVaultService
{
    Task<CredentialReference> StoreCredentialAsync(StoreCredentialRequest request, CancellationToken cancellationToken = default);
    Task<string?> GetCredentialAsync(Guid connectionId, string label, CancellationToken cancellationToken = default);
    Task DeleteCredentialAsync(Guid connectionId, string label, CancellationToken cancellationToken = default);
    Task<bool> CredentialExistsAsync(Guid connectionId, string label, CancellationToken cancellationToken = default);
    Task<AuthHeaders> BuildAuthHeadersAsync(Guid connectionId, AuthType authType, CancellationToken cancellationToken = default);
}
