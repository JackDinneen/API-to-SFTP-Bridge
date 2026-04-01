namespace API.Core.Interfaces;

using API.Core.Entities;

public interface IConnectionRepository
{
    Task<Connection> CreateAsync(Connection connection, CancellationToken cancellationToken = default);
    Task<Connection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Connection?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Connection>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Connection>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Connection> UpdateAsync(Connection connection, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
