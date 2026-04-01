namespace API.Core.Interfaces;

using API.Core.Entities;

public interface IConnectionCredentialRepository
{
    Task<ConnectionCredential> CreateAsync(ConnectionCredential credential, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConnectionCredential>> GetByConnectionIdAsync(Guid connectionId, CancellationToken cancellationToken = default);
    Task<ConnectionCredential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
