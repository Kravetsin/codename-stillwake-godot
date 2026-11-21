using Stillwake.Content;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Xunit;
using Hero = Stillwake.Game.Features.Crew.Hero;

namespace Stillwake.Game.Tests;

/// <summary>The alien core is touched from almost against it, not from an arm's reach across the room (drone-meeting).</summary>
public class CoreTouchTests
{
    /// <summary>A room, the core lying on its floor: cells (6..7, 6..7), the floor's top at row 8.</summary>
    private static readonly string[] Rows =
    [
        "##########",
        "#........#",
        "#........#",
        "#..?.....#",
        "##########",
    ];

    private static Aim? AimFrom(int heroX)
    {
        var w = new TestWorld(Layout.Parse(Rows).Doubled());
        return Aiming.AimAt(w.M, 0, new Hero(heroX, 8 * Hero.Unit, 0, 0, false, -1), (6.5, 6.5));
    }

    [Fact]
    public void ItIsTouchedStandingAgainstIt()
    {
        // The body's left side on the core's right edge, then a cell off it.
        Assert.True(AimFrom(8 * Hero.Unit + Content.Hero.Width / 2)?.InReach);
        Assert.True(AimFrom(9 * Hero.Unit + Content.Hero.Width / 2)?.InReach);
    }

    [Fact]
    public void ItIsNotTouchedFromAFewCellsAwayThoughTheHandsReachThere()
    {
        var aim = AimFrom(13 * Hero.Unit);
        Assert.NotNull(aim?.Press);
        Assert.False(aim.InReach);
    }
}
