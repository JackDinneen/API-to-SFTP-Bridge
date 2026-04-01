namespace API.Application.Services;

using System.Globalization;
using System.Text.Json;
using API.Core.Entities;
using API.Core.Interfaces;
using API.Core.Models;
using Microsoft.Extensions.Logging;

public class TransformEngineService : ITransformEngineService
{
    private readonly ILogger<TransformEngineService> _logger;

    // Known unit conversion factors: (fromUnit, toUnit) -> factor
    private static readonly Dictionary<(string From, string To), decimal> ConversionFactors = new()
    {
        { ("MWh", "kWh"), 1000m },
        { ("GJ", "kWh"), 277.778m },
        { ("therms", "kWh"), 29.3071m },
        { ("gallons", "m³"), 0.00378541m },
        { ("litres", "m³"), 0.001m },
        { ("kg", "t"), 0.001m },
        { ("lbs", "t"), 1m / 2204.62m },
        { ("cubic feet", "m³"), 0.0283168m },
    };

    public TransformEngineService(ILogger<TransformEngineService> logger)
    {
        _logger = logger;
    }

    public List<Dictionary<string, string?>> Transform(JsonElement sourceData, List<ConnectionMapping> mappings)
    {
        var orderedMappings = mappings.OrderBy(m => m.SortOrder).ToList();

        // First pass: find wildcard mappings that produce multiple rows
        int maxRows = 1;
        var extractedPerMapping = new List<(ConnectionMapping Mapping, List<string?> Values)>();

        foreach (var mapping in orderedMappings)
        {
            if (mapping.TransformType == TransformType.StaticValue)
            {
                extractedPerMapping.Add((mapping, new List<string?> { null }));
                continue;
            }

            var values = ExtractValues(sourceData, mapping.SourcePath);
            extractedPerMapping.Add((mapping, values));
            if (values.Count > maxRows)
                maxRows = values.Count;
        }

        // Build rows
        var rows = new List<Dictionary<string, string?>>();
        for (int i = 0; i < maxRows; i++)
        {
            var row = new Dictionary<string, string?>();
            foreach (var (mapping, values) in extractedPerMapping)
            {
                var rawValue = values.Count > i ? values[i] : (values.Count > 0 ? values[^1] : null);
                var transformed = ApplyTransform(rawValue, mapping.TransformType, mapping.TransformConfig, sourceData);
                row[mapping.TargetColumn] = transformed;
            }
            rows.Add(row);
        }

        return rows;
    }

    public List<string?> ExtractValues(JsonElement root, string jsonPath)
    {
        var results = new List<string?>();
        ExtractRecursive(root, jsonPath, results);
        return results;
    }

    private void ExtractRecursive(JsonElement current, string remainingPath, List<string?> results)
    {
        if (string.IsNullOrEmpty(remainingPath))
        {
            results.Add(GetElementStringValue(current));
            return;
        }

        // Parse next segment
        var (segment, rest) = ParseNextSegment(remainingPath);

        if (segment == "[*]")
        {
            // Wildcard array
            if (current.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in current.EnumerateArray())
                {
                    ExtractRecursive(item, rest, results);
                }
            }
            return;
        }

        if (segment.StartsWith("[") && segment.EndsWith("]"))
        {
            // Indexed array access
            var indexStr = segment[1..^1];
            if (int.TryParse(indexStr, out int index) && current.ValueKind == JsonValueKind.Array)
            {
                var arr = current.EnumerateArray().ToList();
                if (index < arr.Count)
                {
                    ExtractRecursive(arr[index], rest, results);
                }
            }
            return;
        }

        // Property access
        if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(segment, out var prop))
        {
            ExtractRecursive(prop, rest, results);
        }
        else
        {
            // Path doesn't exist
            if (string.IsNullOrEmpty(rest))
                results.Add(null);
        }
    }

    private static (string Segment, string Remainder) ParseNextSegment(string path)
    {
        // Remove leading dot
        if (path.StartsWith('.'))
            path = path[1..];

        if (path.StartsWith('['))
        {
            var endBracket = path.IndexOf(']');
            if (endBracket >= 0)
            {
                var segment = path[..(endBracket + 1)];
                var rest = path[(endBracket + 1)..];
                if (rest.StartsWith('.'))
                    rest = rest[1..];
                return (segment, rest);
            }
        }

        // Find next dot or bracket
        int nextDot = path.IndexOf('.');
        int nextBracket = path.IndexOf('[');

        int end;
        if (nextDot < 0 && nextBracket < 0)
            return (path, "");
        else if (nextDot < 0)
            end = nextBracket;
        else if (nextBracket < 0)
            end = nextDot;
        else
            end = Math.Min(nextDot, nextBracket);

        var seg = path[..end];
        var remaining = path[end..];
        if (remaining.StartsWith('.'))
            remaining = remaining[1..];
        return (seg, remaining);
    }

    private static string? GetElementStringValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDecimal().ToString(CultureInfo.InvariantCulture),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.GetRawText(),
        };
    }

    public string? ApplyTransform(string? rawValue, TransformType transformType, string? transformConfigJson, JsonElement root)
    {
        var config = !string.IsNullOrEmpty(transformConfigJson)
            ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(transformConfigJson)
            : null;

        return transformType switch
        {
            TransformType.DirectMapping => rawValue,
            TransformType.ValueMapping => ApplyValueMapping(rawValue, config),
            TransformType.UnitConversion => ApplyUnitConversion(rawValue, config),
            TransformType.DateParse => ApplyDateParse(rawValue, config),
            TransformType.StaticValue => ApplyStaticValue(config),
            TransformType.Concatenation => ApplyConcatenation(config, root),
            TransformType.Split => ApplySplit(rawValue, config),
            _ => rawValue,
        };
    }

    private static string? ApplyValueMapping(string? rawValue, Dictionary<string, JsonElement>? config)
    {
        if (rawValue == null || config == null) return rawValue;

        if (config.TryGetValue("mappings", out var mappingsElement))
        {
            var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(mappingsElement.GetRawText());
            if (mappings != null && mappings.TryGetValue(rawValue, out var mapped))
                return mapped;
        }

        return rawValue; // Unmapped values pass through
    }

    private string? ApplyUnitConversion(string? rawValue, Dictionary<string, JsonElement>? config)
    {
        if (rawValue == null || config == null) return rawValue;

        if (!decimal.TryParse(rawValue, CultureInfo.InvariantCulture, out var numericValue))
            return rawValue;

        decimal? customFactor = null;
        string? fromUnit = null;
        string? toUnit = null;

        if (config.TryGetValue("factor", out var factorEl))
            customFactor = factorEl.GetDecimal();
        if (config.TryGetValue("fromUnit", out var fromEl))
            fromUnit = fromEl.GetString();
        if (config.TryGetValue("toUnit", out var toEl))
            toUnit = toEl.GetString();

        var result = ConvertUnit(numericValue, fromUnit ?? "", toUnit ?? "", customFactor);
        return result.ToString(CultureInfo.InvariantCulture);
    }

    public decimal ConvertUnit(decimal value, string fromUnit, string toUnit, decimal? customFactor = null)
    {
        if (customFactor.HasValue)
            return value * customFactor.Value;

        if (ConversionFactors.TryGetValue((fromUnit, toUnit), out var factor))
            return value * factor;

        _logger.LogWarning("No conversion factor found for {FromUnit} -> {ToUnit}", fromUnit, toUnit);
        return value;
    }

    private static string? ApplyDateParse(string? rawValue, Dictionary<string, JsonElement>? config)
    {
        if (rawValue == null) return null;

        string? outputField = null; // "year" or "month"
        string? inputFormat = null;

        if (config != null)
        {
            if (config.TryGetValue("outputField", out var outputEl))
                outputField = outputEl.GetString();
            if (config.TryGetValue("inputFormat", out var formatEl))
                inputFormat = formatEl.GetString();
        }

        DateTime parsed;
        if (!string.IsNullOrEmpty(inputFormat))
        {
            if (!DateTime.TryParseExact(rawValue, inputFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return rawValue;
        }
        else
        {
            // Try ISO 8601 first, then year-month
            if (!DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
            {
                // Try yyyy-MM format
                if (rawValue.Length == 7 && DateTime.TryParseExact(rawValue, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                {
                    // ok
                }
                else
                {
                    return rawValue;
                }
            }
        }

        return outputField?.ToLowerInvariant() switch
        {
            "year" => parsed.Year.ToString(),
            "month" => parsed.Month.ToString(),
            _ => parsed.ToString("yyyy-MM-dd"),
        };
    }

    private static string? ApplyStaticValue(Dictionary<string, JsonElement>? config)
    {
        if (config != null && config.TryGetValue("value", out var val))
            return val.GetString();
        return null;
    }

    private static string? ApplyConcatenation(Dictionary<string, JsonElement>? config, JsonElement root)
    {
        if (config == null) return null;

        var parts = new List<string>();
        string separator = "";

        if (config.TryGetValue("separator", out var sepEl))
            separator = sepEl.GetString() ?? "";

        if (config.TryGetValue("parts", out var partsEl))
        {
            foreach (var part in partsEl.EnumerateArray())
            {
                var partStr = part.GetString();
                if (partStr != null)
                    parts.Add(partStr);
            }
        }

        return string.Join(separator, parts);
    }

    private static string? ApplySplit(string? rawValue, Dictionary<string, JsonElement>? config)
    {
        if (rawValue == null || config == null) return rawValue;

        string delimiter = "-";
        int index = 0;

        if (config.TryGetValue("delimiter", out var delimEl))
            delimiter = delimEl.GetString() ?? "-";
        if (config.TryGetValue("index", out var indexEl))
            index = indexEl.GetInt32();

        var parts = rawValue.Split(delimiter);
        return index < parts.Length ? parts[index] : null;
    }

    public List<Dictionary<string, string?>> Aggregate(
        List<Dictionary<string, string?>> rows,
        AggregationType aggregationType)
    {
        var grouped = rows.GroupBy(r =>
        {
            var assetId = r.GetValueOrDefault("Asset ID") ?? "";
            var submeter = r.GetValueOrDefault("Submeter Code") ?? "";
            var utility = r.GetValueOrDefault("Utility Type") ?? "";
            var year = r.GetValueOrDefault("Year") ?? "";
            var month = r.GetValueOrDefault("Month") ?? "";
            return $"{assetId}|{submeter}|{utility}|{year}|{month}";
        });

        var result = new List<Dictionary<string, string?>>();

        foreach (var group in grouped)
        {
            var first = group.First();
            var values = group
                .Select(r => r.GetValueOrDefault("Value"))
                .Where(v => v != null && decimal.TryParse(v, CultureInfo.InvariantCulture, out _))
                .Select(v => decimal.Parse(v!, CultureInfo.InvariantCulture))
                .ToList();

            decimal aggregated = aggregationType switch
            {
                AggregationType.Sum => values.Sum(),
                AggregationType.Average => values.Count > 0 ? values.Average() : 0,
                AggregationType.Last => values.Count > 0 ? values[^1] : 0,
                AggregationType.Max => values.Count > 0 ? values.Max() : 0,
                _ => values.Sum(),
            };

            var newRow = new Dictionary<string, string?>(first)
            {
                ["Value"] = aggregated.ToString(CultureInfo.InvariantCulture)
            };
            result.Add(newRow);
        }

        return result;
    }
}
