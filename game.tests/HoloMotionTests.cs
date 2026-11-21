using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A hologram window opening and closing, and the look «Перо» along its opening.</summary>
public class HoloMotionTests
{
    private const double Tiny = 1e-9;

    [Fact]
    public void ItOpensOverItsTimeAndClosesFasterPlayingItBack()
    {
        var holo = new HoloMotion();
        holo.Step(7, HoloLook.OpenS / 2);
        Assert.Equal((0.5, true, (int?)7), (Round(holo.Progress), holo.Shown, holo.Machine));
        holo.Step(7, HoloLook.OpenS);
        Assert.Equal(1.0, holo.Progress);
        holo.Step(null, HoloLook.CloseS / 4);
        Assert.Equal(0.75, Round(holo.Progress));
        holo.Step(null, HoloLook.CloseS);
        Assert.Equal((0.0, false, (int?)null), (holo.Progress, holo.Shown, holo.Machine));
        Assert.True(HoloLook.CloseS < HoloLook.OpenS);
    }

    [Fact]
    public void TheSameMachineOpenedAgainWhileItClosesGoesOnFromWhereItIs()
    {
        var holo = Open(7);
        holo.Step(null, HoloLook.CloseS / 2);
        holo.Step(7, 0);
        Assert.Equal(0.5, Round(holo.Progress));
        holo.Step(7, HoloLook.OpenS / 4);
        Assert.Equal(0.75, Round(holo.Progress));
    }

    [Fact]
    public void AnotherMachineStartsFromNothingTheOldGoneAtOnce()
    {
        var holo = Open(7);
        holo.Step(8, 0);
        Assert.Equal(((int?)8, 0.0), (holo.Machine, holo.Progress));
        holo.Step(9, HoloLook.OpenS / 2);
        Assert.Equal(((int?)9, 0.5), (holo.Machine, Round(holo.Progress)));
    }

    [Fact]
    public void ASnapLeavesNothing()
    {
        var holo = Open(7);
        holo.Snap();
        Assert.Equal(((int?)null, 0.0, false), (holo.Machine, holo.Progress, holo.Shown));
        holo.Step(null, 1);
        Assert.False(holo.Shown);
    }

    [Fact]
    public void ClosedNothingIsDrawn()
    {
        var f = HoloPen.At(0);
        Assert.True(f.Closed);
        Assert.Equal((0.0, 0.0, 0.0, 0.0, 0.0, 0.0), (f.Reach, f.Trace, f.Ground, f.Grid, f.Contents, f.Settle));
        Assert.Equal(0, HoloPen.ControlsAlpha(f));
        Assert.False(new HoloMotion().Shown);
    }

    [Fact]
    public void TheBeamReachesFirstThenThePensRunThenTheGridThenTheContents()
    {
        var reaching = HoloPen.At(HoloLook.ReachEnd / 2);
        Assert.Equal((0.5, 0.0, 0.0), (Round(reaching.Reach), reaching.Trace, reaching.Ground));
        var tracing = HoloPen.At(0.3);
        Assert.True(tracing.Reach == 1 && tracing.Trace is > 0 and < 1 && tracing.Ground > 0 && tracing.Grid == 0);
        var wiping = HoloPen.At(0.55);
        Assert.True(wiping.Grid is > 0 and < 1 && wiping.Contents == 0 && HoloPen.ControlsAlpha(wiping) == HoloLook.GridAlpha);
        var open = HoloPen.At(1);
        Assert.Equal((1.0, 1.0, 1.0, 1.0, 1.0, 1.0), (open.Reach, open.Trace, open.Ground, open.Grid, open.Contents, open.Settle));
        Assert.Equal(1, HoloPen.ControlsAlpha(open));
    }

    [Fact]
    public void TheFrameIsWholeExactlyAtTheTracesEnd()
    {
        double end = HoloLook.TraceFrom + HoloLook.TraceFor;
        Assert.False(HoloPen.At(end - 1e-6).Traced);
        Assert.True(HoloPen.At(end).Traced);
        Assert.Equal(1.0, HoloPen.At(end).Along);
    }

    [Fact]
    public void ThePensBeamsSettleOnTheNearCornersOnlyOnceTheFrameIsWhole()
    {
        Assert.True(HoloLook.SettleFrom >= HoloLook.TraceFrom + HoloLook.TraceFor);
        Assert.Equal(0.0, HoloPen.At(HoloLook.SettleFrom).Settle);
        Assert.Equal(1.0, HoloPen.At(HoloLook.SettleFrom + HoloLook.SettleFor).Settle);
    }

    [Fact]
    public void HeldByADroneWindowsCornersItShowsAsTheirScreenDoesAndForgetsItsMachineOnceGone()
    {
        var holo = new HoloMotion();
        holo.Hold(7, 0.4);
        Assert.Equal((7, 0.4, true), (holo.Machine, holo.Progress, holo.Opening));
        holo.Hold(null, 0.2);
        Assert.Equal((7, 0.2, false), (holo.Machine, holo.Progress, holo.Opening));
        holo.Hold(null, 0);
        Assert.Null(holo.Machine);
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
