using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A machine window's hologram opening and closing (machine-holograms).</summary>
public class HoloMotionTests
{
    private const double Tiny = 1e-9;

    [Fact]
    public void OpensBeamFirstThenTheWindowGrowsWithItsContents()
    {
        var holo = new HoloMotion();
        holo.Step(7, HoloLook.BeamS / 2);
        Assert.Equal((0.5, 0.0, 0.0), (Round(holo.Beam), holo.Window, holo.Contents));
        holo.Step(7, HoloLook.BeamS / 2 + HoloLook.GrowS / 2);
        Assert.Equal((1.0, 0.5, 0.5), (holo.Beam, Round(holo.Window), Round(holo.Contents)));
        holo.Step(7, HoloLook.GrowS);
        Assert.Equal((1.0, 1.0, 1.0, (int?)7), (holo.Beam, holo.Window, holo.Contents, holo.Machine));
        Assert.True(holo.Shown);
    }

    [Fact]
    public void ClosingDropsTheContentsAtOnceShrinksThenRetracts()
    {
        var holo = Open(7);
        holo.Step(null, 0);
        Assert.Equal((0.0, 1.0, 1.0), (holo.Contents, holo.Window, holo.Beam));
        holo.Step(null, HoloLook.ShrinkS / 2);
        Assert.Equal((0.5, 1.0), (Round(holo.Window), holo.Beam));
        holo.Step(null, HoloLook.ShrinkS / 2 + HoloLook.RetractS / 2);
        Assert.Equal((0.0, 0.5), (holo.Window, Round(holo.Beam)));
        holo.Step(null, HoloLook.RetractS);
        Assert.Equal((0.0, false, (int?)null), (holo.Beam, holo.Shown, holo.Machine));
    }

    [Fact]
    public void TheSameMachineOpenedAgainWhileItClosesGoesOnFromWhereItIs()
    {
        var holo = Open(7);
        holo.Step(null, HoloLook.ShrinkS / 2);
        holo.Step(7, 0);
        Assert.Equal((1.0, 0.5, 0.5), (holo.Beam, Round(holo.Window), Round(holo.Contents)));
        // Shrunk whole and the beam half back: it reaches out again, then grows.
        holo.Step(null, HoloLook.ShrinkS / 2 + HoloLook.RetractS / 2);
        Assert.Equal((0.0, 0.5), (holo.Window, Round(holo.Beam)));
        holo.Step(7, HoloLook.BeamS / 2 + HoloLook.GrowS / 4);
        Assert.Equal((1.0, 0.25), (holo.Beam, Round(holo.Window)));
    }

    [Fact]
    public void AnotherMachineStartsFromNothingTheOldGoneAtOnce()
    {
        var holo = Open(7);
        holo.Step(8, 0);
        Assert.Equal(((int?)8, 0.0, 0.0), (holo.Machine, holo.Beam, holo.Window));
        holo.Step(null, HoloLook.ShrinkS / 2);
        holo.Step(9, HoloLook.BeamS / 2);
        Assert.Equal(((int?)9, 0.5, 0.0), (holo.Machine, Round(holo.Beam), holo.Window));
    }

    [Fact]
    public void ASnapLeavesNothing()
    {
        var holo = Open(7);
        holo.Snap();
        Assert.Equal(((int?)null, 0.0, 0.0, false), (holo.Machine, holo.Beam, holo.Window, holo.Shown));
        holo.Step(null, 1);
        Assert.False(holo.Shown);
    }

    private static HoloMotion Open(int machine)
    {
        var holo = new HoloMotion();
        holo.Step(machine, 1);
        return holo;
    }

    private static double Round(double x) => System.Math.Abs(x - System.Math.Round(x, 6)) < Tiny ? System.Math.Round(x, 6) : x;
}
