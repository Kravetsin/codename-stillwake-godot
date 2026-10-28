using Stillwake.Game.Features.Crafting;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Tests;

/// <summary>What the Fabricator's panel shows: each recipe's fixed place, how it looks, where a click goes, the unit's bar, what it is doing.</summary>
public class CraftsTests
{
    private const string Room = """
        #######
        #OOO..#
        #OOO..#
        #OOOII#
        #######
        """;
    private const int Bench = 2;
    private static readonly Content.Station Fabricator = Content.Stations.Get("lifepod");
    private static readonly Content.Station Workbench = Content.Stations.Get("workbench");
    private static readonly Content.Station Former = Content.Stations.Get("former");
    private static readonly Content.Station Assembler = Content.Stations.Get("assembler");

    private static TestWorld Start() => new(Layout.Parse(Room).Doubled() with { Known = Content.Research.StartKnown });

    private static Look LookOf(TestWorld w, string craft) =>
        Crafts.LookOf(w.M.Research.Knows(craft), Crafts.Lines(craft, w.M.Items.Totals(0)), w.M.Items.Creative);

    [Fact]
    public void LaysEveryRecipeInItsFixedPlaceFourARowTwoRowsAtLeast()
    {
        Assert.Equal(4, Crafts.Columns);
        // Each row of a station's recipes from the start of a row of four, two rows even for one.
        Assert.Equal(["plate", "gear", null, null, "wire", null, null, null, "rotor", "coil", "tube", "frame"],
            Crafts.Places(Fabricator with { Rows = [["plate", "gear"], ["wire"], ["rotor", "coil", "tube", "frame"]] }));
        Assert.Equal(["circuit", null, null, null, null, null, null, null], Crafts.Places(Assembler with { Rows = [["circuit"]] }));
        // No station lays more in a row than fits.
        Assert.All(Content.Stations.All, s => Assert.All(s.Rows, row => Assert.InRange(row.Count, 1, Crafts.Columns)));
        // Every craft has its place at some station: new ones go to machines (0057), not all of them to the pod.
        Assert.All(Content.Crafts.All, c => Assert.Contains(Content.Stations.All, s => Crafts.Places(s).Contains(c.Item)));
    }

    [Fact]
    public void LooksReadyWhenAUnitIsCarriedShortWhenNotAndLockedWhenNotKnown()
    {
        var w = Start();
        Assert.Equal(Look.Short, LookOf(w, "wire"));
        Assert.Equal(Look.Locked, LookOf(w, "plate"));
        w.Give(0, ("scrap", 2));
        Assert.Equal(Look.Ready, LookOf(w, "wire"));
    }

    [Fact]
    public void LooksReadyEverywhereInCreativeModeCarryingNothing()
    {
        var w = Start();
        w.Step(new SetCreative(true));
        Assert.All(Content.Crafts.All, c => Assert.Equal(Look.Ready, LookOf(w, c.Item)));
    }

    [Fact]
    public void LaysAUnitsCostAgainstWhatIsCarriedShortOnlyOutOfCreativeMode()
    {
        var w = new TestWorld(Layout.Parse(Room).Doubled()).Give(0, ("gear", 3), ("wire", 1));
        var lines = Crafts.Lines("rotor", w.M.Items.Totals(0));
        Assert.Equal([new CostLine("gear", 3, 1), new CostLine("wire", 1, 2)], lines);
        Assert.Equal([false, true], lines.Select(l => Crafts.Short(l, creative: false)));
        Assert.Equal([false, false], lines.Select(l => Crafts.Short(l, creative: true)));
    }

    [Fact]
    public void GoesToTheFirstItemAShortRecipeLacksThatItsStationMakesAndNowhereForOneMadeElsewhere()
    {
        Assert.Equal("gear", Crafts.MissingCraft(Crafts.Lines("rotor", []), creative: false, Workbench));
        Assert.Equal("wire", Crafts.MissingCraft(Crafts.Lines("rotor", [("gear", 1), ("wire", 1)]), creative: false, Workbench));
        Assert.Null(Crafts.MissingCraft(Crafts.Lines("plate", [("iron", 1)]), creative: false, Workbench));
        Assert.Null(Crafts.MissingCraft(Crafts.Lines("rotor", []), creative: true, Workbench));
        // The assembler makes no wire, and the Former no circuits: a click there would find nothing to go to.
        Assert.Null(Crafts.MissingCraft(Crafts.Lines("circuit", [("silicon", 1)]), creative: false, Assembler));
        Assert.Null(Crafts.MissingCraft(Crafts.Lines("battery", [("plate", 2), ("coil", 2), ("wire", 4)]), creative: false, Former));
    }

    /// <summary>
    /// Every item a queued station's recipe costs says, short, where it comes from, unless that station makes it: in its own
    /// words when salvaged or smelted, or by the stations that make it (wire at the assembler: the workbench, the Former).
    /// </summary>
    [Fact]
    public void SaysWhereEveryItemARecipeCostsComesFromWhenItsStationDoesNotMakeIt()
    {
        foreach (var station in Content.Stations.All.Where(s => !s.Fed))
            foreach (var item in station.Rows.SelectMany(r => r).SelectMany(c => Content.Crafts.Get(c).Cost).Select(c => c.Item).Distinct())
            {
                if (Crafts.Makes(station, item)) Assert.Null(Crafts.From(item, station));
                else Assert.False(string.IsNullOrEmpty(Crafts.From(item, station)), $"{station.Machine}: {item}");
            }
        Assert.Equal(T("crafts.from.scrap"), Crafts.From("scrap", Fabricator));
        Assert.Equal("made at: Workbench, Former", Crafts.From("wire", Assembler));
        Assert.Equal("the circuit assembler, or break cable racks or derelicts", Crafts.From("circuit", Fabricator));
        Assert.Null(Crafts.From("wire", Workbench));
        // An item salvaged, smelted or made besides elsewhere has its own words; one only crafted has none, its stations named.
        foreach (var key in Table.Keys.Where(k => k.StartsWith("crafts.from.", StringComparison.Ordinal)))
        {
            string item = key["crafts.from.".Length..];
            Assert.True(!Content.Crafts.TryGet(item, out _) || Content.Loot.All.Any(l => l.Entries.Any(e => e.Item == item)), item);
        }
    }

    [Fact]
    public void SaysAUnitsTimeAtThePodInSecondsToATenth()
    {
        // Whole seconds or to a tenth, never more digits: at the pod's own speed, and at one that cuts seconds into thirds.
        var thirds = Fabricator with { Speed = 300 };
        Assert.All(Content.Crafts.All, c => Assert.Matches(@"^\d+(\.\d)?$", Crafts.Seconds(c.Item, Fabricator)));
        Assert.All(Content.Crafts.All, c => Assert.Matches(@"^\d+(\.\d)?$", Crafts.Seconds(c.Item, thirds)));
        Assert.Contains(Content.Crafts.All, c => Crafts.Seconds(c.Item, thirds).Contains('.'));
        Assert.All(Content.Crafts.All, c => Assert.InRange(
            double.Parse(Crafts.Seconds(c.Item, Fabricator), System.Globalization.CultureInfo.InvariantCulture) * World.TicksPerSecond,
            c.Ticks * 1000 / Fabricator.Speed - 1, c.Ticks * 1000 / Fabricator.Speed + 1));
    }

    [Fact]
    public void QueuesOneOnAClickAndFiveWithShift()
    {
        Assert.Equal(1, Crafts.CountFor(shift: false));
        Assert.Equal(5, Crafts.CountFor(shift: true));
    }

    [Fact]
    public void FillsTheBarOfTheUnitAtWorkFullWhileItWaitsAndSaysWhatItIsDoing()
    {
        var w = new TestWorld(Layout.Parse(Room).Doubled()).Give(0, ("scrap", 2));
        Assert.Equal(Doing.Ready, Crafts.DoingOf(w.M.Crafting.Queue(Bench), w.M.Crafting.Progress(Bench)));
        w.Step(new Craft(Bench, "wire", 1));
        int work = Crafts.WorkOf("wire");
        Assert.Equal((double)Workbench.Speed / work, Crafts.Share("wire", w.M.Crafting.Progress(Bench)));
        Assert.Equal(1.0, Crafts.Share("wire", work));
        Assert.Equal(Doing.Making, Crafts.DoingOf(w.M.Crafting.Queue(Bench), w.M.Crafting.Progress(Bench)));
        Assert.Equal(Doing.OutputFull, Crafts.DoingOf([new Entry(1, "wire", 1, 0)], work));
    }

    [Fact]
    public void SaysAFirstEntryWaitingForItsInputsIsWaitingNotWithoutPower()
    {
        IReadOnlyList<Entry> queue = [new Entry(1, "plate", 1, 0, FromInputs: true)];
        Assert.Equal(Doing.Waiting, Crafts.DoingOf(queue, 0, works: false, waiting: true));
        Assert.Equal(Doing.NoPower, Crafts.DoingOf(queue, 0, works: false));
    }

    [Fact]
    public void OffersTheRightButtonFromTheInputsAtAStationWithInputsWhateverIsCarriedButNeverAtThePod()
    {
        var former = Content.Stations.Get("former");
        CardKey Rmb = new(T("key.rmb"), T("card.fromInputs")), Shift = new(T("key.shift"), T("card.many", ("n", Crafts.Many))), Craft = new(T("key.lmb"), T("card.craftIt"));
        Assert.Equal([Craft, Rmb, Shift], Crafts.Block("plate", former, [("iron", 2)], creative: false).Keys);
        // Short of iron, the left button goes to nothing the Former makes; the right still queues from the inputs.
        Assert.Equal([Rmb, Shift], Crafts.Block("plate", former, [], creative: false).Keys);
        Assert.Equal([Craft, Shift], Crafts.Block("plate", Fabricator, [("iron", 2)], creative: false).Keys);
        Assert.True(Crafts.HasInputs(former) && Crafts.HasInputs(Assembler) && !Crafts.HasInputs(Fabricator));
    }
}
