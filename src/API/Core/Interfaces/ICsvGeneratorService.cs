namespace API.Core.Interfaces;

/// <summary>
/// Generates and validates CSV files conforming to Obi's 7-column schema.
/// </summary>
public interface ICsvGeneratorService
{
    /// <summary>
    /// Generates CSV content bytes from transformed row data.
    /// Returns (csvBytes, sha256Hash).
    /// </summary>
    (byte[] CsvBytes, string Sha256Hash) GenerateCsv(List<Dictionary<string, string?>> rows);

    /// <summary>
    /// Generates the file name per convention: [client]_[platform]_[DDMMYYYY].csv
    /// </summary>
    string GenerateFileName(string clientName, string platformName, DateTime? date = null);

    /// <summary>
    /// Validates CSV content against Obi's schema rules.
    /// Returns a list of validation error messages (empty if valid).
    /// </summary>
    List<string> ValidateCsv(string csvContent);
}
