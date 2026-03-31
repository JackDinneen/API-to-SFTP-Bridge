namespace API.Core.Entities;

public class ReferenceData : BaseEntity
{
    public string AssetId { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string SubmeterCode { get; set; } = string.Empty;
    public string UtilityType { get; set; } = string.Empty;
    public Guid UploadedById { get; set; }
    public UserProfile UploadedBy { get; set; } = null!;
}
