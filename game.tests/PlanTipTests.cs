using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Xunit;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Tests;

/// <summary>A blueprint's cost under the pointer and a card's price: carried against still needed, alike.</summary>
public class PlanTipTests
{
    [Fact]
    public void LaysWhatIsCarriedAgainstTheCostLessWhatWasDeliveredInTheCostsOrder()
    {
        Assert.Equal(
            [new CostLine("plate", 2, 0), new CostLine("tube", 0, 1), new CostLine("scrap", 100, 2)],
            CostLine.Lacking([("plate", 1), ("tube", 1), ("scrap", 3)], [("scrap", 1), ("plate", 1)], [("scrap", 60), ("plate", 2), ("scrap", 40)]));
        // Nothing delivered: a card's price, the whole cost.
        Assert.Equal([new CostLine("scrap", 5, 2)], CostLine.Lacking([("scrap", 2), ("iron", 0)], [], [("scrap", 5)]));
        // More delivered than the cost asks never needs less than nothing.
        Assert.Equal([new CostLine("scrap", 0, 0)], CostLine.Lacking([("scrap", 2)], [("scrap", 3)], []));
    }

    [Fact]
    public void SaysHaveOverStillNeededGreenWhenEnoughRedWhenNotAndATickOnceDelivered()
    {
        Assert.Equal(("100/2", Tone.Good, false), Row(new CostLine("scrap", 100, 2)));
        Assert.Equal(("2/2", Tone.Good, false), Row(new CostLine("scrap", 2, 2)));
        Assert.Equal(("1/2", Tone.Bad, false), Row(new CostLine("scrap", 1, 2)));
        Assert.Equal(("✓", Tone.Good, true), Row(new CostLine("plate", 0, 0)));

        static (string, Tone, bool) Row(CostLine c) => (c.Count, c.Tone, c.Done);
    }

    [Fact]
    public void ATipNamesWhatIsPlannedOverItsPictureThenARowAnItemWithNoNameLargeAndADeliveredOneFaded()
    {
        var wall = Placement.Wall("iron");
        var tip = new PlanTip(wall, [new CostLine("plate", 3, 0), new CostLine("scrap", 1, 2)]).Tip;
        Assert.Equal($"{T("tool.wall")}: {T("material.iron")}", tip.Name);
        Assert.Equal(wall, tip.Picture);
        Assert.Equal(
            [new TipLine("✓", Tone.Good, "plate", Large: true, Faded: true), new TipLine("1/2", Tone.Bad, "scrap", Large: true)],
            tip.Lines);
        Assert.Equal(T("tool.vent"), new PlanTip(new Placement("machine", "vent"), [new CostLine("tube", 0, 1)]).Tip.Name);
    }

    [Fact]
    public void IsEqualByWhatItSaysSoOneSetEveryFrameRedrawsNothing()
    {
        PlanTip Of(int have) => new(Placement.Wall(), [new CostLine("scrap", have, 2)]);
        Assert.Equal(Of(5), Of(5));
        Assert.Equal(Of(5).Tip, Of(5).Tip);
        Assert.NotEqual(Of(5), Of(4));
        Assert.NotEqual(Of(5).Tip, Of(5).Tip with { Picture = Placement.Wall("iron") });
    }
}
