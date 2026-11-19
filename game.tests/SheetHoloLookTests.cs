using System;
using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The drone draws the planner sheet as a band of light round the hero's eyes (drone-holo, «Дуга»).</summary>
public class SheetHoloLookTests
{
    [Fact]
    public void TheBandCurvesRoundTheEyesThroughTheSheetsMiddle()
    {
        var (radius, toward, half) = SheetHoloLook.Arc((0, 0), (12, 0));
        Assert.Equal(12, radius, 9);
        Assert.Equal(0, toward, 9);
        Assert.Equal(SheetHoloLook.HalfLength / 12, half, 9);
        Assert.Equal(Math.PI / 2, SheetHoloLook.Arc((0, 0), (0, 5 + SheetHoloLook.HalfLength)).Toward, 9);
    }

    [Fact]
    public void ASheetHeldCloseStillGetsABandNoWiderThanAHalfCircle()
    {
        var (radius, _, half) = SheetHoloLook.Arc((0, 0), (1, 0));
        Assert.Equal(SheetHoloLook.HalfLength, radius, 9);
        Assert.Equal(1, half, 9);
    }

    [Fact]
    public void ItComesOnAndGoesOutInItsTimes()
    {
        Assert.Equal(0.5, SheetHoloLook.Step(0, true, SheetHoloLook.OnMs / 2), 9);
        Assert.Equal(1, SheetHoloLook.Step(0.9, true, SheetHoloLook.OnMs));
        Assert.Equal(0.5, SheetHoloLook.Step(1, false, SheetHoloLook.OffMs / 2), 9);
        Assert.Equal(0, SheetHoloLook.Step(0.1, false, SheetHoloLook.OffMs));
    }

    [Fact]
    public void ComingOnItFlickersThenHoldsWhole()
    {
        Assert.Equal(0.85, SheetHoloLook.Brightness(0.1, coming: true));
        Assert.Equal(0.15, SheetHoloLook.Brightness(0.3, coming: true));
        Assert.Equal(1, SheetHoloLook.Brightness(0.5, coming: true));
        Assert.Equal(0.4, SheetHoloLook.Brightness(0.8, coming: true));
        Assert.Equal(1, SheetHoloLook.Brightness(1, coming: true));
        Assert.Equal(1, SheetHoloLook.Thickness(0.3, coming: true));
    }

    [Fact]
    public void GoingOutItFadesAndSquashesFlat()
    {
        Assert.Equal(0.5, SheetHoloLook.Brightness(0.5, coming: false));
        Assert.Equal(1, SheetHoloLook.Thickness(1, coming: false), 9);
        Assert.Equal(SheetHoloLook.FlatAtLast, SheetHoloLook.Thickness(0, coming: false), 9);
    }

    [Fact]
    public void ItsFaintFlickerStaysInRange()
    {
        for (int ms = 0; ms < 5000; ms += 7)
        {
            double f = SheetHoloLook.Flicker(ms, 3.1);
            Assert.InRange(f, 0, 1);
        }
    }
}
