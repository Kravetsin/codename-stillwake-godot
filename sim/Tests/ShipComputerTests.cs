using Stillwake.Content;
using Stillwake.Sim.Grid;
using Xunit;
using Pairs = System.Collections.Generic.IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>;

namespace Stillwake.Sim.Tests;

/// <summary>The ship's computer and the radar antenna: made with a planet's copper and magnetite, opened by research, drawing power once built.</summary>
public class ShipComputerTests
{
    /// <summary>A panel and the antenna out on the roof, the computer in the room under them, both cabled to the panel.</summary>
    private static readonly string[] Rows =
    [
        "    NN      ",
        "PPP NN      ",
        "    NN      ",
        "############",
        "#UUU.......#",
        "#UUU.......#",
        "############",
    ];
    private static readonly (int X, int Y) Panel = (0, 1), Antenna = (4, 2), Computer = (1, 4);

    private static readonly Pairs Cables = [(Panel, Antenna), (Panel, Computer)];

    private static Scenario World() =>
        new(Layout.Parse(Rows, null, Cables), doubled: true);

    private static int IdAt(Scenario w, (int X, int Y) cell) => w.M.Grid.MachineAt(w.At(cell.X, cell.Y))!.Id;

    /// <summary>What a part is made of down to what is never crafted: wrecks' and asteroids' scrap, iron and rock, smelted.</summary>
    private static IEnumerable<string> Raw(string item) =>
        Crafts.All.FirstOrDefault(c => c.Item == item) is { } craft ? craft.Cost.SelectMany(c => Raw(c.Item)) : [item];

    [Theory]
    [InlineData("computer")]
    [InlineData("radarAntenna")]
    public void EachTakesAPlanetsCopperAndMagnetiteSoItIsBuiltAfterTheFirstLanding(string kind)
    {
        var raw = Machines.Get(kind).Cost.Select(c => c.Item).SelectMany(Raw).Distinct().Order(StringComparer.Ordinal);
        // Iron and scrap from hulls, silicon of asteroids' rock and glass of silicon, and a planet's copper and magnetite: no mica or shard.
        Assert.Equal(["copper", "glass", "iron", "magnetite", "scrap", "silicon"], raw);
    }

    [Fact]
    public void TheirPartsAreOfWhatSpaceGivesForTheDropPodSharesThem()
    {
        Assert.Contains("processor", Machines.Get("computer").Cost.Select(c => c.Item));
        Assert.Contains("display", Machines.Get("computer").Cost.Select(c => c.Item));
        Assert.Contains("transceiver", Machines.Get("radarAntenna").Cost.Select(c => c.Item));
        Assert.All(["processor", "display", "transceiver"], part =>
        {
            Assert.Subset(new HashSet<string>(["glass", "iron", "scrap", "silicon"]), Raw(part).ToHashSet());
            Assert.Contains(part, Stations.All.Single(s => s.Machine == "assembler").Rows.SelectMany(r => r));
        });
    }

    [Fact]
    public void AreBoughtInTheTreeAsAnyBlueprintAndArePlannedOnTheShipTab()
    {
        var w = new Scenario(Layout.Parse(Rows, null, Cables) with { Known = Content.Research.StartKnown }, doubled: true);
        var computer = new Placement("machine", "computer");
        Assert.Equal("building.unknown", w.M.Building.Problem(w.At(6, 4), computer, 0));
        Assert.All(["computer", "radarAntenna"], kind => Assert.False(w.M.Research.Knows(kind)));
        Assert.All(["computer", "radarAntenna"], kind => Assert.Equal(MenuTabs.Ship, Machines.Get(kind).Menu!.Tab));
        // A world granted everything plans them at once, creative or not.
        Assert.Null(World().M.Building.Problem(w.At(6, 4), computer, 0));
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
