using FsCheck.Xunit;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

public class ScatterTests
{
    private const uint Seed = 11;
    private const int Tile = 256;

    private static List<Scattered> Over((int, int, int, int) tiles, float mean = 3.4f)
    {
        var spots = new List<Scattered>();
        Scatter.Over(Seed, Tile, mean, tiles, spots);
        return spots;
    }

    [Fact]
    public void TheSameTilesHoldTheSameThings()
    {
        Assert.Equal(Over((-3, -2, 4, 5)), Over((-3, -2, 4, 5)));
    }

    [Fact]
    public void AnotherSeedScattersOtherThings()
    {
        var other = new List<Scattered>();
        Scatter.Over(Seed + 1, Tile, 3.4f, (0, 0, 3, 3), other);
        Assert.NotEqual(Over((0, 0, 3, 3)), other);
    }

    /// <summary>Panning: a tile holds what it holds whichever range of tiles around it is drawn.</summary>
    [Property(MaxTest = 200)]
    public bool ATileHoldsTheSameWhateverIsDrawnAroundIt(short x, short y, byte w1, byte h1, byte w2, byte h2)
    {
        var a = Over((x - w1 % 4, y - h1 % 4, x + 1, y + 1));
        var b = Over((x, y, x + w2 % 4, y + h2 % 4));
        return a.Where(s => s.TileX == x && s.TileY == y).SequenceEqual(b.Where(s => s.TileX == x && s.TileY == y));
    }

    [Property(MaxTest = 200)]
    public bool EverythingLiesInItsTile(short x, short y)
    {
        return Over((x - 1, y - 1, x + 1, y + 1)).All(s => s.X >= 0 && s.X < Tile && s.Y >= 0 && s.Y < Tile);
    }

    [Fact]
    public void ATileHoldsTheMeanRoundedDownOrUpAsOftenAsItsFractionSays()
    {
        var counts = new List<int>();
        for (int y = 0; y < 100; y++)
            for (int x = 0; x < 100; x++)
                counts.Add(Scatter.CountIn(Seed, x, y, 0.3f));
        Assert.All(counts, c => Assert.InRange(c, 0, 1));
        Assert.InRange(counts.Average(), 0.28, 0.32);
    }

    [Fact]
    public void TilesRoundTowardMinusInfinity()
    {
        Assert.Equal((-1, -2, 0, 1), Scatter.Tiles(-0.5, -256.5, 255.9, 256, Tile));
    }

    [Fact]
    public void NextDrawsNumbersInTheUnitRangeAndMovesOn()
    {
        uint h = Scatter.Hash(Seed, 3, 4, 0);
        var drawn = Enumerable.Range(0, 1000).Select(_ => Scatter.Next(ref h)).ToList();
        Assert.All(drawn, u => Assert.InRange(u, 0f, 0.99999994f));
        Assert.True(drawn.Distinct().Count() > 990);
        Assert.InRange(drawn.Average(), 0.45, 0.55);
    }

    /// <summary>A plane follows its share of the camera's movement: a point of it moves that share of what a point of the world does on screen.</summary>
    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    [InlineData(2.5f)]
    public void APlaneMovesItsParallaxOfTheWorldAtEveryZoom(float zoom)
    {
        const float parallax = 0.3f;
        var before = Scatter.OnScreen(500, 300, Scatter.Centre(16000, 8000, parallax, 5, 10), zoom, 1280, 720);
        var after = Scatter.OnScreen(500, 300, Scatter.Centre(16100, 7950, parallax, 5, 10), zoom, 1280, 720);
        // A point of the world moves -100, +50 pixels times the zoom.
        Assert.Equal(-100 * zoom * parallax, after.X - before.X, 3);
        Assert.Equal(50 * zoom * parallax, after.Y - before.Y, 3);
    }

    [Fact]
    public void PanningAwayAndBackShowsThePlaneWhereItWas()
    {
        var home = Scatter.Centre(16000, 8000, 0.6f, 11, 42);
        var away = Scatter.Centre(30000, 1000, 0.6f, 11, 42);
        var back = Scatter.Centre(16000, 8000, 0.6f, 11, 42);
        Assert.NotEqual(home, away);
        Assert.Equal(Scatter.OnScreen(700, 900, home, 2, 1280, 720), Scatter.OnScreen(700, 900, back, 2, 1280, 720));
    }

    [Fact]
    public void ThePlaneDriftsLeft()
    {
        var now = Scatter.OnScreen(700, 900, Scatter.Centre(16000, 8000, 0.3f, 5, 10), 2, 1280, 720);
        var later = Scatter.OnScreen(700, 900, Scatter.Centre(16000, 8000, 0.3f, 5, 11), 2, 1280, 720);
        Assert.Equal(-5 * 2, later.X - now.X, 3);
        Assert.Equal(now.Y, later.Y);
    }

    [Fact]
    public void TheWrecksLieFartherThanTheDustAndBothBehindTheWorld()
    {
        Assert.InRange(Backdrop.WreckParallax, 0.01f, Backdrop.DustParallax - 0.01f);
        Assert.InRange(Backdrop.DustParallax, Backdrop.WreckParallax, 0.99f);
        Assert.True(Backdrop.WreckDrift < Backdrop.DustDrift);
        Assert.True(Backdrop.WreckScaleMin <= Backdrop.WreckScaleMax && Backdrop.WreckScaleMax < 1, "far wrecks are smaller than the debris they are made of");
        Assert.True(Backdrop.WreckMean < 2, "far wrecks are sparse");
        Assert.True(Backdrop.DustAlphaMin <= Backdrop.DustAlphaMax && Backdrop.DustAlphaMax <= 1);
    }
}
