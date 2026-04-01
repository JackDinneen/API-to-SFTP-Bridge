namespace API.Infrastructure.Repositories;

using API.Core.Entities;
using API.Core.Interfaces;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class ReferenceDataRepository : IReferenceDataRepository
{
    private readonly ApplicationDbContext _context;

    public ReferenceDataRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReferenceData> CreateAsync(ReferenceData referenceData, CancellationToken cancellationToken = default)
    {
        _context.ReferenceData.Add(referenceData);
        await _context.SaveChangesAsync(cancellationToken);
        return referenceData;
    }

    public async Task<ReferenceData?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ReferenceData
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ReferenceData>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ReferenceData.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReferenceData>> GetByAssetIdAsync(string assetId, CancellationToken cancellationToken = default)
    {
        return await _context.ReferenceData
            .Where(r => r.AssetId == assetId)
            .ToListAsync(cancellationToken);
    }

    public async Task BulkImportAsync(IEnumerable<ReferenceData> items, CancellationToken cancellationToken = default)
    {
        _context.ReferenceData.AddRange(items);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetByIdAsync(id, cancellationToken);
        if (item != null)
        {
            item.IsDeleted = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
