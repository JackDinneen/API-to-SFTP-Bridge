namespace API.Infrastructure.Repositories;

using API.Core.Entities;
using API.Core.Interfaces;
using API.Core.Models;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class ConnectionRepository : IConnectionRepository
{
    private readonly ApplicationDbContext _context;

    public ConnectionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Connection> CreateAsync(Connection connection, CancellationToken cancellationToken = default)
    {
        _context.Connections.Add(connection);
        await _context.SaveChangesAsync(cancellationToken);
        return connection;
    }

    public async Task<Connection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Connections
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Connection?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Connections
            .Include(c => c.Mappings)
            .Include(c => c.Credentials)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Connection>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Connections.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Connection>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Connections
            .Where(c => c.Status == ConnectionStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<Connection> UpdateAsync(Connection connection, CancellationToken cancellationToken = default)
    {
        _context.Connections.Update(connection);
        await _context.SaveChangesAsync(cancellationToken);
        return connection;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await GetByIdAsync(id, cancellationToken);
        if (connection != null)
        {
            connection.IsDeleted = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
