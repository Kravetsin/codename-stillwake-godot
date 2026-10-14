using Stillwake.Game.Features.Crafting;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>What the crafts panel shows: the known crafts, their cost against what is carried, the unit's bar.</summary>
public class CraftsTests
{
    private const string Room = """
        #####
        #...#
        #####
        """;

    private static TestWorld Start() => new(Layout.Parse(Room) with { Known = Content.Research.StartKnown });

    [Fact]
    public void ShowsTheCraftsTheWorldKnowsAndEveryOneInCreativeMode()
    {
        var w = Start();
        Assert.Equal(["wire"], Crafts.Shown(w.M.Research).Select(r => r.Id));
        w.Step(new SetCreative(true));
        Assert.Equal(Content.Crafts.All.Select(c => c.Item), Crafts.Shown(w.M.Research).Select(r => r.Id));
    }

    [Fact]
    public void LaysAUnitsCostAgainstWhatIsCarriedShortOnlyOutOfCreativeMode()
    {
        var w = new TestWorld(Room).Give(0, ("gear", 3), ("wire", 1));
        var rotor = Crafts.Shown(w.M.Research).Single(r => r.Id == "rotor");
        var lines = Crafts.Lines(rotor, w.M.Items.Totals(0));
        Assert.Equal([new CostLine("gear", 3, 1), new CostLine("wire", 1, 2)], lines);
        Assert.Equal([false, true], lines.Select(l => Crafts.Short(l, creative: false)));
        Assert.Equal([false, false], lines.Select(l => Crafts.Short(l, creative: true)));
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
