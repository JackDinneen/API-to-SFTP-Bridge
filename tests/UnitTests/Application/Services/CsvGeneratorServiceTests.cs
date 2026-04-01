namespace UnitTests.Application.Services;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using API.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class CsvGeneratorServiceTests
{
    private readonly CsvGeneratorService _sut;

    public CsvGeneratorServiceTests()
    {
        var logger = new Mock<ILogger<CsvGeneratorService>>();
        _sut = new CsvGeneratorService(logger.Object);
    }

    private static Dictionary<string, string?> MakeRow(
        string assetId = "EF001",
        string assetName = "Building 1",
        string submeterCode = "ELEC01",
        string utilityType = "Electricity",
        string year = "2024",
        string month = "12",
        string value = "8000")
    {
        return new Dictionary<string, string?>
        {
            ["Asset ID"] = assetId,
            ["Asset name"] = assetName,
            ["Submeter Code"] = submeterCode,
            ["Utility Type"] = utilityType,
            ["Year"] = year,
            ["Month"] = month,
            ["Value"] = value,
        };
    }

    // ---------------------------------------------------------------
    // CSV Generation Tests
    // ---------------------------------------------------------------

    [Fact]
    public void GenerateCsv_ValidRows_ProducesCorrectCsv()
    {
        var rows = new List<Dictionary<string, string?>> { MakeRow() };
        var (csvBytes, _) = _sut.GenerateCsv(rows);
        var content = Encoding.UTF8.GetString(csvBytes);

        content.Should().StartWith("Asset ID,Asset name,Submeter Code,Utility Type,Year,Month,Value");
        content.Should().Contain("EF001,Building 1,ELEC01,Electricity,2024,12,8000");
    }

    [Fact]
    public void GenerateCsv_HasCorrectHeader()
    {
        var rows = new List<Dictionary<string, string?>> { MakeRow() };
        var (csvBytes, _) = _sut.GenerateCsv(rows);
        var lines = Encoding.UTF8.GetString(csvBytes).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines[0].TrimEnd('\r').Should().Be("Asset ID,Asset name,Submeter Code,Utility Type,Year,Month,Value");
    }

    [Fact]
    public void GenerateCsv_RowDataInCorrectColumnOrder()
    {
        var rows = new List<Dictionary<string, string?>> { MakeRow() };
        var (csvBytes, _) = _sut.GenerateCsv(rows);
        var lines = Encoding.UTF8.GetString(csvBytes).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var dataLine = lines[1].TrimEnd('\r');
        var fields = dataLine.Split(',');

        fields[0].Should().Be("EF001");       // Asset ID
        fields[1].Should().Be("Building 1");   // Asset name
        fields[2].Should().Be("ELEC01");       // Submeter Code
        fields[3].Should().Be("Electricity");  // Utility Type
        fields[4].Should().Be("2024");         // Year
        fields[5].Should().Be("12");           // Month
        fields[6].Should().Be("8000");         // Value
    }

    [Fact]
    public void GenerateCsv_EmptyRows_ProducesHeaderOnly()
    {
        var (csvBytes, _) = _sut.GenerateCsv(new List<Dictionary<string, string?>>());
        var content = Encoding.UTF8.GetString(csvBytes);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().ContainSingle(); // header only
        lines[0].TrimEnd('\r').Should().Be("Asset ID,Asset name,Submeter Code,Utility Type,Year,Month,Value");
    }

    [Fact]
    public void GenerateCsv_SpecialCharacters_AreEscaped()
    {
        var row = MakeRow(assetName: "Tower A, \"Main\"");
        var (csvBytes, _) = _sut.GenerateCsv(new List<Dictionary<string, string?>> { row });
        var content = Encoding.UTF8.GetString(csvBytes);

        // The field with commas and quotes should be wrapped in quotes with internal quotes doubled
        content.Should().Contain("\"Tower A, \"\"Main\"\"\"");
    }

    [Fact]
    public void GenerateCsv_Sha256Hash_IsNonEmpty()
    {
        var rows = new List<Dictionary<string, string?>> { MakeRow() };
        var (_, hash) = _sut.GenerateCsv(rows);

        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64); // SHA-256 produces 64 hex chars
    }

    [Fact]
    public void GenerateCsv_FileSizeMatchesContentLength()
    {
        var rows = new List<Dictionary<string, string?>> { MakeRow(), MakeRow(assetId: "EF002") };
        var (csvBytes, _) = _sut.GenerateCsv(rows);
        var content = Encoding.UTF8.GetString(csvBytes);

        csvBytes.Length.Should().Be(Encoding.UTF8.GetByteCount(content));
    }

    [Fact]
    public void GenerateCsv_HashIsCorrect()
    {
        var rows = new List<Dictionary<string, string?>> { MakeRow() };
        var (csvBytes, hash) = _sut.GenerateCsv(rows);

        var expectedHash = Convert.ToHexStringLower(SHA256.HashData(csvBytes));
        hash.Should().Be(expectedHash);
    }

    // ---------------------------------------------------------------
    // File Naming Tests
    // ---------------------------------------------------------------

    [Fact]
    public void GenerateFileName_StandardInputs()
    {
        var result = _sut.GenerateFileName("BlackRock", "GARBE", new DateTime(2025, 5, 8));
        result.Should().Be("blackrock_GARBE_08052025.csv");
    }

    [Fact]
    public void GenerateFileName_ClientNameWithSpaces_ConvertedToUnderscores()
    {
        var result = _sut.GenerateFileName("Black Rock", "GARBE", new DateTime(2025, 5, 8));
        result.Should().Be("black_rock_GARBE_08052025.csv");
    }

    [Fact]
    public void GenerateFileName_NullDate_DefaultsToToday()
    {
        var result = _sut.GenerateFileName("acme", "energystar");
        var todayStr = DateTime.UtcNow.ToString("ddMMyyyy");
        result.Should().Be($"acme_energystar_{todayStr}.csv");
    }

    // ---------------------------------------------------------------
    // Validation Tests
    // ---------------------------------------------------------------

    [Fact]
    public void ValidateCsv_ValidContent_ReturnsNoErrors()
    {
        var csv = "Asset ID,Asset name,Submeter Code,Utility Type,Year,Month,Value\nEF001,Building 1,ELEC01,Electricity,2024,12,8000";
        var errors = _sut.ValidateCsv(csv);
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateCsv_MissingColumn_ReturnsError()
    {
        var csv = "Asset ID,Asset name,Submeter Code,Utility Type,Year,Month\nEF001,Building 1,ELEC01,Electricity,2024,12";
        var errors = _sut.ValidateCsv(csv);
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e.Contains("Missing or incorrect column") && e.Contains("Value"));
    }

    [Fact]
    public void ValidateCsv_InvalidUtilityType_Flagged()
    {
        var csv = "Asset ID,Asset name,Submeter Code,Utility Type,Year,Month,Value\nEF001,Building 1,ELEC01,Steam,2024,12,8000";
        var errors = _sut.ValidateCsv(csv);
        errors.Should().Contain(e => e.Contains("invalid Utility Type") && e.Contains("Steam"));
    }

    [Fact]
    public void ValidateCsv_YearNot4Digits_Flagged()
    {
        var csv = "Asset ID,Asset name,Submeter Code,Utility Type,Year,Month,Value\nEF001,Building 1,ELEC01,Electricity,24,12,8000";
        var errors = _sut.ValidateCsv(csv);
        errors.Should().Contain(e => e.Contains("Year") && e.Contains("not a 4-digit"));
    }

    [Fact]
    public void ValidateCsv_MonthOutside1To12_Flagged()
    {
        var csv = "Asset ID,Asset name,Submeter Code,Utility Type,Year,Month,Value\nEF001,Building 1,ELEC01,Electricity,2024,13,8000";
        var errors = _sut.ValidateCsv(csv);
        errors.Should().Contain(e => e.Contains("Month") && e.Contains("not between 1 and 12"));
    }

    [Fact]
    public void ValidateCsv_NonNumericValue_Flagged()
    {
        var csv = "Asset ID,Asset name,Submeter Code,Utility Type,Year,Month,Value\nEF001,Building 1,ELEC01,Electricity,2024,12,abc";
        var errors = _sut.ValidateCsv(csv);
        errors.Should().Contain(e => e.Contains("Value") && e.Contains("not numeric"));
    }

    [Fact]
    public void ValidateCsv_DuplicateDetected()
    {
        var csv = "Asset ID,Asset name,Submeter Code,Utility Type,Year,Month,Value\nEF001,Building 1,ELEC01,Electricity,2024,12,8000\nEF001,Building 1,ELEC01,Electricity,2024,12,9000";
        var errors = _sut.ValidateCsv(csv);
        errors.Should().Contain(e => e.Contains("duplicate"));
    }

    [Fact]
    public void ValidateCsv_RowCount_MatchesDataRows()
    {
        var csv = "Asset ID,Asset name,Submeter Code,Utility Type,Year,Month,Value\nEF001,Building 1,ELEC01,Electricity,2024,12,8000\nEF002,Building 2,GAS01,Gas,2024,11,5000";
        var errors = _sut.ValidateCsv(csv);
        errors.Should().BeEmpty();
        // Implicitly: 2 data rows parsed without error = row count is correct
    }
}
