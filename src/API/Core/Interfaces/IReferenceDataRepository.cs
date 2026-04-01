namespace API.Core.Interfaces;

using API.Core.Entities;

public interface IReferenceDataRepository
{
    Task<ReferenceData> CreateAsync(ReferenceData referenceData, CancellationToken cancellationToken = default);
    Task<ReferenceData?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReferenceData>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReferenceData>> GetByAssetIdAsync(string assetId, CancellationToken cancellationToken = default);
    Task BulkImportAsync(IEnumerable<ReferenceData> items, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
