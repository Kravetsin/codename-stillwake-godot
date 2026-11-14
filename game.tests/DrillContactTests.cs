using System;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A drill's sparks ride its haze spiral back out of a wreck and leave at the bore's mouth (drill-contact).</summary>
public class DrillContactTests
{
    // The bit's point at the origin, aimed right, its bit 7 px behind, its reach 12 px past.
    private static readonly ToolContact.Screw Right = new((0, 0), (1, 0), (0, 1), 7, 12, 0);

    private static ToolContact.Bore? Bore(Func<double, double, bool> inside) => ToolContact.BoreOf(Right, WorkLooks.ScrewStep, inside);

    [Fact]
    public void AFacePastThePointIsBoredFromItsFaceToTheReach()
    {
        var bore = Assert.NotNull(Bore((x, _) => x > 5));
        Assert.Equal(5.5, bore.Mouth, 9);
        Assert.Equal(12, bore.Deepest, 9);
    }

    [Fact]
    public void ABitPushedInIsBoredFromBehindItsPoint()
    {
        var bore = Assert.NotNull(Bore((x, _) => x > -3));
        Assert.Equal(-2.5, bore.Mouth, 9);
        Assert.Equal(12, bore.Deepest, 9);
    }

    [Fact]
    public void OnlyTheFirstStretchIsTheBore()
    {
        var bore = Assert.NotNull(Bore((x, _) => x is > 2 and < 4 or > 8 and < 10));
        Assert.Equal(2.5, bore.Mouth, 9);
        Assert.Equal(3.5, bore.Deepest, 9);
    }

    [Fact]
    public void NoBoreWhileItMissesOrItsWholeBitIsIn()
    {
        Assert.Null(Bore((_, _) => false));
        Assert.Null(Bore((x, _) => x > -10));
        Assert.Null(Bore((_, y) => y > 1));
    }

    [Fact]
    public void ASparkRidesBackAlongTheAxisWhereTheSpiralIsAtItsWidest()
    {
        // At s = 0 with phase pi/2 the spiral stands out its whole radius and moves only along the axis.
        var (x, y) = Right.At(0, Math.PI / 2);
        Assert.Equal(0, x, 9);
        Assert.Equal(WorkLooks.ScrewRadius, y, 9);
        var (wx, wy) = Right.Rides(0, Math.PI / 2, 100);
        Assert.Equal(-1, wx, 9);
        Assert.Equal(0, wy, 9);
    }

    private static ToolSparks Bored(ToolContact.Bore? bore, double ms)
    {
        var sparks = new ToolSparks();
        string drill = Content.Weapons.Drill.Item;
        sparks.Begin(0);
        sparks.Touch((1, 7), drill, (5, 0), (-1, 0), 0, screw: Right, bore: bore);
        sparks.End();
        sparks.Begin(ms);
        sparks.Touch((1, 7), drill, (5, 0), (-1, 0), ms, screw: Right, bore: bore);
        sparks.End();
        return sparks;
    }

    [Fact]
    public void SparksRideTheSpiralInsideTheBoreThenFlyBackOutOfItsMouth()
    {
        var bore = new ToolContact.Bore(5.5, 12);
        var sparks = Bored(bore, 200);
        int deep = 0, flying = 0;
        for (int n = 0; n < sparks.Count; n++)
        {
            if (sparks.At(n) is not var (_, head, _)) continue;
            if (head.X < bore.Mouth - 1e-6)
            {
                flying++;
                continue;
            }
            // Never in front of the bore's end or off the spiral while inside it.
            Assert.True(head.X <= bore.Deepest + 1e-6 && Math.Abs(head.Y) <= WorkLooks.ScrewRadius + 1e-6, $"spark {n} at {head}");
            if (head.X > bore.Mouth + 1) deep++;
        }
        Assert.True(deep > 0 && flying > 0, $"{deep} riding deep in the bore, {flying} flying");
    }

    [Fact]
    public void ABoreThrowsAsManyAsATouchDoes()
    {
        Assert.Equal(Bored(null, 1000).Count, Bored(new ToolContact.Bore(5.5, 12), 1000).Count);
    }
}
