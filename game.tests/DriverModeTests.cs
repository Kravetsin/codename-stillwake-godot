using Stillwake.Game.Features.Crew;
using Xunit;
using static Stillwake.Game.Features.Crew.ModeDot;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The mark by the pointer that says the power driver's mode (mode-glyph).</summary>
public class DriverModeTests
{
    [Fact]
    public void ItsDotsSayTheSquareOneCellOrTheSquareWaitingForChargeAndNothingForTheScrewdriver()
    {
        var driver = Spec.Screwdrivers.PowerDriver;
        Assert.Equal([Lit, Lit, Lit, Lit], DriverMode.DotsOf(driver, square: true, charged: true));
        Assert.Equal([Lit, Off, Off, Off], DriverMode.DotsOf(driver, square: false, charged: true));
        Assert.Equal([Lit, Off, Off, Off], DriverMode.DotsOf(driver, square: false, charged: false));
        Assert.Equal([Lit, Waiting, Waiting, Waiting], DriverMode.DotsOf(driver, square: true, charged: false));
        // A builder of one cell has no mode to say; nor an empty hand.
        Assert.Null(DriverMode.DotsOf(Spec.Screwdrivers.Basic, square: true, charged: true));
        Assert.Null(DriverMode.DotsOf(null, square: true, charged: true));
    }
}
