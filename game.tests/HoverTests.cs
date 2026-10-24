using System;
using System.Collections.Generic;
using Stillwake.Game.Features.Logistics;
using Stillwake.Sim.Grid;
using Xunit;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Tests;

/// <summary>How a drone is drawn moving (0062, "lively"): on a spring after the sim, leaning, facing, swinging its cargo, lifted, landing.</summary>
public class HoverTests
{
    private const double Frame = 1.0 / 60, Cruise = 64;
    private static readonly DroneLook.Motion M = DroneLook.Lively;

    /// <summary>A drone following a target flying right at cruise from x = 0 for <paramref name="flyS"/> seconds, then standing there.</summary>
    private static (Hover Hover, double Target) FlyThenStop(double flyS, double standS, Action<Hover>? each = null)
    {
        var h = new Hover(1);
        h.Place(0, 0);
        h.Spin = 1;
        double t = 0, x = 0;
        for (; t < flyS; t += Frame)
        {
            x += Cruise * Frame;
            h.Step(Frame, x, 0, Cruise, 0, 1, Cruise, M);
            each?.Invoke(h);
        }
        for (double s = 0; s < standS; s += Frame)
        {
            h.Step(Frame, x, 0, 0, 0, 1, Cruise, M);
            each?.Invoke(h);
        }
        return (h, x);
    }

    [Fact]
    public void FlyingSteadilyItKeepsUpWithTheSimWithoutLagging()
    {
        var (h, x) = FlyThenStop(3, 0);
        Assert.InRange(h.X - x, -1, 1);
        Assert.InRange(h.Vx, Cruise - 2, Cruise + 2);
        Assert.Equal(1, h.Face);
        // Leaning a little into its speed while it cruises.
        Assert.InRange(h.Tilt * 180 / Math.PI, M.TiltCruise - 1.5, M.TiltCruise + 1.5);
    }

    [Fact]
    public void StoppedItIsBackOverItsTargetWithinASecond()
    {
        var (h, x) = FlyThenStop(2, 1);
        Assert.InRange(Math.Abs(h.X - x), 0, 0.5);
        Assert.InRange(Math.Abs(h.Vx), 0, 3);
    }

    [Fact]
    public void ItLeansPastAStopButNeverPastItsMost()
    {
        double most = 0, past = 0, x = 0;
        FlyThenStop(1.5, 2, h =>
        {
            most = Math.Max(most, Math.Abs(h.Tilt));
            past = Math.Max(past, h.X);
        });
        x = 1.5 * Cruise;
        Assert.InRange(most, M.TiltCruise * Math.PI / 180, M.TiltMax * Math.PI / 180 + 1e-9);
        // Lively: it springs a little past where it stops.
        Assert.True(past > x + 0.5, $"overshot only to {past} of {x}");
    }

    [Fact]
    public void SettingOffItsCargoSwingsBackAndItFacesTheWayItFlies()
    {
        var h = new Hover(2);
        h.Place(100, 0);
        h.Spin = 1;
        for (int i = 0; i < 10; i++) h.Step(Frame, 100 - Cruise * i * Frame, 0, -Cruise, 0, 1, Cruise, M);
        Assert.Equal(-1, h.Face);
        var (clampX, _) = h.Body(0, DroneLook.Clamp, 0);
        var (cargoX, cargoY) = h.Cargo(0);
        // Flying left, the stack trails to the right of its clamp, still under it.
        Assert.True(cargoX > clampX, $"cargo at {cargoX}, clamp at {clampX}");
        Assert.True(cargoY > 0);
        Assert.InRange(Math.Abs(h.Swing), 0.01, 0.6);
    }

    [Fact]
    public void ItsRotorsStandOnTheirFirstPictureAndTurnThroughAllFourSpinning()
    {
        var h = new Hover(3);
        h.Place(0, 0);
        Assert.Equal(0, h.Frame);
        var seen = new HashSet<int>();
        for (int i = 0; i < 60; i++)
        {
            h.Step(Frame, 0, 0, 0, 0, 1, Cruise, M);
            seen.Add(h.Frame);
        }
        Assert.Equal(4, seen.Count);
        Assert.InRange(h.Spin, 0.99, 1);
        Assert.InRange(Math.Abs(h.Bob(Cruise, M)), 0, M.BobAmp);
    }

    [Fact]
    public void ItFliesEightPixelsOverItsCellButTwoUnderASolidOne()
    {
        Assert.Equal(8, Hover.Lift(Structure.Empty, Structure.Empty, M));
        Assert.Equal(2, Hover.Lift(Structure.Empty, Structure.Wall, M));
        Assert.Equal(2, Hover.Lift(Structure.Empty, Structure.Floor, M));
        // Under a closed hatch from outside it, the hatch is a ceiling.
        Assert.Equal(2, Hover.Lift(Structure.Empty, Structure.DoorClosed, M));
        Assert.True(Hover.Solid(Structure.Wall));
        Assert.True(Hover.Solid(Structure.Floor));
        Assert.True(Hover.Solid(Structure.DoorClosed));
        Assert.False(Hover.Solid(Structure.Empty));
        Assert.False(Hover.Solid(Structure.DoorOpen));
        Assert.False(Hover.Solid(Structure.Ladder));
    }

    [Fact]
    public void ThroughAClosedDoorItFliesAtItsHeightDippingOnlyUnderTheWallOverIt()
    {
        // A column across a doorway three cells tall: the wall over it, the door, the floor under it.
        int[] column = [Structure.Wall, Structure.DoorClosed, Structure.DoorClosed, Structure.DoorClosed, Structure.Floor];
        double LiftAt(int row) => Hover.Lift(column[row], column[row - 1], M);
        Assert.Equal(2, LiftAt(1));
        Assert.Equal(8, LiftAt(2));
        Assert.Equal(8, LiftAt(3));
        // The same open, and beside it in the room, the same height.
        Assert.Equal(8, Hover.Lift(Structure.DoorOpen, Structure.DoorOpen, M));
        Assert.Equal(8, Hover.Lift(Structure.Empty, Structure.Empty, M));
    }

    [Fact]
    public void LandingItHoldsOverItsPadThenSettlesOntoItAsItsRotorsStop()
    {
        Assert.Equal((DroneLook.LandAbove, 1.0, false), Hover.Landing(0.3));
        var (above, spin, down) = Hover.Landing(DroneLook.LandOverS + DroneLook.LandDownS / 2);
        Assert.InRange(above, 0.5, DroneLook.LandAbove - 0.5);
        Assert.Equal(0.5, spin, 6);
        Assert.False(down);
        Assert.Equal((0.0, 0.0, true), Hover.Landing(DroneLook.LandOverS + DroneLook.LandDownS));
    }

    [Fact]
    public void ABaysSlotsThatCameToHoldADroneOrStoppedAreTold()
    {
        List<int> filled = [], emptied = [];
        Served.Changed([true, false, true, false], [false, true, true, false], filled, emptied);
        Assert.Equal([1], filled);
        Assert.Equal([0], emptied);
    }
}
