using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Power;
using Xunit;
using Pairs = System.Collections.Generic.IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>;

namespace Stillwake.Sim.Tests;

public sealed record RestMachine(int Id, bool Resting) : Command;

/// <summary>Tells power to rest a machine or work it again, as research does with an idle analyzer.</summary>
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
    /// rows of space between; the room's right wall is past a span from the generator.
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
    private static int At(int x, int y) => y * 16 + x;
    private static readonly int Panel = At(0, 0), Generator = At(1, 3), Second = At(4, 3);
    private static readonly Pairs ToFirst = [((0, 0), (1, 3))];
    private static readonly int CableScrap = Content.Power.CableCost.Single(c => c.Item == "scrap").Count;

    private static Layout Map(Pairs? cables = null, string[]? rows = null) =>
        Layout.Parse(rows ?? Rows, Under, cables ?? ToFirst);

    private static Scenario World(Pairs? cables = null, string[]? rows = null) => new(Map(cables, rows));

    private static int IdAt(Scenario w, int cell) => w.M.Grid.MachineAt(cell)?.Id ?? 0;
    private static int Scrap(Scenario w, int peer = 0) =>
        w.M.Items.Inventory(peer).Sum(s => s?.Item == "scrap" ? s.Count : 0);
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
        var w = World().GiveItems(0, ("scrap", 10));
        Lay(w, At(2, 4), At(0, 3)).Run();
        Assert.Empty(w.Refusals);
        Assert.Contains((At(0, 3), Generator, true), CablesOf(w));
        Assert.Equal(10 - CableScrap, Scrap(w));
        Assert.Equal(new[] { Panel, At(0, 3), Generator }, w.M.Power.Networks[0].Nodes);
    }

    [Fact]
    public void RefusesACableToNoNodeToTheSameNodePastASpanTwiceOrUnpaid()
    {
        var w = World().GiveItems(0, ("scrap", 10));
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
        Assert.Equal(CableScrap, Scrap(w));
        Assert.Equal(0, w.M.Power.Satisfaction(IdAt(w, Generator)));
        w.Do(new Cut(Generator, Panel)).Run();
        Assert.Equal(new[] { "power.noCable" }, Reasons(w));
    }

    [Fact]
    public void RefusesACutWhoseRefundDoesNotFitAndGivesNothingBackForOneLaidForFree()
    {
        var w = World().GiveItems(1, ("scrap", Content.Inventory.Slots * Content.Items.Get("scrap").Stack));
        w.Do(new Cut(Panel, Generator) { From = 1 }).Run();
        Assert.Equal(new[] { "items.full" }, Reasons(w));
        w.Do(new Items.SetCreative(true)).Run();
        Lay(w, Generator, At(0, 3), peer: 2).Run();
        Assert.Contains((At(0, 3), Generator, false), CablesOf(w));
        w.Do(new Cut(Generator, At(0, 3)) { From = 2 }).Run();
        Assert.Equal(0, Scrap(w, 2));
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

    // TypeScript goes on to run the generators (atmosphere): waiting for atmosphere, as is "runs a
    // generator at full power at its rate"; "keeps a cabled machine from being taken apart" waits for building.
    [Fact]
    public void SlowsEveryUserAlikeWhenShortTwoGeneratorsOnOnePanelGet625()
    {
        var w = World([.. ToFirst, ((0, 0), (4, 3))]);
        var n = w.M.Power.Networks[0];
        Assert.Equal((5, 8, 625), (n.Supply, n.Demand, n.Satisfaction));
        Assert.Equal(625, w.M.Power.Satisfaction(IdAt(w, Second)));
    }

    private sealed record Step(bool Lay, int A, int B, int Peer);

    /// <summary>
    /// Cells of the map; a third from a few by the machines and the far wall, so pairs repeat as
    /// fast-check's small values make them; one in six in the top rows, the panel's and open space.
    /// </summary>
    private static readonly Gen<int> Cells = Gen.Frequency(
        (3, Gen.Choose(0, 16 * 6 - 1)),
        (2, Gen.Elements(At(0, 0), At(2, 0), At(1, 3), At(2, 4), At(4, 3), At(0, 3), At(8, 3), At(15, 4), At(13, 5))),
        (1, Gen.Choose(0, 16 * 2 - 1)));

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
            var w = World().GiveItems(0, ("scrap", 40));
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

    // Beyond power.test.ts: resting (research's analyzer), drawing on a slot, and the map's cables.

    private static Scenario Resting(Pairs? cables = null) => new(Map(cables), with: m => [new Rester(m.Power)]);
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
        w.Reload();
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
    public void AnAnalyzerDrawsOnlyWhileItsFirstSlotHoldsSomething()
    {
        string[] rows = [.. Rows[..3], "#AA............#", "#AA............#", Rows[5]];
        var w = new Scenario(Layout.Parse(rows, null, ToFirst));
        int analyzer = IdAt(w, Generator);
        Assert.Equal((0, 1000), (w.M.Power.Networks[0].Demand, w.M.Power.Satisfaction(analyzer)));
        w.FillCrate(analyzer, ("scrap", 1)).Run();
        Assert.Equal(Content.Machines.Get("analyzer").Draws!.Value, w.M.Power.Networks[0].Demand);
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
    public void TheWreckStartsWithItsPanelCabledToItsGeneratorAndLamp()
    {
        var w = new Scenario(Registry.Wreck);
        int width = w.Layout.Width;
        var n = Assert.Single(w.M.Power.Networks);
        Assert.Equal(new[] { 17 * width + 50, 19 * width + 50, 21 * width + 46 }, n.Nodes);
        Assert.Equal((5, 5, 1000), (n.Supply, n.Demand, n.Satisfaction));
    }
}
