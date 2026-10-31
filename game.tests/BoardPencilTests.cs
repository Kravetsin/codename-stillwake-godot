using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How far a silhouette has come (docs/plans/board.md).</summary>
public class BoardPencilTests
{
    [Fact]
    public void ItIsTheMeanOfEachPartsShareCappedAtEnough()
    {
        Assert.Equal(0.25, BoardPencil.Of([(1, 2), (0, 5)], null));
        Assert.Equal(0.5, BoardPencil.Of([(99, 2), (0, 5)], null));
        Assert.Equal(1, BoardPencil.Of([(2, 2), (5, 5)], null));
    }

    [Fact]
    public void ACraftWithCountsMetAndNoBenchIsShortOnlyByItsStation()
    {
        Assert.Equal(2 / 3d, BoardPencil.Of([(10, 10), (4, 4)], false), 9);
        Assert.Equal(1, BoardPencil.Of([(10, 10), (4, 4)], true));
    }

    [Fact]
    public void AStationIsOneMorePartOfTheMean() =>
        Assert.Equal(0.5, BoardPencil.Of([(0, 2)], true));

    [Fact]
    public void NothingToHaveIsDone()
    {
        Assert.Equal(1, BoardPencil.Of([], null));
        Assert.Equal(0, BoardPencil.Of([], false));
    }

    [Fact]
    public void ItReadsWhatTheCrewHasOfAThing()
    {
        var wire = BoardThings.All.Single(t => t.Id == "wire");
        Assert.Equal(["workbench", "former"], wire.Stations);
        Assert.Equal(0.25, BoardPencil.Of(wire, _ => wire.Parts[0].Enough / 2, _ => false));
        Assert.Equal(0.75, BoardPencil.Of(wire, _ => wire.Parts[0].Enough / 2, kind => kind == "former"));
    }
}
