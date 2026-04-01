namespace API.Infrastructure.Repositories;

using API.Core.Entities;
using API.Core.Interfaces;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class ConnectionCredentialRepository : IConnectionCredentialRepository
{
    private readonly ApplicationDbContext _context;

    public ConnectionCredentialRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ConnectionCredential> CreateAsync(ConnectionCredential credential, CancellationToken cancellationToken = default)
    {
        _context.ConnectionCredentials.Add(credential);
        await _context.SaveChangesAsync(cancellationToken);
        return credential;
    }

    public async Task<IReadOnlyList<ConnectionCredential>> GetByConnectionIdAsync(Guid connectionId, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionCredentials
            .Where(c => c.ConnectionId == connectionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ConnectionCredential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ConnectionCredentials.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var credential = await GetByIdAsync(id, cancellationToken);
        if (credential != null)
        {
            credential.IsDeleted = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
