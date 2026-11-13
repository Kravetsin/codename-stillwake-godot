using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>How far a silhouette has come (docs/decisions/2026-11-12-one-to-understand.md).</summary>
public class BoardPencilTests
{
    [Fact]
    public void ItIsTheShareOfPartsEverHad()
    {
        Assert.Equal(0.5, BoardPencil.Of([true, false], null));
        Assert.Equal(0, BoardPencil.Of([false, false], null));
        Assert.Equal(1, BoardPencil.Of([true, true], null));
    }

    [Fact]
    public void ACraftWithEveryPartHadAndNoBenchIsShortOnlyByItsStation()
    {
        Assert.Equal(2 / 3d, BoardPencil.Of([true, true], false), 9);
        Assert.Equal(1, BoardPencil.Of([true, true], true));
    }

    [Fact]
    public void AStationIsOneMorePartOfTheShare() =>
        Assert.Equal(0.5, BoardPencil.Of([false], true));

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
        Assert.Equal(0, BoardPencil.Of(wire, _ => false, _ => false));
        Assert.Equal(0.5, BoardPencil.Of(wire, _ => true, _ => false));
        Assert.Equal(1, BoardPencil.Of(wire, _ => true, kind => kind == "former"));
    }
}
