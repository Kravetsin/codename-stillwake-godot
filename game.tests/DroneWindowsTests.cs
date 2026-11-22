using System;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The drone's windows: which open before the pairing, and the frame's list of them.</summary>
public class DroneWindowsTests
{
    [Fact]
    public void BeforeThePairingOnlyTheGearAndAStationsOwnWindowOpenAfterItEveryWindowDoes()
    {
        // A station's window (a machine's, a crate's, the fabricator's, the computer's) is the station's, not the drone's.
        var before = new[] { DroneWindowKind.Gear, DroneWindowKind.Station };
        foreach (var kind in Enum.GetValues<DroneWindowKind>())
        {
            Assert.Equal(Array.IndexOf(before, kind) >= 0, DroneWindows.Opens(kind, paired: false));
            Assert.True(DroneWindows.Opens(kind, paired: true));
        }
    }

    [Fact]
    public void EachWindowIsListedOnceWhereItWasLastHandedAndGoesAsItLeaves()
    {
        var windows = new DroneWindows();
        object bag = new(), machine = new();
        CornerClock bagClock = new(), machineClock = new();
        windows.Hand(bag, (0, 0, 10, 10), bagClock);
        windows.Hand(machine, (50, 0, 10, 10), machineClock);
        windows.Hand(bag, (5, 5, 10, 10), bagClock);
        Assert.Equal(2, windows.All.Count);
        Assert.Equal((5f, 5f, 10f, 10f), windows.All[0].Rect);
        windows.Drop(bag);
        Assert.Same(machine, Assert.Single(windows.All).Owner);
    }

    [Fact]
    public void ItIsOutWhileAnyIsOpenAndTillItsScreenHasGone()
    {
        var windows = new DroneWindows();
        var clock = new CornerClock();
        windows.Hand(new object(), (0, 0, 10, 10), clock);
        Assert.Equal((false, 0), windows.Out);
        clock.Step(0.5, true);
        Assert.Equal((true, 1), windows.Out);
        clock.Step(0.01, false);
        Assert.False(windows.Out.Open);
        Assert.True(windows.Out.Out > 0);
        clock.Step(1, false);
        Assert.Equal((false, 0), windows.Out);
    }
}
