using FsCheck;
using FsCheck.Xunit;
using Stillwake.Game.Features.Flora;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Leaves falling, tumbling, carried by the wind, lying on the ground and fading (plant-stirs).</summary>
public class LeavesTests
{
    private const double Frame = 1 / 60.0;
    private static float Calm(float x) => 0;
    private static readonly Leaves.Flight Flight = new(Fall: 30, Sway: 10, SwayHz: 1, FlipHz: 1.3f, Spin: 1, Tilt: 0.5f, Catch: 1);

    private static Leaves Over(float ground, int seed = 1)
    {
        var random = new Random(seed);
        return new Leaves(random.NextDouble, (x, y) => ground, [Flight]);
    }

    [Fact]
    public void ALeafFallsSlowerThanAStoneLandsOnTheGroundLiesAndFadesAway()
    {
        var leaves = Over(ground: 100);
        var leaf = leaves.Add(0, 0, 0, 0, picture: 0);
        float fastest = 0;
        int frames = 0;
        while (leaf.Lying < 0 && frames++ < 1200)
        {
            leaves.Advance(Frame, Calm);
            fastest = Math.Max(fastest, leaf.Vy);
        }
        Assert.Equal(100, leaf.Y);
        Assert.True(fastest <= Flight.Fall * 1.15f + 0.5f, $"falls at most its own pace: {fastest}");
        Assert.True(frames > 60 * 2, $"a hundred pixels take it over two seconds: {frames} frames");
        Assert.Equal(1, leaf.Alpha);
        for (int i = 0; i < (Leaves.Lie + 0.5) * 60; i++) leaves.Advance(Frame, Calm);
        Assert.InRange(leaf.Alpha, 0.01f, 0.99f);
        Assert.Single(leaves.All);
        for (int i = 0; i < Leaves.Fade * 60; i++) leaves.Advance(Frame, Calm);
        Assert.Empty(leaves.All);
    }

    [Fact]
    public void EachLookFallsAtItsOwnPaceTheSprigFirstTheBlotLast()
    {
        var random = new Random(3);
        var leaves = new Leaves(random.NextDouble, (x, y) => 100, [FloraLooks.Flights["blot"], FloraLooks.Flights["sprig"]]);
        var blot = leaves.Add(0, 0, 0, 0, picture: 0);
        var sprig = leaves.Add(0, 0, 0, 0, picture: 1);
        for (int i = 0; i < 60 * 4; i++) leaves.Advance(Frame, Calm);
        Assert.True(sprig.Lying >= 0, "a sprig is down in four seconds");
        Assert.True(blot.Lying < 0, $"a blot still flutters: {blot.Y}");
    }

    [Fact]
    public void ALeafPulledAlongSlowsBehindWhatPulledIt()
    {
        var leaves = Over(ground: 1000);
        var leaf = leaves.Add(0, 0, 200, 0, picture: 0);
        for (int i = 0; i < 60; i++) leaves.Advance(Frame, Calm);
        Assert.InRange(leaf.X, 40, 200);
        Assert.True(Math.Abs(leaf.Vx) < 80, $"slowed: {leaf.Vx}");
    }

    [Fact]
    public void TheWindCarriesALeafItsWay()
    {
        var leaves = Over(ground: 10_000);
        var leaf = leaves.Add(0, 0, 0, 0, picture: 0);
        for (int i = 0; i < 180; i++) leaves.Advance(Frame, _ => 100);
        Assert.True(leaf.X > 150, $"carried: {leaf.X}");
    }

    [Fact]
    public void ALeafTumblesItsWidthChangingAsItFalls()
    {
        var leaves = Over(ground: 10_000);
        var leaf = leaves.Add(0, 0, 0, 0, picture: 0);
        var widths = new HashSet<int>();
        for (int i = 0; i < 120; i++)
        {
            leaves.Advance(Frame, Calm);
            widths.Add((int)Math.Round(leaf.Width * 10));
        }
        Assert.True(widths.Count > 6, $"{widths.Count} widths");
        Assert.Contains(widths, w => w < 0);
    }

    [Fact]
    public void PastTheMostTheOldestGo()
    {
        var leaves = Over(ground: 1000);
        var first = leaves.Add(0, 0, 0, 0, 0);
        for (int i = 0; i < Leaves.Most; i++) leaves.Add(i, 0, 0, 0, 0);
        Assert.Equal(Leaves.Most, leaves.All.Count);
        Assert.DoesNotContain(first, leaves.All);
    }

    /// <summary>However a leaf is thrown, it never ends under the ground it falls on.</summary>
    [Property(MaxTest = 60)]
    public void NeverUnderTheGround(NormalFloat vx, NormalFloat vy, PositiveInt seed)
    {
        var leaves = Over(ground: 50, seed.Get);
        var leaf = leaves.Add(0, 0, (float)Math.Clamp(vx.Get, -400, 400), (float)Math.Clamp(vy.Get, -400, 400), 0);
        for (int i = 0; i < 600 && leaves.All.Count > 0; i++)
        {
            leaves.Advance(Frame, Calm);
            Assert.True(leaf.Y <= 50, $"{leaf.Y}");
        }
    }
}

/// <summary>A bush's leaves run out as they are shaken off and grow back (plant-stirs).</summary>
public class LeafageTests
{
    [Fact]
    public void ABushGivesItsLeavesTillTheyRunOutThenGrowsThemBack()
    {
        var bush = FloraLooks.Bush;
        var leafage = new Leafage();
        Assert.Equal((int)bush.Leafage, leafage.Take(1, bush, 1000, 0));
        Assert.Equal(0, leafage.Take(1, bush, 5, 0.1));
        Assert.Equal(4, leafage.Take(1, bush, 4, 0.1 + 4 / bush.Regrow));
        Assert.Equal(3, leafage.Take(2, bush, 3, 0.1));
        leafage.Forget(1);
        Assert.Equal(10, leafage.Take(1, bush, 10, 0.2));
    }
}
