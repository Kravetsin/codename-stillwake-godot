using System.Linq;
using Stillwake.Game.Features.Research;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The tree laying itself out on its map.</summary>
public class TechMapTests
{
    private static readonly TechMap Map = new(Spec.Research.Categories);

    [Fact]
    public void ALineAlwaysRunsLeftToRight()
    {
        Assert.All(Spec.Research.Nodes, node => Assert.All(node.Needs, need => Assert.True(Map.Spots[need].Column < Map.Spots[node.Id].Column, $"{need} to {node.Id}")));
    }

    [Fact]
    public void NoTwoNodesShareASpotAndEachStandsInItsBand()
    {
        Assert.Equal(Spec.Research.Nodes.Count, Map.Spots.Values.Distinct().Count());
        foreach (var band in Map.Bands)
            Assert.All(band.Category.Nodes, node => Assert.InRange(Map.Spots[node.Id].Row, band.FirstRow, band.FirstRow + band.Rows - 1));
        Assert.Equal(Map.Rows, Map.Bands.Sum(b => b.Rows));
    }
}
