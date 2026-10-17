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
        #OOO..#
        #######
        """;
    private const int Pod = 1;
    private static readonly Content.Station Fabricator = Content.Stations.Get("lifepod");

    private static TestWorld Start() => new(Layout.Parse(Room) with { Known = Content.Research.StartKnown });

    private static Look LookOf(TestWorld w, string craft) =>
        Crafts.LookOf(w.M.Research.Knows(craft), Crafts.Lines(craft, w.M.Items.Totals(0)), w.M.Items.Creative);

    [Fact]
    public void LaysEveryRecipeInItsFixedPlaceFourARowTwoRowsAtLeast()
    {
        Assert.Equal(4, Crafts.Columns);
        Assert.Equal(["plate", "gear", "tube", "frame", "wire", "coil", "rotor", null, "fireAxe", "harpoon", "drill", null, "jetpack", "battery", null, null], Crafts.Places(Fabricator));
        Assert.Equal(Content.Crafts.All.Count, Crafts.Places(Fabricator).Count(p => p != null));
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
    public void GoesToTheFirstCraftedItemAShortRecipeLacksAndNowhereForARawOne()
    {
        Assert.Equal("gear", Crafts.MissingCraft(Crafts.Lines("rotor", []), creative: false));
        Assert.Equal("wire", Crafts.MissingCraft(Crafts.Lines("rotor", [("gear", 1), ("wire", 1)]), creative: false));
        Assert.Null(Crafts.MissingCraft(Crafts.Lines("plate", [("iron", 1)]), creative: false));
        Assert.Null(Crafts.MissingCraft(Crafts.Lines("rotor", []), creative: true));
    }

    [Fact]
    public void SaysWhereEveryRawItemARecipeCostsComesFrom()
    {
        var raw = Content.Crafts.All.SelectMany(c => c.Cost).Select(c => c.Item).Where(Crafts.Raw).Distinct().ToList();
        Assert.Contains("scrap", raw);
        Assert.All(raw, item => Assert.True(Table.ContainsKey($"crafts.from.{item}"), item));
    }

    [Fact]
    public void SaysAUnitsTimeAtThePodInSecondsToATenth()
    {
        // The pod at a fifth of full speed: a wire 5 s, a plate 10, a rotor 15, a frame 20, the harpoon 30.
        Assert.Equal(["5", "10", "15", "20", "30"], new[] { "wire", "plate", "rotor", "frame", "harpoon" }.Select(c => Crafts.Seconds(c, Fabricator)));
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
        var w = new TestWorld(Room).Give(0, ("scrap", 2));
        Assert.Equal(Doing.Ready, Crafts.DoingOf(w.M.Crafting.Queue(Pod), w.M.Crafting.Progress(Pod)));
        w.Step(new Craft(Pod, "wire", 1));
        int work = Crafts.WorkOf("wire");
        Assert.Equal((double)Fabricator.Speed / work, Crafts.Share("wire", w.M.Crafting.Progress(Pod)));
        Assert.Equal(1.0, Crafts.Share("wire", work));
        Assert.Equal(Doing.Making, Crafts.DoingOf(w.M.Crafting.Queue(Pod), w.M.Crafting.Progress(Pod)));
        Assert.Equal(Doing.OutputFull, Crafts.DoingOf([new Entry(1, "wire", 1, 0)], work));
    }
}
