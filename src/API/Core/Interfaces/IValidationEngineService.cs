namespace API.Core.Interfaces;

public interface IValidationEngineService
{
    Task<ValidationReport> ValidateAsync(List<Dictionary<string, object?>> rows, CancellationToken ct = default);
}

public class ValidationReport
{
    public int TotalRows { get; set; }
    public int PassedRows { get; set; }
    public int WarningRows { get; set; }
    public int ErrorRows { get; set; }
    public List<RowValidation> Rows { get; set; } = new();
}

public class RowValidation
{
    public int RowNumber { get; set; }
    public string Status { get; set; } = "Passed"; // Passed, Warning, Error
    public List<string> Messages { get; set; } = new();
}
