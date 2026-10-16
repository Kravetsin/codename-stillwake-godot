using System.Text.Json;
using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The grid's layers saved as runs: the same cells back, and space costing nothing.</summary>
public class RunsTests
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new Runs() } };

    [Fact]
    public void WritesEachRunAsItsValueThenItsCount()
    {
        Assert.Equal("[0,3,2,1,0,2]", JsonSerializer.Serialize(new[] { 0, 0, 0, 2, 0, 0 }, Options));
        Assert.Equal("[]", JsonSerializer.Serialize(Array.Empty<int>(), Options));
    }

    [Property(MaxTest = 200)]
    public Property ReadsBackTheCellsItWrote()
    {
        // Few values, so runs of every length are drawn.
        var cells = (from n in Gen.Choose(0, 60) from a in Gen.Choose(0, 3).ArrayOf(n) select a).ToArbitrary();
        return Prop.ForAll(cells, layer =>
        {
            string text = JsonSerializer.Serialize(layer, Options);
            Assert.Equal(layer, JsonSerializer.Deserialize<int[]>(text, Options));
            Assert.Equal(layer, Runs.Cells(JsonNode.Parse(text)!));
            Assert.Equal(text, Runs.Node(layer).ToJsonString());
        });
    }

    [Fact]
    public void RefusesARunWithoutItsCountOrOfNoCells()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int[]>("[1,2,3]", Options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int[]>("[1,0]", Options));
    }
}
