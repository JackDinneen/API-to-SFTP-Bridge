namespace API.Application.Services;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using API.Core.Interfaces;
using Microsoft.Extensions.Logging;

public class CsvGeneratorService : ICsvGeneratorService
{
    private static readonly string[] RequiredColumns =
    {
        "Asset ID", "Asset name", "Submeter Code", "Utility Type", "Year", "Month", "Value"
    };

    private static readonly HashSet<string> ValidUtilityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Electricity", "Gas", "Water", "Waste", "District Heating", "District Cooling"
    };

    private readonly ILogger<CsvGeneratorService> _logger;

    public CsvGeneratorService(ILogger<CsvGeneratorService> logger)
    {
        _logger = logger;
    }

    public (byte[] CsvBytes, string Sha256Hash) GenerateCsv(List<Dictionary<string, string?>> rows)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine(string.Join(",", RequiredColumns));

        // Data rows
        foreach (var row in rows)
        {
            var values = RequiredColumns.Select(col =>
            {
                var val = row.GetValueOrDefault(col) ?? "";
                return EscapeCsvField(val);
            });
            sb.AppendLine(string.Join(",", values));
        }

        var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = SHA256.HashData(csvBytes);
        var hashString = Convert.ToHexStringLower(hash);

        return (csvBytes, hashString);
    }

    private static string EscapeCsvField(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    public string GenerateFileName(string clientName, string platformName, DateTime? date = null)
    {
        var effectiveDate = date ?? DateTime.UtcNow;

        // Client name: lowercase, spaces to underscores
        var client = clientName.ToLowerInvariant().Replace(" ", "_");

        // Date in DDMMYYYY format
        var dateStr = effectiveDate.ToString("ddMMyyyy");

        return $"{client}_{platformName}_{dateStr}.csv";
    }

    public List<string> ValidateCsv(string csvContent)
    {
        var errors = new List<string>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (lines.Count == 0)
        {
            errors.Add("CSV is empty");
            return errors;
        }

        // Validate header
        var header = ParseCsvLine(lines[0]);
        for (int i = 0; i < RequiredColumns.Length; i++)
        {
            if (i >= header.Length || header[i] != RequiredColumns[i])
            {
                errors.Add($"Missing or incorrect column: expected '{RequiredColumns[i]}' at position {i + 1}");
            }
        }

        if (errors.Count > 0)
            return errors; // Can't validate rows if header is wrong

        // Validate data rows
        var seen = new HashSet<string>();
        for (int rowIdx = 1; rowIdx < lines.Count; rowIdx++)
        {
            var fields = ParseCsvLine(lines[rowIdx]);
            var lineNum = rowIdx + 1;

            if (fields.Length < RequiredColumns.Length)
            {
                errors.Add($"Row {lineNum}: insufficient columns ({fields.Length} found, {RequiredColumns.Length} required)");
                continue;
            }

            var utilityType = fields[3];
            if (!ValidUtilityTypes.Contains(utilityType))
            {
                errors.Add($"Row {lineNum}: invalid Utility Type '{utilityType}'");
            }

            var year = fields[4];
            if (!Regex.IsMatch(year, @"^\d{4}$"))
            {
                errors.Add($"Row {lineNum}: Year '{year}' is not a 4-digit number");
            }

            var month = fields[5];
            if (!int.TryParse(month, out var monthNum) || monthNum < 1 || monthNum > 12)
            {
                errors.Add($"Row {lineNum}: Month '{month}' is not between 1 and 12");
            }

            var value = fields[6];
            if (!decimal.TryParse(value, CultureInfo.InvariantCulture, out _))
            {
                errors.Add($"Row {lineNum}: Value '{value}' is not numeric");
            }

            // Duplicate check
            var key = $"{fields[0]}|{fields[2]}|{fields[4]}|{fields[5]}";
            if (!seen.Add(key))
            {
                errors.Add($"Row {lineNum}: duplicate (Asset ID '{fields[0]}' + Submeter Code '{fields[2]}' + Year '{fields[4]}' + Month '{fields[5]}')");
            }
        }

        return errors;
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // skip next quote
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}
