namespace API.Application.Services;

using API.Core.Interfaces;
using Microsoft.Extensions.Logging;

public class ValidationEngineService : IValidationEngineService
{
    private readonly IReferenceDataRepository _referenceDataRepo;
    private readonly ILogger<ValidationEngineService> _logger;

    private static readonly HashSet<string> AcceptedUtilityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Electricity", "Gas", "Water", "Waste", "District Heating", "District Cooling",
        "DistrictHeating", "DistrictCooling"
    };

    public ValidationEngineService(
        IReferenceDataRepository referenceDataRepo,
        ILogger<ValidationEngineService> logger)
    {
        _referenceDataRepo = referenceDataRepo;
        _logger = logger;
    }

    public async Task<ValidationReport> ValidateAsync(List<Dictionary<string, object?>> rows, CancellationToken ct = default)
    {
        var report = new ValidationReport { TotalRows = rows.Count };
        var allReferenceData = await _referenceDataRepo.GetAllAsync(ct);

        // Build lookup structures from reference data
        var assetIds = new HashSet<string>(allReferenceData.Select(r => r.AssetId), StringComparer.OrdinalIgnoreCase);
        var submeterToAsset = allReferenceData
            .GroupBy(r => r.SubmeterCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new HashSet<string>(g.Select(r => r.AssetId), StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

        // Track seen combinations for duplicate detection
        var seenCombinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Build historical averages from all rows for range check
        var historicalAverages = BuildHistoricalAverages(rows);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowValidation = new RowValidation { RowNumber = i + 1 };

            var assetId = GetStringValue(row, "AssetId") ?? GetStringValue(row, "Asset ID") ?? "";
            var submeterCode = GetStringValue(row, "SubmeterCode") ?? GetStringValue(row, "Submeter Code") ?? "";
            var utilityType = GetStringValue(row, "UtilityType") ?? GetStringValue(row, "Utility Type") ?? "";
            var year = GetIntValue(row, "Year");
            var month = GetIntValue(row, "Month");
            var value = GetDecimalValue(row, "Value");

            // Check 1: Asset ID exists in reference data
            if (!string.IsNullOrEmpty(assetId) && !assetIds.Contains(assetId))
            {
                rowValidation.Messages.Add($"Asset ID '{assetId}' not found in reference data");
                rowValidation.Status = "Error";
            }

            // Check 2: Submeter Code exists and matches Asset ID
            if (!string.IsNullOrEmpty(submeterCode))
            {
                if (!submeterToAsset.ContainsKey(submeterCode))
                {
                    rowValidation.Messages.Add($"Submeter Code '{submeterCode}' not found in reference data");
                    if (rowValidation.Status != "Error") rowValidation.Status = "Error";
                }
                else if (!string.IsNullOrEmpty(assetId) && !submeterToAsset[submeterCode].Contains(assetId))
                {
                    rowValidation.Messages.Add($"Submeter Code '{submeterCode}' is not associated with Asset ID '{assetId}'");
                    if (rowValidation.Status != "Error") rowValidation.Status = "Error";
                }
            }

            // Check 3: Utility Type is in accepted list
            if (!string.IsNullOrEmpty(utilityType) && !AcceptedUtilityTypes.Contains(utilityType))
            {
                rowValidation.Messages.Add($"Utility Type '{utilityType}' is not in the accepted list");
                if (rowValidation.Status != "Error") rowValidation.Status = "Error";
            }

            // Check 4: Duplicate detection (Asset ID + Submeter Code + Year + Month)
            if (!string.IsNullOrEmpty(assetId) && !string.IsNullOrEmpty(submeterCode) && year.HasValue && month.HasValue)
            {
                var key = $"{assetId}|{submeterCode}|{year}|{month}";
                if (!seenCombinations.Add(key))
                {
                    rowValidation.Messages.Add($"Duplicate detected: Asset ID '{assetId}', Submeter Code '{submeterCode}', Year {year}, Month {month}");
                    if (rowValidation.Status != "Error") rowValidation.Status = "Error";
                }
            }

            // Check 5: Range check — flag values > 10x historical average
            if (value.HasValue && !string.IsNullOrEmpty(assetId) && !string.IsNullOrEmpty(submeterCode))
            {
                var meterKey = $"{assetId}|{submeterCode}";
                if (historicalAverages.TryGetValue(meterKey, out var avgInfo) && avgInfo.Count > 1 && avgInfo.Average > 0)
                {
                    if (value.Value > avgInfo.Average * 10)
                    {
                        rowValidation.Messages.Add(
                            $"Value {value.Value} exceeds 10x the historical average ({avgInfo.Average:F2}) for meter '{assetId}/{submeterCode}'");
                        if (rowValidation.Status == "Passed") rowValidation.Status = "Warning";
                    }
                }
            }

            report.Rows.Add(rowValidation);
        }

        report.PassedRows = report.Rows.Count(r => r.Status == "Passed");
        report.WarningRows = report.Rows.Count(r => r.Status == "Warning");
        report.ErrorRows = report.Rows.Count(r => r.Status == "Error");

        _logger.LogInformation(
            "Validation complete: {Total} rows, {Passed} passed, {Warning} warnings, {Error} errors",
            report.TotalRows, report.PassedRows, report.WarningRows, report.ErrorRows);

        return report;
    }

    private static Dictionary<string, (decimal Average, int Count)> BuildHistoricalAverages(List<Dictionary<string, object?>> rows)
    {
        var meterValues = new Dictionary<string, List<decimal>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var assetId = GetStringValue(row, "AssetId") ?? GetStringValue(row, "Asset ID") ?? "";
            var submeterCode = GetStringValue(row, "SubmeterCode") ?? GetStringValue(row, "Submeter Code") ?? "";
            var value = GetDecimalValue(row, "Value");

            if (!string.IsNullOrEmpty(assetId) && !string.IsNullOrEmpty(submeterCode) && value.HasValue)
            {
                var key = $"{assetId}|{submeterCode}";
                if (!meterValues.ContainsKey(key))
                    meterValues[key] = new List<decimal>();
                meterValues[key].Add(value.Value);
            }
        }

        return meterValues.ToDictionary(
            kvp => kvp.Key,
            kvp => (Average: kvp.Value.Average(v => v), Count: kvp.Value.Count),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string? GetStringValue(Dictionary<string, object?> row, string key)
    {
        if (row.TryGetValue(key, out var val) && val != null)
            return val.ToString();
        return null;
    }

    private static int? GetIntValue(Dictionary<string, object?> row, string key)
    {
        var str = GetStringValue(row, key);
        if (str != null && int.TryParse(str, out var result))
            return result;
        return null;
    }

    private static decimal? GetDecimalValue(Dictionary<string, object?> row, string key)
    {
        var str = GetStringValue(row, key);
        if (str != null && decimal.TryParse(str, out var result))
            return result;
        return null;
    }
}
