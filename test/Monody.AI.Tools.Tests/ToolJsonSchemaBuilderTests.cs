using System;
using System.Linq;
using System.Text.Json;
using Monody.AI.Provider.OpenAI;
using Monody.AI.Tools.Capabilities.Weather;
using Monody.AI.Tools.ToolHandler;
using Xunit;

namespace Monody.AI.Tools.Tests;

public sealed class ToolJsonSchemaBuilderTests
{
    [Fact]
    public void FromParameters_Converts_WeatherToolRequest_Correctly()
    {
        var parametersSchema = ToolSchema.ParametersFromType<WeatherToolRequest>();

        Assert.NotNull(parametersSchema);
        Assert.NotEmpty(parametersSchema);

        var doc = ToolJsonSchemaBuilder.FromParameters(parametersSchema);

        var root = doc.RootElement;
        Assert.Equal("object", root.GetProperty("type").GetString());

        var properties = root.GetProperty("properties");
        var expectedProps = new[] { "LocationQuery", "Latitude", "Longitude", "Range", "Days", "Units" };
        foreach (var name in expectedProps)
        {
            Assert.True(properties.TryGetProperty(name, out _), $"Missing property '{name}'");
        }

        // Types
        Assert.Equal("string", properties.GetProperty("LocationQuery").GetProperty("type").GetString());
        Assert.Equal("number", properties.GetProperty("Latitude").GetProperty("type").GetString());
        Assert.Equal("number", properties.GetProperty("Longitude").GetProperty("type").GetString());
        Assert.Equal("string", properties.GetProperty("Range").GetProperty("type").GetString());
        Assert.Equal("number", properties.GetProperty("Days").GetProperty("type").GetString());
        Assert.Equal("string", properties.GetProperty("Units").GetProperty("type").GetString());

        // Range & Units should expose enum values and default must be one of them
        var rangeProp = properties.GetProperty("Range");
        Assert.True(rangeProp.TryGetProperty("enum", out var rangeEnum));
        Assert.True(rangeEnum.ValueKind == JsonValueKind.Array && rangeEnum.GetArrayLength() > 0);
        Assert.True(rangeProp.TryGetProperty("default", out var rangeDefault) && rangeDefault.ValueKind == JsonValueKind.String);
        var rangeDefaultStr = rangeDefault.GetString();
        var rangeEnumSet = rangeEnum.EnumerateArray().Select(e => e.GetString()).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(rangeDefaultStr, rangeEnumSet);

        var unitsProp = properties.GetProperty("Units");
        Assert.True(unitsProp.TryGetProperty("enum", out var unitsEnum));
        Assert.True(unitsEnum.ValueKind == JsonValueKind.Array && unitsEnum.GetArrayLength() > 0);
        Assert.True(unitsProp.TryGetProperty("default", out var unitsDefault) && unitsDefault.ValueKind == JsonValueKind.String);
        var unitsDefaultStr = unitsDefault.GetString();
        var unitsEnumSet = unitsEnum.EnumerateArray().Select(e => e.GetString()).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(unitsDefaultStr, unitsEnumSet);

        // Numeric constraints (min/max) — Latitude, Longitude, Days
        var lat = properties.GetProperty("Latitude");
        Assert.True(lat.TryGetProperty("minimum", out var latMin)); Assert.Equal(-90, latMin.GetDouble());
        Assert.True(lat.TryGetProperty("maximum", out var latMax)); Assert.Equal(90, latMax.GetDouble());

        var lon = properties.GetProperty("Longitude");
        Assert.True(lon.TryGetProperty("minimum", out var lonMin)); Assert.Equal(-180, lonMin.GetDouble());
        Assert.True(lon.TryGetProperty("maximum", out var lonMax)); Assert.Equal(180, lonMax.GetDouble());

        var days = properties.GetProperty("Days");
        Assert.True(days.TryGetProperty("minimum", out var daysMin)); Assert.Equal(1, daysMin.GetDouble());
        Assert.True(days.TryGetProperty("maximum", out var daysMax)); Assert.Equal(14, daysMax.GetDouble());

        // Required groups -> should be represented as "oneOf" with two groups:
        // - group 1: ["LocationQuery"]
        // - group 2: ["Latitude", "Longitude"]
        Assert.True(root.TryGetProperty("oneOf", out var oneOf));
        Assert.Equal(2, oneOf.GetArrayLength());

        var groups = oneOf.EnumerateArray()
            .Select(el => el.GetProperty("required").EnumerateArray().Select(x => x.GetString()).OrderBy(s => s).ToArray())
            .ToArray();

        // Normalize expected groups for comparison
        var expectedGroupA = new[] { "LocationQuery" }.OrderBy(s => s).ToArray();
        var expectedGroupB = new[] { "Latitude", "Longitude" }.OrderBy(s => s).ToArray();

        Assert.Contains(groups, g => g.SequenceEqual(expectedGroupA));
        Assert.Contains(groups, g => g.SequenceEqual(expectedGroupB));
    }
}