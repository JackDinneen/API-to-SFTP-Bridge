namespace API.Core.Entities;

public class SyncRunRecord : BaseEntity
{
    public Guid SyncRunId { get; set; }
    public SyncRun SyncRun { get; set; } = null!;
    public string? AssetId { get; set; }
    public string? AssetName { get; set; }
    public string? SubmeterCode { get; set; }
    public string? UtilityType { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public decimal? Value { get; set; }
    public bool IsValid { get; set; } = true;
    public string? ValidationMessage { get; set; }
}
