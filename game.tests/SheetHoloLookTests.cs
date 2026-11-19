using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The drone's projection before the hero flickers on and goes out (drone-holo).</summary>
public class SheetHoloLookTests
{
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
    }

    [Fact]
    public void GoingOutItFades() => Assert.Equal(0.5, SheetHoloLook.Brightness(0.5, coming: false));

    [Fact]
    public void ItsFaintFlickerStaysInRange()
    {
        for (int ms = 0; ms < 5000; ms += 7) Assert.InRange(SheetHoloLook.Flicker(ms, 3.1), 0, 1);
    }
}
