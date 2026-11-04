using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>What stands in for the pointer's arrow (pointer-looks): who hides it, and a builder's frame.</summary>
public class PointerLooksTests
{
    [Fact]
    public void TheArrowIsHiddenWhileAnythingStandsInForItAndNeverUnderTheMenu()
    {
        var arrow = new PointerArrow();
        Assert.False(arrow.Hidden(menuOpen: false));
        arrow.StandIn("items", true);
        arrow.StandIn("crew", true);
        Assert.True(arrow.Hidden(menuOpen: false));
        Assert.False(arrow.Hidden(menuOpen: true));
        // One letting go leaves the other standing in: none shows the arrow under another.
        arrow.StandIn("items", false);
        Assert.True(arrow.Hidden(menuOpen: false));
        arrow.StandIn("crew", false);
        arrow.StandIn("crew", false);
        Assert.False(arrow.Hidden(menuOpen: false));
    }

    [Fact]
    public void ABuildersFrameIsWhatTheNextPressWorksAndKeepsTheModeInSight()
    {
        var driver = Spec.Screwdrivers.PowerDriver;
        // Charged in its mode: the square round the nearest corner, as the square it works.
        Assert.Equal(((4, 3, 2), ((int, int, int)?)null), Aiming.FrameAt(16, 10, (5.4, 3.6), driver, square: true, charged: true, whole: false));
        // One cell, switched or the screwdriver: the cell under the point.
        Assert.Equal(((5, 3, 1), ((int, int, int)?)null), Aiming.FrameAt(16, 10, (5.4, 3.6), driver, square: false, charged: true, whole: false));
        Assert.Equal(((5, 3, 1), ((int, int, int)?)null), Aiming.FrameAt(16, 10, (5.4, 3.6), Spec.Screwdrivers.Basic, square: true, charged: true, whole: false));
        // The square wanted with no charge: the cell it works, the square dotted round it.
        Assert.Equal(((5, 3, 1), (4, 3, 2)), Aiming.FrameAt(16, 10, (5.4, 3.6), driver, square: true, charged: false, whole: false));
        // On a station, worked whole at one cell: the cell.
        Assert.Equal(((5, 3, 1), ((int, int, int)?)null), Aiming.FrameAt(16, 10, (5.4, 3.6), driver, square: true, charged: true, whole: true));
        // Off the grid, none.
        Assert.Null(Aiming.FrameAt(16, 10, (-1, 3), driver, square: true, charged: true, whole: false));
    }
}
