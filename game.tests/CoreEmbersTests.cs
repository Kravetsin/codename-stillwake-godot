using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The drone's core filling (drone-abilities): it shows as full as its charge, eased, and each gain beyond its own filling sends embers in
/// that land on it.
/// </summary>
public class CoreEmbersTests
{
    private static int Flying(CoreEmbers e, double t) => e.At(t, new CoreEmber[CoreEmbers.Most]);

    /// <summary>The most a 16 ms frame's own filling may add.</summary>
    private static readonly int Own = (int)Math.Ceiling(Content.Abilities.Regen * (Sim.World.TicksPerSecond * 0.016 + 1));

    [Fact]
    public void TheFirstReadingADropAndItsOwnFillingSendNoEmbersLootSendsByItsWorth()
    {
        var e = new CoreEmbers();
        e.Step(200, 300, 0, 0.016);
        Assert.Equal(0, Flying(e, 0.1));
        e.Step(100, 300, 1, 0.016);
        Assert.Equal(0, Flying(e, 1.1));
        e.Step(100 + Own, 300, 1.5, 0.016);
        Assert.Equal(0, Flying(e, 1.6));
        e.Step(160 + 2 * Own, 300, 2, 0.016);
        Assert.Equal(60 / CoreEmbers.WorthEach, Flying(e, 2.3));
        e.Step(161 + 3 * Own, 300, 3, 0.016);
        Assert.Equal(1, Flying(e, 3.1));
        e.Step(400, 400, 4, 0.016);
        Assert.Equal(CoreEmbers.MostEach, Flying(e, 4.45));
    }

    [Fact]
    public void EmbersFlyInFromRoundItAndLandWithAFlare()
    {
        var e = new CoreEmbers();
        e.Step(0, 300, 0, 0.016);
        e.Step(20 + Own, 300, 1, 0.016);
        var at = new CoreEmber[CoreEmbers.Most];
        Assert.Equal(1, e.At(1.01, at));
        float start = MathF.Sqrt(at[0].X * at[0].X + at[0].Y * at[0].Y);
        Assert.InRange(start, CoreEmbers.From * 0.95f, CoreEmbers.From);
        e.At(1 + CoreEmbers.Flight * 0.9, at);
        Assert.True(MathF.Sqrt(at[0].X * at[0].X + at[0].Y * at[0].Y) < start / 2);
        Assert.Equal(0, e.At(1 + CoreEmbers.Flight + 0.01, at));
        e.Step(20 + Own, 300, 1 + CoreEmbers.Flight + 0.01, 0.02);
        Assert.True(e.Flare > 0.9f);
        e.Step(20 + Own, 300, 2, 0.5);
        Assert.Equal(0, e.Flare);
    }

    [Fact]
    public void ItShowsAsFullAsItsChargeEased()
    {
        var e = new CoreEmbers();
        e.Step(150, 300, 0, 0.016);
        Assert.InRange(e.Shown, 0.001f, 0.1f);
        for (int i = 1; i < 200; i++) e.Step(150, 300, i * 0.016, 0.016);
        Assert.InRange(e.Shown, 0.49f, 0.5f);
    }

    [Fact]
    public void ItsBeadsShowWhileItIsShortOfFullAndGoAMomentAfterItFills()
    {
        var e = new CoreEmbers();
        e.Step(1200, 1200, 0, 0.016);
        Assert.Equal(0, e.Beads);
        for (int i = 1; i < 60; i++) e.Step(1000, 1200, i * 0.016, 0.016);
        Assert.True(e.Beads > 0.95f);
        e.Step(1200, 1200, 1, 0.016);
        for (int i = 1; i < 30; i++) e.Step(1200, 1200, 1 + i * 0.016, 0.016);
        Assert.True(e.Beads > 0.9f);
        for (int i = 30; i < 150; i++) e.Step(1200, 1200, 1 + i * 0.016, 0.016);
        Assert.True(e.Beads < 0.05f);
    }

    [Fact]
    public void ABeadIsLitPerThrowTheFillingOnePartlyAndTheyArcOverTheCoalLeftToRight()
    {
        Assert.Equal([1f, 1f, 0.5f, 0f], new[] { 0, 1, 2, 3 }.Select(i => CoreEmbers.BeadLit(i, 500, 200)));
        var (first, last) = (CoreEmbers.BeadAt(0, 6), CoreEmbers.BeadAt(5, 6));
        Assert.True(first.X < 0 && last.X > 0 && first.Y < 0 && last.Y < 0);
        Assert.Equal(first.Y, last.Y, 3);
        Assert.Equal(CoreEmbers.BeadRadius, MathF.Sqrt(first.X * first.X + first.Y * first.Y), 3);
    }
}
