namespace API.Core.Entities;

using API.Core.Models;

public class ConnectionMapping : BaseEntity
{
    public Guid ConnectionId { get; set; }
    public Connection Connection { get; set; } = null!;
    public string SourcePath { get; set; } = string.Empty; // JSON path e.g., "data[0].meters[].value"
    public string TargetColumn { get; set; } = string.Empty; // One of: Asset ID, Asset name, Submeter Code, Utility Type, Year, Month, Value
    public TransformType TransformType { get; set; } = TransformType.DirectMapping;
    public string? TransformConfig { get; set; } // JSON config for the transform
    public int SortOrder { get; set; }
}
