using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using Craft = Stillwake.Sim.Crafting.Craft;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Modules (0086), slice 1: the arc module fitted into a machine by hand and taken out again, twice an electric station's
/// speed for twice its power; never into the furnace or the pod, never moved by drones, kept by a save.
/// </summary>
public class ArcModuleTests
{
    private const string Arc = "arcModule";

    /// <summary>
    /// A sealed room with an electric smelter, a Former, a circuit assembler, a furnace and a crate; four solar panels out
    /// under the stars, cabled in a line, each cabled down to a station: 20 of power, more than the three draw with an arc
    /// each (8, 6, 4).
    /// </summary>
    private static readonly string[] Rows =
    [
        "PPPPPPPPPPPP       ",
        "                   ",
        "###################",
        "#EE.MM.YY.FF.KK...#",
        "#EE.MM.YY.FF......#",
        "###################",
    ];

    private static readonly IReadOnlyList<((int X, int Y) A, (int X, int Y) B)> Cables =
    [
        ((0, 0), (3, 0)), ((3, 0), (6, 0)), ((6, 0), (9, 0)),
        ((0, 0), (1, 3)), ((3, 0), (4, 3)), ((6, 0), (7, 3)),
    ];

    private static Scenario World() => new Scenario(Layout.Parse(Rows, null, Cables)).GiveItems(0, (Arc, 3), ("iron", 40), ("rock", 4)).Run(2);

    private static int Id(Scenario w, string kind) => w.M.Grid.MachinesOf(kind).Single().Id;
    private static int SlotOf(Scenario w, string item, int peer = 0) => w.M.Items.Inventory(peer).ToList().FindIndex(s => s?.Item == item);
    private static int Carried(Scenario w, string item, int peer = 0) => w.M.Items.Inventory(peer).Sum(s => s?.Item == item ? s.Count : 0);
    private static int Held(Scenario w, int machine, string item) => w.M.Items.Crate(machine).Sum(s => s?.Item == item ? s.Count : 0);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int Demand(Scenario w) => w.M.Power.NetworkOf(0)!.Demand;

    private static Scenario FitArc(Scenario w, int machine, int peer = 0) => w.Do(new Fit(new Slot(SlotOf(w, Arc, peer)), machine) { From = peer }).Run();

    [Fact]
    public void FitsAModuleFromTheBagIntoAMachineAndTakesItOutIntoASlotOrWhereverItFits()
    {
        var w = World();
        int smelter = Id(w, "smelter");
        FitArc(w, smelter);
        Assert.Empty(w.Refusals);
        Assert.Equal([Arc], w.M.Items.FittedTo(smelter));
        Assert.Equal(2, Carried(w, Arc));
        // Out of every slot: the smelter's slots hold nothing of it.
        Assert.Equal(0, Held(w, smelter, Arc));
        Assert.Equal(new Boost(1000, 1000, 1000, 0), w.M.Items.Boost(smelter));
        Assert.Equal(default, w.M.Items.Boost(Id(w, "former")));

        w.Do(new Unfit(smelter, Arc, 20)).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Items.FittedTo(smelter));
        Assert.Equal(new Stack(Arc, 1), w.M.Items.Inventory(0)[20]);

        FitArc(w, smelter).Do(new Unfit(smelter, Arc)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(3, Carried(w, Arc));
    }

    [Fact]
    public void FitsOneIntoEachOfSeveralMachinesAndFromACrateKeptByMachineInIdOrder()
    {
        var w = World();
        int crate = Id(w, "crate");
        w.Do(new Move(new Slot(SlotOf(w, Arc)), new Target(4, crate), 1)).Run();
        int assembler = Id(w, "assembler"), smelter = Id(w, "smelter");
        // Into the later machine first: the list keeps them by machine all the same.
        w.Do(new Fit(new Slot(4, crate), assembler)).Run();
        FitArc(w, smelter);
        Assert.Empty(w.Refusals);
        Assert.Equal(0, Held(w, crate, Arc));
        Assert.Equal([Arc], w.M.Items.FittedTo(assembler));
        Assert.Equal([Arc], w.M.Items.FittedTo(smelter));
        Assert.Equal(1, Carried(w, Arc));
    }

    [Fact]
    public void RefusesEachFitAndUnfitForItsReasonLeavingEverythingAsItWas()
    {
        var w = World();
        int smelter = Id(w, "smelter"), former = Id(w, "former"), furnace = Id(w, "furnace"), crate = Id(w, "crate");
        int panel = w.M.Grid.MachinesOf("solarPanel")[0].Id;
        int arc = SlotOf(w, Arc), iron = SlotOf(w, "iron");
        int empty = w.M.Items.Inventory(0).ToList().FindIndex(s => s == null);
        FitArc(w, smelter);
        w.Refusals.Clear();
        string Items() => string.Join(";", w.M.Items.Inventory(0).Select(s => s?.ToString())) + "|" + string.Join(";", w.M.Grid.Machines.Select(m => string.Join(",", w.M.Items.FittedTo(m.Id)) + string.Join(",", w.M.Items.Crate(m.Id))));
        var was = Items();
        arc = SlotOf(w, Arc);
        w.Do(new Fit(new Slot(arc), 999))
            .Do(new Fit(new Slot(0, panel), smelter))
            .Do(new Fit(new Slot(Spec.Inventory.Total), smelter))
            .Do(new Fit(new Slot(empty), smelter))
            .Do(new Fit(new Slot(iron), smelter))
            .Do(new Fit(new Slot(arc), furnace))
            .Do(new Fit(new Slot(arc), crate))
            .Do(new Fit(new Slot(arc), smelter))
            .Do(new Unfit(former, Arc))
            .Do(new Unfit(smelter, "iron"))
            .Do(new Unfit(smelter, Arc, Spec.Inventory.Slots))
            .Do(new Unfit(smelter, Arc, -1))
            .Do(new Unfit(smelter, Arc, iron))
            .Run();
        Assert.Equal(
            ["items.noMachine", "items.noCrate", "items.noSlot", "items.empty", "items.notModule", "items.notModule", "items.notModule", "items.fitted",
             "items.notFitted", "items.notFitted", "items.noSlot", "items.noSlot", "items.full"],
            Reasons(w));
        Assert.Equal(was, Items());

        // A bag with no room left: it stays fitted.
        var full = new Scenario(Layout.Parse(Rows, null, Cables)).GiveItems(0, (Arc, 1));
        FitArc(full, Id(full, "smelter"));
        full.GiveItems(0, ("iron", 500 * Spec.Inventory.Slots));
        full.Do(new Unfit(Id(full, "smelter"), Arc)).Run();
        Assert.Equal(["items.full"], Reasons(full));
        Assert.Equal([Arc], full.M.Items.FittedTo(Id(full, "smelter")));
    }

    [Fact]
    public void TheFurnaceAndThePodTakeNoModuleTheElectricStationsTakeTheArc()
    {
        Assert.Equal(["smelter", "former", "assembler"], Spec.Modules.Arc.Hosts);
        Assert.False(Spec.Modules.Fits(Arc, "furnace"));
        Assert.False(Spec.Modules.Fits(Arc, "lifepod"));
        Assert.False(Spec.Modules.Fits("iron", "smelter"));
        var w = World();
        FitArc(w, Id(w, "furnace"));
        Assert.Equal(["items.notModule"], Reasons(w));
        Assert.Empty(w.M.Items.FittedTo(Id(w, "furnace")));
    }

    [Fact]
    public void DoublesTheSmeltersSpeedAndWhatItDraws()
    {
        var w = World();
        int smelter = Id(w, "smelter");
        var station = Spec.Stations.Get("smelter");
        int speed = 2 * station.Speed, draws = 2 * Spec.Machines.Get("smelter").Draws!.Value;
        FitArc(w, smelter).Run();
        Assert.Equal((speed, station.Heat), (w.M.Crafting.StationOf(smelter)!.Speed, w.M.Crafting.StationOf(smelter)!.Heat));
        Assert.Equal(draws, w.M.Power.Draw(smelter));
        Assert.Equal(0, Demand(w));
        w.Do(new Move(new Slot(SlotOf(w, "rock")), new Target(0, smelter), 1)).Run();
        // The tick it is fed is the first of its work, at the full share the network gave it resting.
        Assert.Equal(speed, w.M.Crafting.Progress(smelter));
        w.Run();
        Assert.Equal(draws, Demand(w));
        // The furnace's work on a rock at the doubled speed: the ticks it takes, rounded up.
        var rock = Spec.Items.Get("rock");
        w.Run((rock.SmeltTicks!.Value * 1000 + speed - 1) / speed - 3);
        Assert.Equal(0, Held(w, smelter, "silicon"));
        w.Run();
        Assert.Equal(rock.Yields!.Single(y => y.Item == "silicon").Count, Held(w, smelter, "silicon"));
    }

    [Fact]
    public void OnANetworkShortOfItsDoubledDrawTheSmelterWorksAtItsShareOfTheDoubledSpeedRoundedDown()
    {
        string[] rows = ["PPP         ", "            ", "############", "#EE........#", "#EE........#", "############"];
        var w = new Scenario(Layout.Parse(rows, null, [((0, 0), (1, 3))])).GiveItems(0, (Arc, 1), ("rock", 1)).Run(2);
        int smelter = Id(w, "smelter");
        FitArc(w, smelter);
        w.Do(new Move(new Slot(SlotOf(w, "rock")), new Target(0, smelter), 1)).Run().Run();
        // A panel's power for the doubled draw: that share of the doubled speed, more than the speed without the arc.
        int makes = Spec.Machines.Get("solarPanel").Makes!.Value, draws = 2 * Spec.Machines.Get("smelter").Draws!.Value;
        int speed = 2 * Spec.Stations.Get("smelter").Speed;
        Assert.True(makes < draws, "the panel falls short of the doubled draw");
        int share = w.M.Power.Satisfaction(smelter);
        Assert.Equal(makes * 1000 / draws, share);
        Assert.Equal(speed + speed * share / 1000, w.M.Crafting.Progress(smelter));
        Assert.True(speed * share / 1000 > speed / 2, "more than it makes without the arc");
    }

    [Fact]
    public void DoublesTheFormersAndTheAssemblersSpeedAndWhatTheyDraw()
    {
        var w = World().GiveItems(0, ("silicon", 2), ("wire", 4));
        int former = Id(w, "former"), assembler = Id(w, "assembler");
        FitArc(w, former);
        FitArc(w, assembler).Run();
        (int Speed, int Draws) Doubled(string kind) => (2 * Spec.Stations.Get(kind).Speed, 2 * Spec.Machines.Get(kind).Draws!.Value);
        var (formerAt, assemblerAt) = (Doubled("former"), Doubled("assembler"));
        Assert.Equal(formerAt, (w.M.Crafting.StationOf(former)!.Speed, w.M.Power.Draw(former)));
        Assert.Equal(assemblerAt, (w.M.Crafting.StationOf(assembler)!.Speed, w.M.Power.Draw(assembler)));
        w.Do(new Craft(former, "gear", 1)).Do(new Craft(assembler, "circuit", 1)).Run();
        Assert.Equal((formerAt.Speed, assemblerAt.Speed), (w.M.Crafting.Progress(former), w.M.Crafting.Progress(assembler)));
        w.Run();
        Assert.Equal(formerAt.Draws + assemblerAt.Draws, Demand(w));
        // A gear and a circuit at the doubled speeds, each in the ticks it takes rounded up, the first two run.
        int Ticks(string craft, int speed) => (Spec.Crafts.Get(craft).Ticks * 1000 + speed - 1) / speed;
        int gear = Ticks("gear", formerAt.Speed), circuit = Ticks("circuit", assemblerAt.Speed);
        Assert.True(gear < circuit, "the gear comes first");
        w.Run(gear - 2);
        Assert.Equal(1, Held(w, former, "gear"));
        w.Run(circuit - gear);
        Assert.Equal(1, Held(w, assembler, "circuit"));
    }

    [Fact]
    public void GainsRoundDownAndCostsUp()
    {
        Assert.Equal(4, Boost.Gain(3, 500));
        Assert.Equal(5, Boost.Cost(3, 500));
        Assert.Equal(1333, Boost.Gain(1000, 333));
        Assert.Equal(8, Boost.Cost(4, 1000));
        Assert.Equal(4000, Boost.Gain(2000, 1000));
        Assert.Equal((3, 3), (Boost.Gain(3, 0), Boost.Cost(3, 0)));
    }

    [Fact]
    public void AMachineHoldingAModuleIsNotTakenApartNorItsMarkWorked()
    {
        var w = new Scenario(Layout.Parse(["##########", "#EE......#", "#EE......#", "##########"])).GiveItems(0, (Arc, 1));
        int smelter = Id(w, "smelter");
        int cell = w.M.Grid.MachineById(smelter)!.Cell;
        FitArc(w, smelter);
        w.Do(new Deconstruct(cell, "machine")).Run();
        Assert.Equal(["building.notEmpty"], Reasons(w));

        // Marked empty, then fitted: its work is refused until the module is out.
        w.Do(new Unfit(smelter, Arc)).Run();
        w.Do(new Deconstruct(cell, "machine")).Run();
        int mark = w.M.Building.Plans.Single().Id;
        FitArc(w, smelter).Do(new Work(mark)).Run();
        Assert.Equal(["building.notEmpty", "building.notEmpty"], Reasons(w));
        w.Do(new Unfit(smelter, Arc)).Run();
        for (int i = 0; i < 400 && w.M.Grid.MachineById(smelter) != null; i++) w.Do(new Work(mark)).Run();
        Assert.Null(w.M.Grid.MachineById(smelter));
        Assert.Equal(1, Carried(w, Arc));
    }

    [Fact]
    public void ASaveKeepsWhatIsFittedAndWritesNothingWhileNoneIs()
    {
        var w = World();
        Assert.DoesNotContain("\"fitted\"", w.World.Save());
        int smelter = Id(w, "smelter"), assembler = Id(w, "assembler");
        FitArc(w, smelter);
        FitArc(w, assembler);
        var hash = w.World.Hash();
        Assert.Contains("\"fitted\"", w.World.Save());
        w.Reload();
        Assert.Equal(hash, w.World.Hash());
        Assert.Equal([Arc], w.M.Items.FittedTo(smelter));
        Assert.Equal(8, w.M.Power.Draw(smelter));
        Assert.Equal(2000, w.M.Crafting.StationOf(assembler)!.Speed);
        w.Do(new Unfit(smelter, Arc)).Do(new Unfit(assembler, Arc)).Run();
        Assert.DoesNotContain("\"fitted\"", w.World.Save());
    }

    [Fact]
    public void DronesServeAMachineHoldingAModuleAndNeverTouchIt()
    {
        string[] rows =
        [
            "########################",
            "#.........D............#",
            "#RR....TT.D...EE.......#",
            "#RR.KK.TTVD..VEE..KK...#",
            "########################",
        ];
        var w = LogisticsTests.Aired(rows, LogisticsTests.RoomsUnder).GiveItems(0, (Arc, 1), ("rock", 2));
        int smelter = Id(w, "smelter");
        int left = w.M.Grid.MachinesOf("crate").First().Id;
        LogisticsTests.Spread(w, left, ("rock", 50));
        FitArc(w, smelter);
        w.Do(new Move(new Slot(SlotOf(w, "rock")), new Target(0, smelter), 2)).Run(600);
        Assert.Empty(w.Refusals);
        // Drones kept its input topped up from the crate, and left its module where it was.
        Assert.True(Held(w, smelter, "rock") > 2, $"rock {Held(w, smelter, "rock")}");
        Assert.Equal([Arc], w.M.Items.FittedTo(smelter));
        Assert.Equal(0, Carried(w, Arc));
        Assert.DoesNotContain(w.M.Grid.Machines, m => Held(w, m.Id, Arc) > 0);
        Assert.DoesNotContain(w.M.Items.Floor, s => s.Item == Arc);
    }

    /// <summary>
    /// A sealed room with nothing open to space, so no stack is ever thrown away: the smelter, the Former, the furnace and a
    /// crate.
    /// </summary>
    private static readonly string[] Sealed = ["##########", "#EE.MM.KK#", "#EE.MM.FF#", "#......FF#", "##########"];

    /// <summary>Every arc module there is: carried by peers 0 to 4, held by machines, on the floor and fitted.</summary>
    private static int Arcs(Scenario w) =>
        Enumerable.Range(0, 5).Sum(p => Carried(w, Arc, p))
        + w.M.Grid.Machines.Sum(m => Held(w, m.Id, Arc) + w.M.Items.FittedTo(m.Id).Count(f => f == Arc))
        + w.M.Items.Floor.Where(s => s.Item == Arc).Sum(s => s.Count);

    /// <summary>
    /// Items are conserved across fits and takes: whatever the items fuzz draws, fits, takes, moves, drops and pick-ups
    /// by three peers carrying modules, no arc module is made or lost.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property FitsAndTakesNeitherMakeNorLoseAModule(PositiveInt seed)
    {
        var w = new Scenario(Layout.Parse(Sealed)).GiveItems(0, (Arc, 2)).GiveItems(1, (Arc, 1), ("iron", 9)).GiveItems(2, (Arc, 2));
        int before = Arcs(w);
        var rng = new Rng(new RngState { S = (uint)seed.Get });
        for (int i = 0; i < 300; i++)
            if (w.M.Items.Fuzz(rng, i % 3) is { } c) w.Do(c).Run();
        return (Arcs(w) == before).Label($"{Arcs(w)} arc modules of {before}");
    }

    [Fact]
    public void TheFuzzDrawsFitsAndTakesTakenAndRefused()
    {
        var w = World().GiveItems(1, (Arc, 2)).GiveItems(2, (Arc, 1));
        var rng = new Rng(new RngState { S = 11 });
        int fits = 0, unfits = 0;
        for (int i = 0; i < 4000; i++)
        {
            if (w.M.Items.Fuzz(rng, i % 3) is not { } c) continue;
            int refused = w.Refusals.Count;
            w.Do(c).Run();
            if (w.Refusals.Count > refused) continue;
            if (c is Fit) fits++;
            if (c is Unfit) unfits++;
        }
        Assert.True(fits > 3 && unfits > 3, $"{fits} fits, {unfits} takes");
        var reasons = w.Refusals.Where(r => r.Command is Fit or Unfit).Select(r => r.Reason).ToHashSet();
        foreach (var why in new[] { "items.noMachine", "items.notModule", "items.fitted", "items.notFitted", "items.noSlot", "items.empty" })
            Assert.Contains(why, reasons);
    }

    /// <summary>
    /// Two worlds fed the same fits and takes, through the wire's encoding, stay alike; one loaded from the other's save
    /// half way plays on alike: what is fitted is saved and hashed.
    /// </summary>
    [Fact]
    public void TwoWorldsFittingAndTakingAlikeStayAlikeThroughASave()
    {
        static Scenario Make() => World().GiveItems(1, (Arc, 2)).GiveItems(2, (Arc, 1));
        var a = Make();
        var b = Make();
        var rng = new Rng(new RngState { S = 5 });
        int fitted = 0;
        for (int i = 0; i < 1500; i++)
        {
            if (i == 700) b.Reload(a.World.Save());
            if (a.M.Items.Fuzz(rng, i % 3) is not { } c) continue;
            a.Do(c).Run();
            b.Do(b.World.Decode(a.World.Encode(c))).Run();
            fitted = Math.Max(fitted, a.M.Grid.Machines.Sum(m => a.M.Items.FittedTo(m.Id).Count));
            Assert.Equal(a.World.Hash(), b.World.Hash());
        }
        Assert.True(fitted > 0);
    }
}
