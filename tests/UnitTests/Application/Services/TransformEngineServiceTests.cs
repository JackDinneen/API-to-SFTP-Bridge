namespace UnitTests.Application.Services;

using System.Globalization;
using System.Text.Json;
using API.Application.Services;
using API.Core.Entities;
using API.Core.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

public class TransformEngineServiceTests
{
    private readonly TransformEngineService _sut;

    public TransformEngineServiceTests()
    {
        var logger = new Mock<ILogger<TransformEngineService>>();
        _sut = new TransformEngineService(logger.Object);
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    // ---------------------------------------------------------------
    // Direct Mapping Tests
    // ---------------------------------------------------------------

    [Fact]
    public void ExtractValues_SimpleProperty_ReturnsValue()
    {
        var json = Parse("""{"value": "42"}""");
        var result = _sut.ExtractValues(json, "value");
        result.Should().ContainSingle().Which.Should().Be("42");
    }

    [Fact]
    public void ExtractValues_NestedPath_ReturnsValue()
    {
        var json = Parse("""{"data": {"meters": {"reading": 123.5}}}""");
        var result = _sut.ExtractValues(json, "data.meters.reading");
        result.Should().ContainSingle().Which.Should().Be("123.5");
    }

    [Fact]
    public void ExtractValues_ArrayIndexedPath_ReturnsValue()
    {
        var json = Parse("""{"data": [{"value": "first"}, {"value": "second"}]}""");
        var result = _sut.ExtractValues(json, "data[0].value");
        result.Should().ContainSingle().Which.Should().Be("first");
    }

    [Fact]
    public void ExtractValues_WildcardPath_ProducesMultipleRows()
    {
        var json = Parse("""{"data": [{"value": "A"}, {"value": "B"}, {"value": "C"}]}""");
        var result = _sut.ExtractValues(json, "data[*].value");
        result.Should().HaveCount(3);
        result.Should().ContainInOrder("A", "B", "C");
    }

    [Fact]
    public void ExtractValues_DeeplyNestedWildcards_FlattensCorrectly()
    {
        var json = Parse("""
        {
            "data": [
                {"meters": [{"reading": 10}, {"reading": 20}]},
                {"meters": [{"reading": 30}]}
            ]
        }
        """);
        var result = _sut.ExtractValues(json, "data[*].meters[*].reading");
        result.Should().HaveCount(3);
        result.Should().ContainInOrder("10", "20", "30");
    }

    [Fact]
    public void ExtractValues_MissingPath_ReturnsNull()
    {
        var json = Parse("""{"data": {"name": "test"}}""");
        var result = _sut.ExtractValues(json, "data.nonexistent");
        result.Should().ContainSingle().Which.Should().BeNull();
    }

    // ---------------------------------------------------------------
    // Value Mapping Tests
    // ---------------------------------------------------------------

    [Fact]
    public void ApplyTransform_ValueMapping_ElectricToElectricity()
    {
        var config = """{"mappings": {"electric": "Electricity", "gas_natural": "Gas"}}""";
        var root = Parse("{}");
        var result = _sut.ApplyTransform("electric", TransformType.ValueMapping, config, root);
        result.Should().Be("Electricity");
    }

    [Fact]
    public void ApplyTransform_ValueMapping_GasNaturalToGas()
    {
        var config = """{"mappings": {"electric": "Electricity", "gas_natural": "Gas"}}""";
        var root = Parse("{}");
        var result = _sut.ApplyTransform("gas_natural", TransformType.ValueMapping, config, root);
        result.Should().Be("Gas");
    }

    [Fact]
    public void ApplyTransform_ValueMapping_UnmappedPassesThrough()
    {
        var config = """{"mappings": {"electric": "Electricity"}}""";
        var root = Parse("{}");
        var result = _sut.ApplyTransform("unknown_type", TransformType.ValueMapping, config, root);
        result.Should().Be("unknown_type");
    }

    [Fact]
    public void ApplyTransform_ValueMapping_IsCaseSensitive()
    {
        var config = """{"mappings": {"electric": "Electricity"}}""";
        var root = Parse("{}");
        var result = _sut.ApplyTransform("Electric", TransformType.ValueMapping, config, root);
        // "Electric" != "electric", so it should pass through unchanged
        result.Should().Be("Electric");
    }

    // ---------------------------------------------------------------
    // Unit Conversion Tests
    // ---------------------------------------------------------------

    [Fact]
    public void ConvertUnit_MWhToKWh()
    {
        var result = _sut.ConvertUnit(5m, "MWh", "kWh");
        result.Should().Be(5000m);
    }

    [Fact]
    public void ConvertUnit_GJToKWh()
    {
        var result = _sut.ConvertUnit(1m, "GJ", "kWh");
        result.Should().Be(277.778m);
    }

    [Fact]
    public void ConvertUnit_ThermsToKWh()
    {
        var result = _sut.ConvertUnit(1m, "therms", "kWh");
        result.Should().Be(29.3071m);
    }

    [Fact]
    public void ConvertUnit_GallonsToM3()
    {
        var result = _sut.ConvertUnit(1000m, "gallons", "m³");
        result.Should().BeApproximately(3.78541m, 0.0001m);
    }

    [Fact]
    public void ConvertUnit_LitresToM3()
    {
        var result = _sut.ConvertUnit(5000m, "litres", "m³");
        result.Should().Be(5m);
    }

    [Fact]
    public void ConvertUnit_KgToT()
    {
        var result = _sut.ConvertUnit(2500m, "kg", "t");
        result.Should().Be(2.5m);
    }

    [Fact]
    public void ConvertUnit_LbsToT()
    {
        var result = _sut.ConvertUnit(2204.62m, "lbs", "t");
        result.Should().BeApproximately(1m, 0.001m);
    }

    [Fact]
    public void ConvertUnit_CubicFeetToM3()
    {
        var result = _sut.ConvertUnit(1000m, "cubic feet", "m³");
        result.Should().BeApproximately(28.3168m, 0.001m);
    }

    [Fact]
    public void ConvertUnit_CustomFactor_Applied()
    {
        var result = _sut.ConvertUnit(10m, "any", "any", customFactor: 1.5m);
        result.Should().Be(15m);
    }

    // ---------------------------------------------------------------
    // Date Parse Tests
    // ---------------------------------------------------------------

    [Fact]
    public void ApplyTransform_DateParse_Iso8601_Year()
    {
        var config = """{"outputField": "year"}""";
        var root = Parse("{}");
        var result = _sut.ApplyTransform("2024-12-15T00:00:00Z", TransformType.DateParse, config, root);
        result.Should().Be("2024");
    }

    [Fact]
    public void ApplyTransform_DateParse_Iso8601_Month()
    {
        var config = """{"outputField": "month"}""";
        var root = Parse("{}");
        var result = _sut.ApplyTransform("2024-12-15T00:00:00Z", TransformType.DateParse, config, root);
        result.Should().Be("12");
    }

    [Fact]
    public void ApplyTransform_DateParse_YearMonth()
    {
        var config = """{"outputField": "year"}""";
        var root = Parse("{}");
        var result = _sut.ApplyTransform("2024-12", TransformType.DateParse, config, root);
        result.Should().Be("2024");
    }

    [Fact]
    public void ApplyTransform_DateParse_CustomFormat()
    {
        var config = """{"inputFormat": "dd/MM/yyyy", "outputField": "month"}""";
        var root = Parse("{}");
        var result = _sut.ApplyTransform("15/12/2024", TransformType.DateParse, config, root);
        result.Should().Be("12");
    }

    // ---------------------------------------------------------------
    // Static Value Tests
    // ---------------------------------------------------------------

    [Fact]
    public void ApplyTransform_StaticValue_ReturnsConfiguredValue()
    {
        var config = """{"value": "EF001"}""";
        var root = Parse("{}");
        var result = _sut.ApplyTransform(null, TransformType.StaticValue, config, root);
        result.Should().Be("EF001");
    }

    [Fact]
    public void ApplyTransform_StaticValue_IgnoresSourceData()
    {
        var config = """{"value": "EF001"}""";
        var root = Parse("""{"data": "something"}""");
        var result = _sut.ApplyTransform("ignored_value", TransformType.StaticValue, config, root);
        result.Should().Be("EF001");
    }

    // ---------------------------------------------------------------
    // Concatenation Tests
    // ---------------------------------------------------------------

    [Fact]
    public void ApplyTransform_Concatenation_WithSeparator()
    {
        var config = """{"parts": ["EF001", "ELEC01"], "separator": "-"}""";
        var root = Parse("{}");
        var result = _sut.ApplyTransform(null, TransformType.Concatenation, config, root);
        result.Should().Be("EF001-ELEC01");
    }

    // ---------------------------------------------------------------
    // Split Tests
    // ---------------------------------------------------------------

    [Fact]
    public void ApplyTransform_Split_Index0()
    {
        var config = """{"delimiter": "-", "index": 0}""";
        var root = Parse("{}");
        var result = _sut.ApplyTransform("EF001-ELEC01", TransformType.Split, config, root);
        result.Should().Be("EF001");
    }

    [Fact]
    public void ApplyTransform_Split_Index1()
    {
        var config = """{"delimiter": "-", "index": 1}""";
        var root = Parse("{}");
        var result = _sut.ApplyTransform("EF001-ELEC01", TransformType.Split, config, root);
        result.Should().Be("ELEC01");
    }

    // ---------------------------------------------------------------
    // Aggregation Tests
    // ---------------------------------------------------------------

    [Fact]
    public void Aggregate_Sum_DailyValuesToMonthlyTotal()
    {
        var rows = Enumerable.Range(1, 30).Select(d => new Dictionary<string, string?>
        {
            ["Asset ID"] = "EF001",
            ["Asset name"] = "Building 1",
            ["Submeter Code"] = "ELEC01",
            ["Utility Type"] = "Electricity",
            ["Year"] = "2024",
            ["Month"] = "12",
            ["Value"] = "100"
        }).ToList();

        var result = _sut.Aggregate(rows, AggregationType.Sum);
        result.Should().ContainSingle();
        decimal.Parse(result[0]["Value"]!, CultureInfo.InvariantCulture).Should().Be(3000m);
    }

    [Fact]
    public void Aggregate_Average_DailyValuesToMonthlyAverage()
    {
        var rows = new List<Dictionary<string, string?>>
        {
            new() { ["Asset ID"] = "EF001", ["Submeter Code"] = "ELEC01", ["Utility Type"] = "Electricity", ["Year"] = "2024", ["Month"] = "12", ["Value"] = "100" },
            new() { ["Asset ID"] = "EF001", ["Submeter Code"] = "ELEC01", ["Utility Type"] = "Electricity", ["Year"] = "2024", ["Month"] = "12", ["Value"] = "200" },
            new() { ["Asset ID"] = "EF001", ["Submeter Code"] = "ELEC01", ["Utility Type"] = "Electricity", ["Year"] = "2024", ["Month"] = "12", ["Value"] = "300" },
        };

        var result = _sut.Aggregate(rows, AggregationType.Average);
        result.Should().ContainSingle();
        decimal.Parse(result[0]["Value"]!, CultureInfo.InvariantCulture).Should().Be(200m);
    }

    [Fact]
    public void Aggregate_MultipleMeters_GroupedSeparately()
    {
        var rows = new List<Dictionary<string, string?>>
        {
            new() { ["Asset ID"] = "EF001", ["Submeter Code"] = "ELEC01", ["Utility Type"] = "Electricity", ["Year"] = "2024", ["Month"] = "12", ["Value"] = "100" },
            new() { ["Asset ID"] = "EF001", ["Submeter Code"] = "ELEC01", ["Utility Type"] = "Electricity", ["Year"] = "2024", ["Month"] = "12", ["Value"] = "200" },
            new() { ["Asset ID"] = "EF001", ["Submeter Code"] = "GAS01", ["Utility Type"] = "Gas", ["Year"] = "2024", ["Month"] = "12", ["Value"] = "50" },
            new() { ["Asset ID"] = "EF001", ["Submeter Code"] = "GAS01", ["Utility Type"] = "Gas", ["Year"] = "2024", ["Month"] = "12", ["Value"] = "70" },
        };

        var result = _sut.Aggregate(rows, AggregationType.Sum);
        result.Should().HaveCount(2);

        var elecRow = result.First(r => r["Submeter Code"] == "ELEC01");
        decimal.Parse(elecRow["Value"]!, CultureInfo.InvariantCulture).Should().Be(300m);

        var gasRow = result.First(r => r["Submeter Code"] == "GAS01");
        decimal.Parse(gasRow["Value"]!, CultureInfo.InvariantCulture).Should().Be(120m);
    }

    // ---------------------------------------------------------------
    // Full Transform Integration Test
    // ---------------------------------------------------------------

    [Fact]
    public void Transform_WithMappings_ProducesCorrectRows()
    {
        var json = Parse("""
        {
            "buildings": [
                {"id": "B1", "name": "Tower A", "kwh": 1500},
                {"id": "B2", "name": "Tower B", "kwh": 2200}
            ]
        }
        """);

        var mappings = new List<ConnectionMapping>
        {
            new() { SourcePath = "buildings[*].id", TargetColumn = "Asset ID", TransformType = TransformType.DirectMapping, SortOrder = 1 },
            new() { SourcePath = "buildings[*].name", TargetColumn = "Asset name", TransformType = TransformType.DirectMapping, SortOrder = 2 },
            new() { SourcePath = "buildings[*].kwh", TargetColumn = "Value", TransformType = TransformType.DirectMapping, SortOrder = 3 },
        };

        var result = _sut.Transform(json, mappings);
        result.Should().HaveCount(2);
        result[0]["Asset ID"].Should().Be("B1");
        result[0]["Value"].Should().Be("1500");
        result[1]["Asset ID"].Should().Be("B2");
        result[1]["Value"].Should().Be("2200");
    }
}
