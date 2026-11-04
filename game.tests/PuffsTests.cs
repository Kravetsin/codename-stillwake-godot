using System;
using System.Linq;
using Stillwake.Game.Features.Logistics;
using Xunit;
using Look = Stillwake.Game.SalvageLook;

namespace Stillwake.Game.Tests;

/// <summary>A salvage drone's puffs of air (salvage-drones): which nozzles fire, how often, how long a puff lives in air and in vacuum.</summary>
public class PuffsTests
{
    private const double Frame = 1.0 / 60;

    /// <summary>A drone held still at the origin for <paramref name="seconds"/>, then its puffs.</summary>
    private static Puffs Hold(double seconds, bool inside, bool low = false)
    {
        var puffs = new Puffs();
        for (double t = 0; t < seconds - 1e-9; t += Frame) puffs.Step(Frame, 0, 0, 0, 0, 0, 1, inside, low, true);
        return puffs;
    }

    [Fact]
    public void TheNozzlesAgainstItsAccelerationBurnAndInAirTheDownwardOnesHoldItUp()
    {
        Assert.Equal(new Puffs.Groups(2, 0, 0, 0), Puffs.Firing(Look.Fire + 1, 0, false));
        Assert.Equal(new Puffs.Groups(0, 2, 0, 0), Puffs.Firing(-Look.Fire - 1, 0, false));
        Assert.Equal(new Puffs.Groups(0, 0, 0, 2), Puffs.Firing(0, -Look.Fire - 1, false));
        Assert.Equal(new Puffs.Groups(0, 0, 2, 0), Puffs.Firing(0, Look.Fire + 1, false));
        // Under the least acceleration nothing fires in vacuum; in air the downward ones hold it up, weaker, unless they burn.
        Assert.Equal(new Puffs.Groups(0, 0, 0, 0), Puffs.Firing(Look.Fire - 1, -Look.Fire + 1, false));
        Assert.Equal(new Puffs.Groups(0, 0, 0, 1), Puffs.Firing(0, 0, true));
        Assert.Equal(new Puffs.Groups(0, 0, 2, 1), Puffs.Firing(0, Look.Fire + 1, true));
        Assert.Equal(new Puffs.Groups(0, 0, 0, 2), Puffs.Firing(0, -Look.Fire - 1, true));
    }

    [Fact]
    public void StillInAirOnlyTheDownwardNozzlesPuffEveryOtherTimeAndNoneInVacuum()
    {
        Assert.Empty(Hold(1, inside: false).Alive);
        var puffs = Hold(1, inside: true);
        Assert.NotEmpty(puffs.Alive);
        // Blown down from under it, smaller than a burn's.
        Assert.All(puffs.Alive, p => Assert.True(p.Y > 0, $"{p}"));
        int down = SalvageInk.Nozzles.Count(n => n.Group == 'D');
        int born = (int)Math.Ceiling(Look.LifeInS / Look.PuffEveryS / 2);
        Assert.InRange(puffs.Alive.Count, down * (born - 1), down * born);
        Assert.All(puffs.Alive, p => Assert.True(p.Size <= (Look.PuffBorn + Look.GrowIn) * Look.HoldShrink + 1e-9));
    }

    [Fact]
    public void APuffLivesLongerInAirThanInVacuumAndThinsAndGrowsAsItGoes()
    {
        Assert.True(Look.LifeInS > Look.LifeOutS);
        var puffs = new Puffs();
        // One burn to the right in vacuum: its left nozzles puff once, then it coasts.
        puffs.Step(Frame, 0, 0, 0, 0, 0, 1, false, false, true);
        double v = 0;
        int steps = (int)Math.Ceiling(Look.PuffEveryS / Frame) + 1;
        for (int i = 0; i < steps; i++) puffs.Step(Frame, 0, 0, v += (Look.Fire + 4) * Frame, 0, 0, 1, false, false, true);
        Assert.NotEmpty(puffs.Alive);
        Assert.All(puffs.Alive, p => Assert.True(p.X < 0, $"{p}"));
        var first = puffs.Alive[0];
        for (int i = 0; i < 6; i++) puffs.Step(Frame, 0, 0, v, 0, 0, 1, false, false, true);
        var later = puffs.Alive[0];
        Assert.True(later.Size > first.Size && later.Alpha < first.Alpha);
        for (double t = 0; t < Look.LifeOutS; t += Frame) puffs.Step(Frame, 0, 0, v, 0, 0, 1, false, false, true);
        Assert.Empty(puffs.Alive);
    }

    [Fact]
    public void FacingLeftItsNozzlesAreMirroredAndLowItsPuffsAreSmaller()
    {
        var right = new Puffs();
        var left = new Puffs();
        for (int i = 0; i < 30; i++)
        {
            right.Step(Frame, 0, 0, 0, 0, 0, 1, true, false, true);
            left.Step(Frame, 0, 0, 0, 0, 0, -1, true, false, true);
        }
        Assert.Equal(right.Alive.Select(p => Math.Round(-p.X, 6)).Order(), left.Alive.Select(p => Math.Round(p.X, 6)).Order());
        var low = Hold(0.5, inside: true, low: true);
        Assert.True(low.Alive.Max(p => p.Size) < Hold(0.5, inside: true).Alive.Max(p => p.Size));
    }

    [Fact]
    public void DockedItPuffsNoMoreAndWhatItPuffedFades()
    {
        var puffs = Hold(0.5, inside: true);
        Assert.NotEmpty(puffs.Alive);
        for (double t = 0; t < Look.LifeInS + Frame; t += Frame) puffs.Step(Frame, 0, 0, 0, 0, 0, 1, true, false, false);
        Assert.Empty(puffs.Alive);
    }

    [Fact]
    public void EveryNozzleIsInsideItsFlyingPictureAndBlowsOutOfIt()
    {
        foreach (var n in SalvageInk.Nozzles)
        {
            Assert.InRange(n.X, -SalvageInk.Margin, SalvageInk.FlyWidth + SalvageInk.Margin);
            Assert.InRange(n.Y, -SalvageInk.Margin, SalvageInk.FlyHeight + SalvageInk.Margin);
            Assert.Equal(1, Math.Abs(n.Dx) + Math.Abs(n.Dy));
            // Blowing away from the body's middle.
            Assert.True((n.X - SalvageInk.AnchorX) * n.Dx + (n.Y - SalvageInk.AnchorY) * n.Dy > 0, $"{n}");
        }
        Assert.Equal(['D', 'L', 'R', 'U'], SalvageInk.Nozzles.Select(n => n.Group).Distinct().Order());
    }
}
