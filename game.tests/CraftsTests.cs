using Stillwake.Game.Features.Crafting;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Tests;

/// <summary>What the crafts window shows: each craft's fixed place, how it looks, where a click goes, the pins, the unit's bar.</summary>
public class CraftsTests
{
    private const string Room = """
        #####
        #...#
        #####
        """;

    private static TestWorld Start() => new(Layout.Parse(Room) with { Known = Content.Research.StartKnown });

    private static Look LookOf(TestWorld w, string craft) =>
        Crafts.LookOf(w.M.Research.Knows(craft), Crafts.Lines(craft, w.M.Items.Totals(0)), w.M.Items.Creative);

    [Fact]
    public void FindsEachCraftInItsFixedPlaceAndNoPlaceForARawItem()
    {
        Assert.Equal(["parts"], Crafts.Tabs.Select(t => t.Id));
        Assert.Equal(new Spot(0, 0, 0), Crafts.SpotOf("plate"));
        Assert.Equal(new Spot(0, 0, 2), Crafts.SpotOf("rotor"));
        Assert.Equal(new Spot(0, 1, 0), Crafts.SpotOf("wire"));
        Assert.Null(Crafts.SpotOf("scrap"));
    }

    [Fact]
    public void DrawsAtLeastFiveRowsAndWrapsTheWheelRoundTheTabs()
    {
        Assert.Equal(Crafts.RowsShown, Crafts.RowsOf(Crafts.Tabs[0]));
        Assert.Equal(0, Crafts.Step(0, 1 * Crafts.Tabs.Count));
        Assert.Equal(Crafts.Tabs.Count - 1, Crafts.Step(0, -1));
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
        var w = new TestWorld(Room).Give(0, ("gear", 3), ("wire", 1));
        var lines = Crafts.Lines("rotor", w.M.Items.Totals(0));
        Assert.Equal([new CostLine("gear", 3, 1), new CostLine("wire", 1, 2)], lines);
        Assert.Equal([false, true], lines.Select(l => Crafts.Short(l, creative: false)));
        Assert.Equal([false, false], lines.Select(l => Crafts.Short(l, creative: true)));
    }

    [Fact]
    public void GoesToTheFirstCraftedItemAShortCraftLacksAndNowhereForARawOne()
    {
        Assert.Equal("gear", Crafts.MissingCraft(Crafts.Lines("rotor", []), creative: false));
        Assert.Equal("wire", Crafts.MissingCraft(Crafts.Lines("rotor", [("gear", 1), ("wire", 1)]), creative: false));
        Assert.Null(Crafts.MissingCraft(Crafts.Lines("plate", [("iron", 1)]), creative: false));
        Assert.Null(Crafts.MissingCraft(Crafts.Lines("rotor", []), creative: true));
    }

    [Fact]
    public void SaysWhereEveryRawItemACraftCostsComesFrom()
    {
        var raw = Content.Crafts.All.SelectMany(c => c.Cost).Select(c => c.Item).Where(Crafts.Raw).Distinct().ToList();
        Assert.Contains("scrap", raw);
        Assert.All(raw, item => Assert.True(Table.ContainsKey($"crafts.from.{item}"), item));
        Assert.All(Crafts.Tabs, tab => Assert.True(Table.ContainsKey($"crafts.tab.{tab.Id}"), tab.Id));
    }

    [Fact]
    public void PinsAKnownCraftAtTheEndUnpinsAPinnedOneAndKeepsNoMoreThanItsRow()
    {
        Assert.Equal(["wire", "gear"], Crafts.Toggle(["wire"], "gear", known: true));
        Assert.Equal(["wire"], Crafts.Toggle(["wire"], "gear", known: false));
        Assert.Equal(["gear"], Crafts.Toggle(["wire", "gear"], "wire", known: false));
        var full = Enumerable.Repeat("wire", Crafts.PinMost).Select((p, i) => $"{p}{i}").ToList();
        Assert.Equal(full, Crafts.Toggle(full, "gear", known: true));
    }

    [Fact]
    public void LoadsSavedPinsOfCraftsThereStillAreEachOnce()
    {
        Assert.Equal(["rotor", "wire"], Crafts.Loaded(["rotor", "teleporter", "wire", "rotor"]));
    }

    [Fact]
    public void SaysAUnitsTimeInSecondsToATenth()
    {
        Assert.All(Content.Crafts.All, c => Assert.InRange(
            double.Parse(Crafts.Seconds(c.Item), System.Globalization.CultureInfo.InvariantCulture) * World.TicksPerSecond,
            c.Ticks - 1, c.Ticks + 1));
    }

    [Fact]
    public void QueuesOneOnAClickAndFiveWithShift()
    {
        Assert.Equal(1, Crafts.CountFor(shift: false));
        Assert.Equal(5, Crafts.CountFor(shift: true));
    }

    [Fact]
    public void FillsTheBarOfTheUnitAtWorkFullWhileItWaits()
    {
        var w = new TestWorld(Room).Give(0, ("scrap", 2));
        w.Step(new Craft("wire", 1));
        int ticks = Content.Crafts.Get("wire").Ticks;
        Assert.Equal(1.0 / ticks, Crafts.Share("wire", w.M.Crafting.Progress(0)));
        Assert.Equal(1.0, Crafts.Share("wire", ticks));
    }
}
