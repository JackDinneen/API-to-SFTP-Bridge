namespace API.Core.Interfaces;

using System.Text.Json;
using API.Core.Entities;
using API.Core.Models;

/// <summary>
/// Transforms raw API JSON response data into flattened CSV row data
/// using configured ConnectionMappings.
/// </summary>
public interface ITransformEngineService
{
    /// <summary>
    /// Applies all mappings to the raw JSON and returns transformed row dictionaries.
    /// Each dictionary maps TargetColumn -> transformed value string.
    /// </summary>
    List<Dictionary<string, string?>> Transform(JsonElement sourceData, List<ConnectionMapping> mappings);

    /// <summary>
    /// Extracts value(s) from a JSON element using a dot/bracket path.
    /// Wildcard [*] paths may produce multiple values.
    /// </summary>
    List<string?> ExtractValues(JsonElement root, string jsonPath);

    /// <summary>
    /// Applies a specific transform to a raw value given the transform type and config.
    /// </summary>
    string? ApplyTransform(string? rawValue, TransformType transformType, string? transformConfigJson, JsonElement root);

    /// <summary>
    /// Converts a numeric value from one unit to another using known conversion factors
    /// or a custom factor from config.
    /// </summary>
    decimal ConvertUnit(decimal value, string fromUnit, string toUnit, decimal? customFactor = null);

    /// <summary>
    /// Aggregates daily/sub-monthly records into monthly totals or averages,
    /// grouped by (AssetId, SubmeterCode, UtilityType, Year, Month).
    /// </summary>
    List<Dictionary<string, string?>> Aggregate(
        List<Dictionary<string, string?>> rows,
        AggregationType aggregationType);
}
