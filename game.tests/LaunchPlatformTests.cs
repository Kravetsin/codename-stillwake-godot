using Stillwake.Content;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Features.Power;
using Stillwake.Sim;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The launch platform in the client: its card, its runs and their lamps.</summary>
public class LaunchPlatformTests
{
    private static readonly Tool Card = Tools.All.Single(t => t is PlaceTool { Placement.Kind: Builds.Platform });

    [Fact]
    public void PutsThePlatformsCardOnTheShipTab() => Assert.Contains(Card, Tools.Tabs.Single(t => t.Id == MenuTabs.Ship).Tools);

    /// <summary>A panel in space above a room: a run of two pieces cabled to the panel, a piece alone uncabled.</summary>
    private static readonly string[] Rows =
    [
        "PPP             ",
        "                ",
        "################",
        "#..............#",
        "#..__....._....#",
        "################",
    ];

    [Fact]
    public void FindsEachRunAndLightsOnlyTheOneOnANetworkWithPower()
    {
        var w = new TestWorld(Layout.Parse(Rows, [], [((0, 0), (3, 4))]).Doubled());
        int width = w.M.Grid.Width;
        int Char(int x, int y) => 2 * y * width + 2 * x;
        var runs = PlatformRuns.Of(w.M.Grid);
        Assert.Equal([(Char(3, 4), 4), (Char(10, 4), 2)], runs);
        Assert.Equal([true, false], runs.Select(r => PlatformRuns.Lit(w.M.Power, r)));
    }
}
