using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Building;
using Stillwake.Sim.Power;
using Xunit;
using Pairs = System.Collections.Generic.IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>;

namespace Stillwake.Sim.Tests;

public sealed record RestMachine(int Id, bool Resting) : Command;

/// <summary>Tells power to rest a machine or work it again, as a module may with what stands idle.</summary>
public sealed class Rester : Module<Rester.Nothing>
{
    public sealed class Nothing;
    public override string Id => "rester";

    public Rester(IPowerOps power) => Handles<RestMachine>(_ => null, c => power.Rest(c.Id, c.Resting));

    protected override Nothing Init(Layout layout) => new();
}

/// <summary>packages/sim/src/modules/power/power.test.ts, ported; and the rules research and maps lean on.</summary>
public class PowerTests
{
    /// <summary>
    /// Sixteen wide: a solar panel out in space above a sealed room, its generator piped to a vent. Two
    /// rows of space between; the room's right wall is past a span from the generator. Drawn a character a 16 px cell, doubled (0104).
    /// </summary>
    private static readonly string[] Rows =
    [
        "PPP             ",
        "                ",
        "################",
        "#GG.GG.........#",
        "#GGVGG.........#",
        "################",
    ];
    private static readonly string[] Under = ["", "", "", "", " ---"];
    private const int Width = 2 * 16;
    /// <summary>The cell of the map's character at (<paramref name="x"/>, <paramref name="y"/>): its block's top-left.</summary>
    private static int At(int x, int y) => 2 * y * Width + 2 * x;
    /// <summary>The four cells of the map's character at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private static int[] Block(int x, int y) => [At(x, y), At(x, y) + 1, At(x, y) + Width, At(x, y) + Width + 1];
    private static readonly int Panel = At(0, 0), Generator = At(1, 3), Second = At(4, 3);
    private static readonly Pairs ToFirst = [((0, 0), (1, 3))];
    private static readonly int CableWire = Content.Power.CableCost.Single(c => c.Item == "wire").Count;

    private static Layout Map(Pairs? cables = null, string[]? rows = null) =>
        Layout.Parse(rows ?? Rows, Under, cables ?? ToFirst);

    private static Scenario World(Pairs? cables = null, string[]? rows = null) => new(Map(cables, rows), doubled: true);

    private static int IdAt(Scenario w, int cell) => w.M.Grid.MachineAt(cell)?.Id ?? 0;
    private static int Wire(Scenario w, int peer = 0) =>
        w.M.Items.Inventory(peer).Sum(s => s?.Item == "wire" ? s.Count : 0);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static (int, int, bool)[] CablesOf(Scenario w) => w.M.Power.Cables.Select(c => (c.A, c.B, c.Paid)).ToArray();
    private static (int, int[], int, int, int) Summary(PowerNetwork n) => (n.Id, n.Nodes.ToArray(), n.Supply, n.Demand, n.Satisfaction);
    private static Scenario Lay(Scenario w, int a, int b, int peer = 0) => w.Do(new Lay(a, b) { From = peer });

    [Fact]
    public void JoinsTheCablesAMapLaysItsPanelOutInSpaceFeedingTheGeneratorInFull()
    {
        var w = World();
        var q = w.M.Power;
        Assert.Equal(new[] { (Panel, Generator, true) }, CablesOf(w));
        var network = Assert.Single(q.Networks);
        Assert.Equal(Panel, network.Id);
        Assert.Equal(new[] { Panel, Generator }, network.Nodes);
        Assert.Equal((5, 4, 1000), (network.Supply, network.Demand, network.Satisfaction));
        Assert.Equal(1000, q.Satisfaction(IdAt(w, Generator)));
        // The second generator, cabled to nothing, gets nothing.
        Assert.Equal(0, q.Satisfaction(IdAt(w, Second)));
    }

    [Fact]
    public void LaysACableFromAnyCellOfAMachineToAnAnchorOnAWallPaidByTheSender()
    {
        var w = World().GiveItems(0, ("wire", 10));
        Lay(w, At(2, 4), At(0, 3)).Run();
        Assert.Empty(w.Refusals);
        Assert.Contains((At(0, 3), Generator, true), CablesOf(w));
        Assert.Equal(10 - CableWire, Wire(w));
        Assert.Equal(new[] { Panel, At(0, 3), Generator }, w.M.Power.Networks[0].Nodes);
    }

    [Fact]
    public void RefusesACableToNoNodeToTheSameNodePastASpanTwiceOrUnpaid()
    {
        var w = World().GiveItems(0, ("wire", 10));
        Lay(w, Generator, At(8, 1)).Run(); // open space, nothing to mount on
        Lay(w, Generator, At(3, 4)).Run(); // a vent draws no power
        Lay(w, At(1, 3), At(2, 4)).Run();
        Lay(w, Generator, At(15, 3)).Run();
        Lay(w, Generator, Panel).Run();
        Lay(w, Generator, Second, peer: 1).Run();
        Assert.Equal(
            new[] { "power.noNode", "power.noNode", "power.sameNode", "power.tooLong", "power.twice", "items.notEnough" },
            Reasons(w));
        Assert.Single(w.M.Power.Cables);
        Assert.True(At(15, 3) - At(2, 3) > Content.Power.MaxSpan);
    }

    [Fact]
    public void CutsACableGivingBackWhatWasPaidTheGeneratorCutOffGetsNothing()
    {
        var w = World();
        w.Do(new Cut(Generator, Panel)).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Power.Cables);
        Assert.Equal(CableWire, Wire(w));
        Assert.Equal(0, w.M.Power.Satisfaction(IdAt(w, Generator)));
        w.Do(new Cut(Generator, Panel)).Run();
        Assert.Equal(new[] { "power.noCable" }, Reasons(w));
    }

    [Fact]
    public void RefusesACutWhoseRefundDoesNotFitAndGivesNothingBackForOneLaidForFree()
    {
        var w = World().GiveItems(1, ("wire", Content.Inventory.Slots * Content.Items.Get("wire").Stack));
        w.Do(new Cut(Panel, Generator) { From = 1 }).Run();
        Assert.Equal(new[] { "items.full" }, Reasons(w));
        w.Do(new Items.SetCreative(true)).Run();
        Lay(w, Generator, At(0, 3), peer: 2).Run();
        Assert.Contains((At(0, 3), Generator, false), CablesOf(w));
        w.Do(new Cut(Generator, At(0, 3)) { From = 2 }).Run();
        Assert.Equal(0, Wire(w, 2));
    }

    [Fact]
    public void MakesNothingFromAPanelInsideARoom()
    {
        string[] inside =
        [
            "                ",
            "################",
            "#PPP...........#",
            "#GG............#",
            "#GG............#",
            "################",
        ];
        var w = World([((1, 2), (1, 3))], inside);
        var n = w.M.Power.Networks[0];
        Assert.Equal((0, 4, 0), (n.Supply, n.Demand, n.Satisfaction));
    }

    [Fact]
    public void ShadesAPanelInSpaceOnceABackWallStandsBehindItAndLightsItWhenItGoes()
    {
        var w = World();
        Assert.Null(w.M.Grid.BackAt(Panel));
        Assert.Equal(5, w.M.Power.Networks[0].Supply);
        w.Do(new Place(At(1, 0), Grid.Placement.BackWall())).Run();
        Assert.Equal(0, w.M.Power.Networks[0].Supply);
        w.Do(new ClearAt(At(1, 0), "back")).Run();
        Assert.Equal(5, w.M.Power.Networks[0].Supply);
    }

    [Fact]
    public void MakesNothingFromAPanelInARoomTornOpenToSpaceWhileItsBackWallStands()
    {
        // The left wall gone two cells high: the room is space to the air, its back walls still up.
        string[] torn =
        [
            "                ",
            "################",
            "  PPP..........#",
            "  GG...........#",
            "  GG...........#",
            "################",
        ];
        var w = World([((2, 2), (2, 3))], torn);
        Assert.Equal(Rooms.RoomsModule.Space, w.M.Rooms.RoomAt(At(5, 2)));
        Assert.Equal(5, w.M.Power.Networks[0].Supply);
        foreach (int x in new[] { 2, 3, 4 })
            foreach (int cell in Block(x, 2)) w.Do(new Place(cell, Grid.Placement.BackWall()));
        w.Run();
        Assert.Equal(0, w.M.Power.Networks[0].Supply);
    }

    private static int Store(Scenario w, int cell) => w.M.Atmosphere.OxygenIn(IdAt(w, cell));

    [Fact]
    public void SlowsEveryUserAlikeWhenShortTwoGeneratorsOnOnePanelGet625()
    {
        var w = World([.. ToFirst, ((0, 0), (4, 3))]);
        var n = w.M.Power.Networks[0];
        Assert.Equal((5, 8, 625), (n.Supply, n.Demand, n.Satisfaction));
        Assert.Equal(625, w.M.Power.Satisfaction(IdAt(w, Second)));
        // Unpiped, the second keeps all it makes: 750 a minute, a hundred and twenty units in 192 ticks, four times what a
        // 16 px cell's generator made (0104).
        const int ticks = 192;
        w.Run(ticks);
        Assert.Equal(ticks * Content.Air.GeneratorRate * 625 / 1000 / Content.Air.LeakEvery, Store(w, Second));
        Assert.Equal(120, Store(w, Second));
    }

    [Fact]
    public void RunsAGeneratorAtFullPowerAtItsRateItsFractionCarried()
    {
        var w = new Scenario(Layout.Parse(Rows, [], ToFirst), doubled: true);
        w.Run(120);
        // Twelve hundred a minute, four times a 16 px cell's (0104): one a tick.
        Assert.Equal(120, Store(w, Generator));
    }

    /// <summary>Works every plan to its end, a tick of E at a time.</summary>
    private static void WorkAll(Scenario w)
    {
        for (int i = 0; i < 400 && w.M.Building.Plans.Count > 0; i++) w.Do(new Work(w.M.Building.Plans[0].Id)).Run();
        Assert.Empty(w.M.Building.Plans);
    }

    [Fact]
    public void AWiredMachineComesApartItsCableLeftOnAPegWhereItsPlugWasAndBuiltAgainThereItPlugsBackIn()
    {
        var w = World();
        // The generator's plug, 16 px across its picture at 8 a cell: two cells right of its top-left.
        int peg = Generator + 2;
        w.Do(new Deconstruct(Generator, "machine")).Run();
        WorkAll(w);
        Assert.Empty(w.Refusals);
        Assert.Null(w.M.Grid.MachineAt(Generator));
        Assert.Equal([(Panel, peg, true)], CablesOf(w));
        Assert.Equal([Panel, peg], w.M.Power.Networks.Single().Nodes);
        // Saved and loaded, the peg stays.
        w = w.Reload();
        Assert.Equal([(Panel, peg, true)], CablesOf(w));

        // Built again with its plug on the peg, the cable plugs in and the panel feeds it.
        w.GiveItems(0, ("frame", 1), ("rotor", 1), ("tube", 1), ("wire", 2)).Do(new PlacePlan(Generator, new Grid.Placement("machine", "generator"))).Run();
        WorkAll(w);
        Assert.Empty(w.Refusals);
        Assert.Equal([(Panel, Generator, true)], CablesOf(w));
        Assert.Equal([Panel, Generator], w.M.Power.Networks.Single().Nodes);
        Assert.Equal(1000, w.M.Power.Satisfaction(IdAt(w, Generator)));
    }

    [Fact]
    public void RefusesCablesAwayFromTheShip()
    {
        var w = new Scenario(Layout.Parse(["........", ".######.", ".#.S..#.", ".######."]));
        w.Do(new Crew.Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Content.PlanetTypes.Small.Id)).Run();
        w.GiveItems(0, ("wire", 4)).Do(new Transfer(1)).Run();
        var m = w.On(1);
        int ground = m.Crew.Spawn + m.Grid.Width;
        w.Do(new Lay(ground, ground + 2)).Do(new Move(ground, ground + 3)).Run();
        Assert.Equal(["power.notAboard", "power.notAboard"], Reasons(w));
        Assert.Empty(m.Power.Cables);
    }

    private sealed record Step(bool Lay, int A, int B, int Peer);

    /// <summary>
    /// Cells of the map; a third from a few by the machines and the far wall, so pairs repeat as
    /// fast-check's small values make them; one in six in the top rows, the panel's and open space.
    /// </summary>
    private static readonly Gen<int> Cells = Gen.Frequency(
        (3, Gen.Choose(0, Width * 12 - 1)),
        (2, Gen.Elements(At(0, 0), At(2, 0), At(1, 3), At(2, 4), At(4, 3), At(0, 3), At(8, 3), At(15, 4), At(13, 5))),
        (1, Gen.Choose(0, Width * 4 - 1)));

    [Property(MaxTest = 100)]
    public Property KeepsCablesBetweenDistinctNodesInSpanOnceEachAndSatisfactionWhatSupplyAllows()
    {
        var step = from lay in Gen.Elements(true, false)
                   from a in Cells
                   from b in Cells
                   from peer in Gen.Choose(0, 1)
                   select new Step(lay, a, b, peer);
        var steps = from n in Gen.Choose(0, 40) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = World().GiveItems(0, ("wire", 40));
            foreach (var s in all)
            {
                w.Do(s.Lay ? new Lay(s.A, s.B) { From = s.Peer } : new Cut(s.A, s.B) { From = s.Peer });
                // Now and then a wall goes, an anchor's or the panel's shade.
                if (s.A % 7 == 0) w.Do(new ClearAt(s.B, "structure"));
                w.Run();
            }
            var cables = w.M.Power.Cables;
            if (cables.Select(c => (c.A, c.B)).Distinct().Count() != cables.Count) return false;
            if (cables.Any(c => c.A >= c.B)) return false;
            foreach (var n in w.M.Power.Networks)
            {
                if (n.Satisfaction < 0 || n.Satisfaction > 1000) return false;
                if (n.Demand * n.Satisfaction / 1000 > n.Supply) return false;
            }
            return true;
        });
    }

    // Moving cables.

    /// <summary>In creative mode, cables laid from <paramref name="from"/> to each of <paramref name="to"/>.</summary>
    private static Scenario Laid(int from, params int[] to)
    {
        var w = World([]).Do(new Items.SetCreative(true));
        foreach (int t in to) Lay(w, from, t);
        return w.Run();
    }

    private static readonly int S = At(8, 3);

    [Fact]
    public void MovesEveryCableEndingAtASupportToAnother()
    {
        var w = Laid(S, At(5, 5), At(11, 5)).Do(new Move(S, At(8, 5))).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(new[] { (At(5, 5), At(8, 5), false), (At(8, 5), At(11, 5), false) }, CablesOf(w));
    }

    [Fact]
    public void MovesOneCablesEndAloneFromAMachineToASupport()
    {
        var w = Laid(Generator, S, At(12, 3)).Do(new Move(Generator, At(4, 3), new Ends(Generator, S))).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(new[] { (Generator, At(12, 3), false), (At(4, 3), S, false) }, CablesOf(w));
    }

    [Fact]
    public void RefusesAMoveFromWhereNoCableEndsToNoNodeOntoItsOtherEndTwiceOrTaut()
    {
        var w = Laid(S, At(5, 5), At(11, 5))
            .Do(new Move(At(9, 4), At(10, 4)))
            .Do(new Move(S, At(10, 0)))
            .Do(new Move(S, At(11, 5), new Ends(S, At(11, 5))))
            .Do(new Move(At(5, 5), At(11, 5), new Ends(S, At(5, 5))))
            .Do(new Move(S, At(15, 3), new Ends(S, At(5, 5))))
            .Run();
        Assert.Equal(
            new[] { "power.noCable", "power.noNode", "power.sameNode", "power.twice", "power.taut" },
            Reasons(w));
        Assert.Equal(CablesOf(Laid(S, At(5, 5), At(11, 5))), CablesOf(w));
    }

    [Property(MaxTest = 100)]
    public Property NeverLeavesACableSpanningFartherThanACableReaches()
    {
        var step = from kind in Gen.Choose(0, 2) from a in Cells from b in Cells select (kind, a, b);
        var steps = from n in Gen.Choose(0, 40) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = World([]).Do(new Items.SetCreative(true));
            foreach (var (kind, a, b) in all)
            {
                w.Do(kind switch { 0 => new Lay(a, b), 1 => new Move(a, b), _ => new Cut(a, b) });
                w.Run();
            }
            return w.M.Power.Cables.All(c => PowerNodes.InSpan(w.M.Grid, c.A, c.B));
        });
    }

    // Beyond power.test.ts: resting and the map's cables.

    private static Scenario Resting(Pairs? cables = null) => new(Map(cables), with: m => [new Rester(m.Power)], doubled: true);
    private static IReadOnlyList<int> RestingIds(Scenario w) => ((PowerState)w.M.Power.Slice).Resting;

    [Fact]
    public void AMachineToldToRestDrawsNothingUntilToldToWorkAgainAndStaysRestingThroughALoad()
    {
        var w = Resting([.. ToFirst, ((0, 0), (4, 3))]);
        int second = IdAt(w, Second);
        w.Do(new RestMachine(second, true)).Do(new RestMachine(second, true)).Run();
        Assert.Equal((5, 4, 1000), (w.M.Power.Networks[0].Supply, w.M.Power.Networks[0].Demand, w.M.Power.Networks[0].Satisfaction));
        // Resting, it is still on the network: it gets the network's share, drawing on it or not.
        Assert.Equal(1000, w.M.Power.Satisfaction(second));
        Assert.Equal(new[] { second }, RestingIds(w));
        w = w.Reload();
        Assert.Equal(4, w.M.Power.Networks[0].Demand);
        w.Do(new RestMachine(second, false)).Do(new RestMachine(second, false)).Run();
        Assert.Equal((8, 625), (w.M.Power.Networks[0].Demand, w.M.Power.Networks[0].Satisfaction));
        Assert.Empty(RestingIds(w));
    }

    [Fact]
    public void KeepsTheRestingSortedAndForgetsAMachineTakenApart()
    {
        var w = Resting([.. ToFirst, ((0, 0), (4, 3))]);
        int gen = IdAt(w, Generator), second = IdAt(w, Second);
        w.Do(new RestMachine(second, true)).Do(new RestMachine(gen, true)).Run();
        Assert.Equal(new[] { gen, second }, RestingIds(w));
        Assert.Equal(0, w.M.Power.Networks[0].Demand);
        w.Do(new Cut(Panel, Second)).Run().Do(new ClearAt(Second, "machine")).Run();
        Assert.Equal(new[] { gen }, RestingIds(w));
    }

    [Fact]
    public void AMapLaysItsCablesFromAnyCellOfAMachineAndRefusesOneEndingOnNoNode()
    {
        var w = World([((2, 4), (0, 0)), ((1, 0), (8, 3))]);
        Assert.Equal(new[] { (Panel, Generator, true), (Panel, S, true) }, CablesOf(w));
        Assert.Throws<FormatException>(() => World([((0, 0), (8, 1))]));  // open space
        Assert.Throws<FormatException>(() => World([((0, 0), (3, 4))]));  // a vent draws no power
        Assert.Throws<FormatException>(() => World([((0, 0), (0, 6))]));  // off the map
    }

    [Fact]
    public void TheWreckStartsWithItsPanelOnNoCable()
    {
        var w = new Scenario(Registry.Wreck);
        Assert.All(w.M.Power.Networks, n => Assert.Equal(0, n.Demand));
    }
}
