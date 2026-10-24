using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;
using Pairs = System.Collections.Generic.IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>;

namespace Stillwake.Sim.Tests;

/// <summary>The ship's computer and the radar antenna: a scrap each, planned in creative mode alone, drawing power once built.</summary>
public class ShipComputerTests
{
    /// <summary>A panel and the antenna out on the roof, the computer in the room under them, both cabled to the panel.</summary>
    private static readonly string[] Rows =
    [
        "PPP NN      ",
        "    NN      ",
        "############",
        "#UUU.......#",
        "#UUU.......#",
        "############",
    ];
    private static readonly (int X, int Y) Panel = (0, 0), Antenna = (4, 1), Computer = (1, 3);

    private static readonly Pairs Cables = [(Panel, Antenna), (Panel, Computer)];

    private static Scenario World() =>
        new(Layout.Parse(Rows, null, Cables), doubled: true);

    private static int IdAt(Scenario w, (int X, int Y) cell) => w.M.Grid.MachineAt(w.At(cell.X, cell.Y))!.Id;

    [Fact]
    public void CostAScrapEachAndAreTheOnlyBlueprintsKnownInCreativeModeAlone()
    {
        Assert.Equal(["computer", "radarAntenna"], Research.Blueprints.CreativeOnly);
        Assert.All(Research.Blueprints.CreativeOnly, kind => Assert.Equal(new[] { ("scrap", 1) }, Machines.Get(kind).Cost));
        Assert.All(Research.Blueprints.CreativeOnly, kind => Assert.Equal(MenuTabs.Ship, Machines.Get(kind).Menu!.Tab));
    }

    [Fact]
    public void ArePlannedInCreativeModeAloneThoughTheWorldWasGrantedEveryBlueprint()
    {
        var w = World();
        int free = w.At(6, 3);
        var computer = new Placement("machine", "computer");
        var antenna = new Placement("machine", "radarAntenna");
        // Everything else is granted on a map with no Layout.Known: a lamp may be planned there.
        Assert.Null(w.M.Building.Problem(free, new Placement("machine", "lamp")));
        Assert.Equal("building.unknown", w.M.Building.Problem(free, computer));
        Assert.Equal("building.unknown", w.M.Building.Problem(free, antenna));
        Assert.False(w.M.Research.Knows(computer));

        w.Do(new SetCreative(true)).Run();
        Assert.Null(w.M.Building.Problem(free, computer));
        Assert.Null(w.M.Building.Problem(w.At(6, 3), antenna));
        Assert.True(w.M.Research.Knows(antenna));

        w.Do(new SetCreative(false)).Run();
        Assert.Equal("building.unknown", w.M.Building.Problem(free, computer));
    }

    [Fact]
    public void DrawTheirPowerCabledToAPanelAndAreFullyServedByOne()
    {
        var w = World().Run(2);
        Assert.Equal(1000, w.M.Power.Satisfaction(IdAt(w, Computer)));
        Assert.Equal(1000, w.M.Power.Satisfaction(IdAt(w, Antenna)));
        var network = w.M.Power.NetworkOf(w.At(Computer.X, Computer.Y))!;
        Assert.Equal(Machines.Get("computer").Draws + Machines.Get("radarAntenna").Draws, network.Demand);
    }

    [Fact]
    public void DrawNothingWithNoCable()
    {
        var w = new Scenario(Layout.Parse(Rows), doubled: true).Run(2);
        Assert.Equal(0, w.M.Power.Satisfaction(IdAt(w, Computer)));
        Assert.Equal(0, w.M.Power.Satisfaction(IdAt(w, Antenna)));
    }
}
