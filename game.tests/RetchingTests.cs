using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A hero throwing up (food-tuning, bile-slide): the bile out of a bare mouth, on the glass of a helmet, and the hunch.</summary>
public class RetchingTests
{
    private static Ink Drawn(RetchSpray spray, bool floor)
    {
        var ink = new Ink();
        if (floor) spray.DrawFloor(ink);
        else spray.DrawAir(ink);
        return ink;
    }

    /// <summary>Played through at 60 frames a second from the purge, thrown ahead from a mouth 30 px over a deck, or afloat.</summary>
    private static RetchSpray Played(double ms, bool afloat)
    {
        var spray = new RetchSpray();
        for (double t = 0; t < ms; t += 1000 / 60.0) spray.Step(1 / 60.0, Retching.Gush(t), (0, -30), (1, 0), afloat, 0);
        return spray;
    }

    [Fact]
    public void OnADeckTheBileFallsIntoPuddlesOnTheFloorThatDryAway()
    {
        var spray = Played(3000, afloat: false);
        Assert.False(Drawn(spray, floor: false).Points.Count > 0, "nothing still in the air");
        var puddles = Drawn(spray, floor: true);
        Assert.NotEmpty(puddles.Points);
        Assert.All(puddles.Points, p => Assert.InRange(p.Y, -2, 2));
        Assert.True(puddles.Points.Max(p => p.X) > 10, "thrown ahead of the hero");
        Assert.True(Played(3000 + RetchSpray.MarkMs, afloat: false).Empty);
    }

    [Fact]
    public void AfloatTheBileDriftsAndFadesWithNoPuddle()
    {
        var spray = Played(1400, afloat: true);
        Assert.Empty(Drawn(spray, floor: true).Points);
        Assert.NotEmpty(Drawn(spray, floor: false).Points);
        Assert.True(Played(1600 + RetchSpray.DriftMs, afloat: true).Empty);
    }

    [Fact]
    public void TheGlassClearsAndTheBodyStraightensAfterIt()
    {
        var ink = new Ink();
        Retching.Film(ink, 1, p => p, 1);
        Assert.NotEmpty(ink.Points);
        ink.Clear();
        Retching.Film(ink, (Retching.Ms - Retching.GushMs) / 1000, p => p, 1);
        Assert.Empty(ink.Points);
        Assert.True(Retching.Hunch(Retching.GushMs + 300) > 0.9);
        Assert.Equal(0, Retching.Lean(Retching.Ms));
        Assert.Equal(0, Retching.Hunch(double.PositiveInfinity));
    }
}
