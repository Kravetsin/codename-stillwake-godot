using Stillwake.Game.Features.Light;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Tests;

/// <summary>
/// Starlight falls off near where it comes in (0070): a hole in a back wall lights a few cells round it,
/// a doorway to space a few cells into the room, and open space stays lit whole.
/// </summary>
public class StarFalloffTests
{
    private const int W = 48, H = 28, Hx = 24, Hy = 14;

    /// <summary>A hall of back walls, walled round, in the middle of a world of space.</summary>
    private static Registry Hall(params string[] cut)
    {
        var rows = new List<string>();
        for (int y = 0; y < H; y++)
            rows.Add(y is < 2 or >= H - 2 ? new string(' ', W)
                : y is 2 or H - 3 ? "  " + new string('#', W - 4) + "  "
                : "  #" + new string('.', W - 6) + "#  ");
        var m = new Registry();
        World.Create(1, Layout.Parse(rows), m.All);
        return m;
    }

    private static int RedAt(StarField f, int x, int y) =>
        f.Texels[((y - f.Region.Top) * f.Region.Width + x - f.Region.Left) * 4];

    private static byte[] TexelAt(StarField f, int x, int y) =>
        f.Texels.AsSpan(((y - f.Region.Top) * f.Region.Width + x - f.Region.Left) * 4, 4).ToArray();

    private static StarField Worked(Registry m)
    {
        var field = new StarField();
        field.Work(m.Grid);
        return field;
    }

    private static int Whole => (int)(Math.Min(1, L.Star.R) * 255);

    /// <summary>How many cells from where it comes in light of <paramref name="share"/> shows in a texel: past them it rounds to nothing.</summary>
    private static int Shows(double share)
    {
        int n = 0;
        for (double light = share * Math.Max(L.Star.R, Math.Max(L.Star.G, L.Star.B)) * 255; light >= 1; light *= L.StarKeep) n++;
        return n;
    }

    private static int HoleShows() => Shows(L.StarHole);

    [Fact]
    public void AHoleInABackWallLightsOnlyTheCellsNearIt()
    {
        var m = Hall();
        int hx = Hx, hy = Hy;
        m.Grid.Clear(hy * W + hx, "back");
        var field = Worked(m);
        var none = Worked(Hall());
        Assert.Equal((int)(L.StarHole * L.Star.R * 255), RedAt(field, hx, hy));
        Assert.True(RedAt(field, hx + 1, hy) > 0 && RedAt(field, hx, hy - 1) > 0);
        // A few cells, where the stars' light once ran some forty.
        Assert.InRange(HoleShows(), 2, 10);
        // A few cells from the hole the hall is as it is with none: dark, but by the hull, where its walls let a little through.
        var r = field.Region;
        for (int y = r.Top; y <= r.Bottom; y++)
            for (int x = r.Left; x <= r.Right; x++)
                if (Math.Abs(x - hx) + Math.Abs(y - hy) >= HoleShows()) Assert.Equal(TexelAt(none, x, y), TexelAt(field, x, y));
        Assert.Equal(0, RedAt(none, hx, hy));
    }

    [Fact]
    public void OpenSpaceRoundAShipIsLitWhole()
    {
        var field = Worked(Hall());
        var r = field.Region;
        for (int y = r.Top; y <= r.Bottom; y++)
            for (int x = r.Left; x <= r.Right; x++)
                if (x < 2 || x >= W - 2 || y < 2 || y >= H - 2) Assert.True(RedAt(field, x, y) == Whole, $"{x},{y}: {RedAt(field, x, y)}");
    }

    [Fact]
    public void LightThroughADoorwayToSpaceFallsOffIntoTheRoom()
    {
        var m = Hall();
        // The hall's right wall taken out over three rows, its back wall kept: a gap to space.
        for (int y = Hy - 1; y <= Hy + 1; y++) m.Grid.Clear(y * W + W - 3, "structure");
        var field = Worked(m);
        var none = Worked(Hall());
        Assert.Equal(Whole, RedAt(field, W - 2, Hy));
        Assert.Equal((int)(L.Star.R * L.StarKeep * 255), RedAt(field, W - 3, Hy));
        int last = W - 4, step = RedAt(field, last, Hy);
        Assert.Equal((int)(L.Star.R * L.StarKeep * L.StarKeep * 255), step);
        for (int x = last - 1; x > last - 4; x--)
        {
            Assert.True(RedAt(field, x, Hy) < step, $"{x}: {RedAt(field, x, Hy)} after {step}");
            step = RedAt(field, x, Hy);
        }
        // Further in the hall is as it is walled whole.
        Assert.InRange(Shows(L.StarKeep * L.StarKeep), 2, 10);
        for (int y = 0; y < H; y++)
            for (int x = 0; x <= last - Shows(L.StarKeep * L.StarKeep) - Math.Abs(y - Hy); x++) Assert.Equal(TexelAt(none, x, y), TexelAt(field, x, y));
        Assert.Equal(0, RedAt(none, W / 2, Hy));
    }

    [Fact]
    public void PatchingAHoleDarkensOnlyItsSurroundings()
    {
        var m = Hall();
        int hx = Hx, hy = Hy, cell = hy * W + hx;
        Assert.InRange(HoleShows(), 2, 10);
        m.Grid.Clear(cell, "back");
        var field = Worked(m);
        var before = field.Texels.ToArray();
        int version = m.Grid.Version;
        m.Grid.Place(cell, Placement.BackWall());
        field.Follow(m.Grid, m.Grid.ChangedSince(version)!.Select(c => c.Cell).ToList());
        var r = field.Region;
        int changed = 0;
        for (int y = r.Top; y <= r.Bottom; y++)
            for (int x = r.Left; x <= r.Right; x++)
            {
                int at = ((y - r.Top) * r.Width + x - r.Left) * 4;
                if (before.AsSpan(at, 4).SequenceEqual(field.Texels.AsSpan(at, 4))) continue;
                changed++;
                Assert.True(Math.Abs(x - hx) + Math.Abs(y - hy) < HoleShows(), $"{x},{y} changed");
            }
        Assert.True(changed > 4, $"{changed}");
        // Patched, the hall is as one that never had the hole.
        Assert.Equal(Worked(Hall()).Texels, field.Texels);
    }
}
