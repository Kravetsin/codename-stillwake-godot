using Stillwake.Game.Features.Items;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Gains counted where they came from (2026-11-06-gain-spots): a drill along a seam keeps one count, each piece caught its own.</summary>
public class GainSpotsTests
{
    /// <summary>A cell on the screen close in, where <see cref="GainSpots.Near"/> rules.</summary>
    private const double Close = 32;

    [Fact]
    public void GainsNearAPlaceStillShowingGoOntoItsColumn()
    {
        var spots = new GainSpots();
        // A drill working along a seam, a cell at a time.
        for (int x = 0; x <= 3; x++) spots.Add("ore", 1, 10 + x, 20, Close);
        spots.Add("ice", 1, 13, 21, Close);
        spots.Update(16);
        var spot = Assert.Single(spots.Spots);
        Assert.Equal((10.0, 20.0), (spot.X, spot.Y));
        Assert.Equal([("ore", 4), ("ice", 1)], spot.Lines.Lines.Select(l => (l.Key, l.Count)));
    }

    [Fact]
    public void PiecesFartherApartThanNearKeepTheirOwnCounts()
    {
        var spots = new GainSpots();
        spots.Add("scrap", 2, 0, 0, Close);
        spots.Add("scrap", 3, GainSpots.Near + 0.5, 0, Close);
        spots.Update(16);
        Assert.Equal([2, 3], spots.Spots.Select(s => s.Lines.Lines[0].Count));
    }

    [Fact]
    public void ZoomedOutPlacesThatWouldOverlapOnTheScreenMerge()
    {
        var spots = new GainSpots();
        // Four px a cell: two pieces fifteen cells apart stand 60 px apart side by side, their lines over each other; one twelve
        // cells above stands clear of them, a row and a half up; one past the reach across keeps its own.
        spots.Add("scrap", 2, 0, 0, 4);
        spots.Add("scrap", 3, 15, 0, 4);
        spots.Add("scrap", 4, 0, -12, 4);
        spots.Add("scrap", 1, GainSpots.WidePx / 4 + 1, 0, 4);
        spots.Update(16);
        Assert.Equal([5, 4, 1], spots.Spots.Select(s => s.Lines.Lines[0].Count));
    }

    [Fact]
    public void AGainGoesToTheNearestOfTwoPlacesInReach()
    {
        var spots = new GainSpots();
        spots.Add("ore", 1, 0, 0, Close);
        spots.Add("ore", 1, 3, 3, Close);
        spots.Add("ore", 1, 3, 1, Close);
        spots.Update(16);
        Assert.Equal([1, 2], spots.Spots.Select(s => s.Lines.Lines[0].Count));
    }

    [Fact]
    public void APlaceGoesWithItsLastLineAndANewGainThereOpensAgain()
    {
        var spots = new GainSpots();
        spots.Add("ore", 1, 0, 0, Close);
        spots.Update(GainLines.HoldMs + GainLines.FadeMs);
        Assert.Empty(spots.Spots);
        spots.Add("ore", 1, 0, 0, Close);
        Assert.Equal(1, Assert.Single(spots.Spots).Lines.Lines[0].Count);
    }

    [Fact]
    public void ANewPlacePastTheMostPushesTheOldestOff()
    {
        var spots = new GainSpots();
        for (int i = 0; i <= GainSpots.Most; i++) spots.Add("ore", 1, i * 10, 0, Close);
        Assert.Equal(GainSpots.Most, spots.Spots.Count);
        Assert.Equal(10, spots.Spots[0].X);
    }
}
