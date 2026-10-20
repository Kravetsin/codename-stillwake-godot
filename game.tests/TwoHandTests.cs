using Stillwake.Game;
using Stillwake.Game.Features.Crew;
using Xunit;
using static Stillwake.Game.Tests.PoseTests;

namespace Stillwake.Game.Tests;

/// <summary>The two-handed hold (0078): a drill held as a rifle is, both hands on its grips.</summary>
public class TwoHandTests
{
    private static readonly HeldLook[] Held = [HeldLook.Drill, HeldLook.ArcDrill];

    /// <summary>Every turn the hold takes, a hundredth of a radian apart.</summary>
    private static IEnumerable<double> Turns()
    {
        for (int i = (int)Math.Round(-TwoHand.Up * 100); i <= (int)Math.Round(TwoHand.Down * 100); i++) yield return i / 100.0;
    }

    [Fact]
    public void BothDrillsAreHeldInBothHandsTheFarOnAForegripBeforeTheNearOnesGrip()
    {
        foreach (var look in Held)
        {
            Assert.NotNull(look.Both);
            Assert.True(look.Both!.Foregrip.X > look.Grip.X + 5);
        }
        Assert.Null(HeldLook.Harpoon.Both);
        Assert.Null(HeldLook.FireAxe.Both);
    }

    [Fact]
    public void EachFistClosesOnItsGripAtEveryTurnTheHoldTakes()
    {
        foreach (var look in Held)
            foreach (double turn in Turns())
            {
                var (near, far) = TwoHand.Arms(look, turn);
                var (grip, fore) = (TwoHand.At(look, turn, look.Grip), TwoHand.At(look, turn, look.Both!.Foregrip));
                Close(grip.X, Rig.FistOf(near).X, 3);
                Close(grip.Y, Rig.FistOf(near).Y, 3);
                Close(fore.X, Rig.FistOf(far).X, 3);
                Close(fore.Y, Rig.FistOf(far).Y, 3);
            }
    }

    [Fact]
    public void NoArmCrossesTheHelmetAndNoElbowRisesAboveTheShoulder()
    {
        // The helmet's picture, from the shoulder: 13 pixels across its neck and 12 up from it.
        double neck = Rig.Neck - Rig.Shoulder;
        bool InHelmet(Vec p) => p.X > -6.5 && p.X < 6.5 && p.Y < -neck && p.Y > -neck - 12;
        foreach (var look in Held)
            foreach (double turn in Turns())
                foreach (var arm in new[] { TwoHand.Arms(look, turn).Near, TwoHand.Arms(look, turn).Far })
                {
                    var elbow = Rig.EndOf(arm with { Lower = 0 }, Rig.Arm with { Lower = 0 });
                    var fist = Rig.FistOf(arm);
                    Assert.True(elbow.Y >= 0, $"the elbow rises above the shoulder turned {turn}");
                    for (int i = 0; i <= 10; i++)
                    {
                        double t = i / 10.0;
                        Assert.False(InHelmet(new Vec(elbow.X * t, elbow.Y * t)), $"the upper arm crosses the helmet turned {turn}");
                        Assert.False(InHelmet(new Vec(elbow.X + (fist.X - elbow.X) * t, elbow.Y + (fist.Y - elbow.Y) * t)), $"the forearm crosses the helmet turned {turn}");
                    }
                }
    }

    [Fact]
    public void TurnsSoTheBitsLineRunsThroughThePointWithinTheClampAndStopsAtItBeyond()
    {
        var look = HeldLook.Drill;
        foreach (var (dx, dy) in new[] { (60.0, 8.0), (40.0, -10.0), (50.0, 20.0), (100.0, 0.0), (30.0, 8.0) })
        {
            double turn = TwoHand.AngleFor(look, dx, dy);
            Assert.InRange(turn, -TwoHand.Up + 1e-6, TwoHand.Down - 1e-6);
            var (from, to) = (TwoHand.At(look, turn, look.EdgeFrom), TwoHand.At(look, turn, look.EdgeTo));
            // How far the point lies off the bit's line.
            double ux = to.X - from.X, uy = to.Y - from.Y, off = ((dx - from.X) * uy - (dy - from.Y) * ux) / Math.Sqrt(ux * ux + uy * uy);
            Close(0, off, 3);
        }
        // Straight up or down, or behind above and below, it turns only as far as the clamp lets it.
        Assert.Equal(-TwoHand.Up, TwoHand.AngleFor(look, 0, -100));
        Assert.Equal(TwoHand.Down, TwoHand.AngleFor(look, 0, 100));
        Assert.Equal(-TwoHand.Up, TwoHand.AngleFor(look, -100, -5));
        Assert.Equal(TwoHand.Down, TwoHand.AngleFor(look, -100, 30));
        Assert.Equal((0.6, 0.6), (TwoHand.Up, TwoHand.Down));
    }

    [Fact]
    public void HoldsTheDrillAcrossTheChestItsBitNoMoreThanACellAndAHalfBeyondTheBody()
    {
        var look = HeldLook.Drill;
        // Aimed level, the tube's middle row runs across the chest, between the shoulder and the hip.
        var tube = TwoHand.At(look, 0, (12, 5));
        Assert.InRange(tube.Y, 0, Rig.Shoulder - Rig.Hip);
        Assert.InRange(TwoHand.At(look, 0, look.EdgeTo).X, Rig.W / 2, Rig.W / 2 + 1.5 * Rig.Cell);
    }
}
