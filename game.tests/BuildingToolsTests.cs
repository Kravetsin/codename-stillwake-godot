using System.Text.RegularExpressions;
using Stillwake.Content;
using Stillwake.Game.Features.Building;
using Stillwake.Game.I18n;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The build menu, the hand and the overlay as a filter: apps/game/src/features/building/tools.test.ts, one for one.</summary>
public class BuildingToolsTests
{
    /// <summary>What a card puts in the world, by the name the grid gives it, a wall by its material.</summary>
    private static string? What(Tool? tool) => tool switch
    {
        null => null,
        CableTool => "cable",
        PlaceTool { Placement: var p } when p.Kind is "wall" or "backWall" => $"{p.Kind}:{p.Material}",
        PlaceTool { Placement: var p } => p.Kind,
        _ => null,
    };

    private static Tool Card(string kind) => Tools.All.First(each => What(each) == kind);

    [Fact]
    public void HasACardForEverythingTheShipIsBuiltOfEachOnOneTab()
    {
        var carded = Tools.Tabs.SelectMany(tab => tab.Tools.Select(What)).ToList();
        Assert.Equal(carded.Count, carded.Distinct().Count());
        Assert.All(Tools.Tabs, tab => Assert.NotEmpty(tab.Tools));
        var expected = Materials.All.SelectMany(m => new[] { $"wall:{m.Id}", $"backWall:{m.Id}" })
            .Concat(Builds.All.Select(b => b.Id))
            .Concat(Machines.All.Select(m => m.Kind))
            .Append("cable");
        Assert.Equal(expected.Order(StringComparer.Ordinal), carded.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void NamesEveryTabAndToolAndSaysWhatEachToolIsForInWordsNotKeys()
    {
        foreach (var tab in Tools.Tabs) Assert.True(En.Table.ContainsKey($"build.tab.{tab.Id}"), tab.Id);
        foreach (var tool in Tools.All)
        {
            Assert.DoesNotMatch(new Regex(@"\b(tool|material)\.\w"), tool.Label);
            Assert.True(En.Table.ContainsKey($"tool.{tool.Id}.about"), tool.Id);
        }
    }

    [Fact]
    public void PricesEachCardAsTheSimDoesACableAtItsOwnCost()
    {
        var q = new TestWorld("#.#").M.Building;
        Assert.Equal(Builds.Get("door").Cost, Tools.CostOf(Card("door"), q.CostFor));
        Assert.Equal(Content.Power.CableCost, Tools.CostOf(Card("cable"), q.CostFor));
    }

    [Fact]
    public void LaysWhatIsNeededAgainstWhatThereIsItemByItemShortOnesCountedShort()
    {
        Assert.Equal(
            [new CostLine("scrap", 5, 2), new CostLine("iron", 0, 1)],
            CostLine.Of([("scrap", 2), ("iron", 1)], [("scrap", 5)]));
        Assert.Empty(CostLine.Of([], [("scrap", 5)]));
    }

    [Fact]
    public void OpensOnTheTabOfTheOverlayThatIsOn()
    {
        Assert.Equal(new[] { "power", "air", "air", null }, new[] { Tools.TabFor(Tools.Power), Tools.TabFor("pipes"), Tools.TabFor("atmosphere"), Tools.TabFor(null) });
        Assert.Contains(Tools.Tabs, tab => tab.Id == "power");
        Assert.Contains(Tools.Tabs, tab => tab.Id == "air");
    }

    [Fact]
    public void HoldsThePlanWhileItIsOutACardOrNoneTheCableAsTheCable()
    {
        var ui = new Ui();
        var plan = new PlanHand(ui);
        plan.PutPlanAway();
        Assert.Equal(Holding.Free, ui.Hand.Get());
        plan.PlanOut.Set(true);
        Assert.Equal(Holding.Plan, ui.Hand.Get());
        plan.Selected.Set(Card("floor"));
        Assert.Equal(Holding.Card, ui.Hand.Get());
        plan.Selected.Set(null);
        Assert.Equal((Holding.Plan, true), (ui.Hand.Get(), plan.PlanOut.Get()));
        plan.Selected.Set(Card("cable"));
        Assert.Equal(Holding.Cable, ui.Hand.Get());
        plan.PutPlanAway();
        Assert.Equal((Holding.Free, false, (Tool?)null), (ui.Hand.Get(), plan.PlanOut.Get(), plan.Selected.Get()));
    }

    [Fact]
    public void TakesThePlanOutWithACardPickedWhileItIsAwayAsTheMiddleButtonDoes()
    {
        var ui = new Ui();
        var plan = new PlanHand(ui);
        plan.Selected.Set(Card("lamp"));
        Assert.Equal((Holding.Card, true), (ui.Hand.Get(), plan.PlanOut.Get()));
    }

    [Fact]
    public void PutsThePlanAwayWhenFreedFromElsewhereAHotbarSlotPicked()
    {
        var ui = new Ui();
        var plan = new PlanHand(ui);
        plan.Selected.Set(Card("floor"));
        ui.Hand.Set(Holding.Free);
        Assert.Equal((Holding.Free, false, (Tool?)null), (ui.Hand.Get(), plan.PlanOut.Get(), plan.Selected.Get()));
    }

    [Fact]
    public void ShowsEachCardsOverlayOrNoneAndGivesBackTheOneBeforeOnceThePlanIsAway()
    {
        string? Shown(string kind) => Tools.OverlayFor(Card(kind));
        Assert.All(new[] { "wall:iron", "floor", "door", "crate", "furnace" }.Select(Shown), Assert.Null);
        Assert.All(new[] { "pipe", "valve", "vent", "tank", "generator" }.Select(Shown), o => Assert.Equal("pipes", o));
        Assert.All(new[] { "cable", "lamp", "solarPanel" }.Select(Shown), o => Assert.Equal(Tools.Power, o));

        var ui = new Ui();
        var plan = new PlanHand(ui);
        ui.OverlayOn.Set("atmosphere");
        plan.Selected.Set(Card("pipe"));
        Assert.Equal("pipes", ui.OverlayOn.Get());
        plan.Selected.Set(Card("floor"));
        Assert.Null(ui.OverlayOn.Get());
        plan.Selected.Set(Card("lamp"));
        Assert.Equal(Tools.Power, ui.OverlayOn.Get());
        // A card let go keeps what is shown while the plan is out; the plan away gives back the one before.
        plan.Selected.Set(null);
        Assert.Equal(Tools.Power, ui.OverlayOn.Get());
        plan.PutPlanAway();
        Assert.Equal("atmosphere", ui.OverlayOn.Get());
    }

    [Fact]
    public void KeepsAnyCardButTheCableUnderAnyOverlayTheCableGoesWithThePowerOverlay()
    {
        var ui = new Ui();
        var plan = new PlanHand(ui);
        plan.Selected.Set(Card("wall:iron"));
        ui.OverlayOn.Set(Tools.Power);
        Assert.Equal("wall:iron", What(plan.Selected.Get()));
        plan.Selected.Set(Card("cable"));
        Assert.Equal(Tools.Power, ui.OverlayOn.Get());
        // Another overlay, or none, puts the cable away and stays; the plan stays out.
        ui.OverlayOn.Set("pipes");
        Assert.Equal(((Tool?)null, "pipes", Holding.Plan), (plan.Selected.Get(), ui.OverlayOn.Get(), ui.Hand.Get()));
        plan.Selected.Set(Card("cable"));
        ui.OverlayOn.Set(null);
        Assert.Equal(((Tool?)null, (string?)null), (plan.Selected.Get(), ui.OverlayOn.Get()));
    }

    [Fact]
    public void RemovesAndPicksPipesUnderThePipesOverlayAndWhatTheShipIsBuiltOfElsewhere()
    {
        Assert.Equal(["conduit"], Tools.LayersFor("pipes"));
        Assert.Equal(["machine", "structure", "back"], Tools.LayersFor(null));
        Assert.Equal(Tools.LayersFor(null), Tools.LayersFor("atmosphere"));
    }

    [Fact]
    public void PicksTheCardOfWhatIsUnderThePointerAPlanFirstThenWhatIsBuiltInTheOverlaysLayers()
    {
        // Seven cells wide: cell (x, y) is y * 7 + x. A tank at 8; a pipe at 11; open cells behind
        // walls; a cable along the top, from the hull's corner at 0 to 6.
        string[] rows = ["#######", "#TT -.#", "#TT...#", "#######"];
        var w = new TestWorld(Layout.Parse(rows, null, [((0, 0), (6, 0))]));
        string? Pick(int cell, string? overlay = null) =>
            What(Tools.CardAt(cell, overlay, w.M.Grid, w.M.Building, w.M.Power, w.M.Research));
        Assert.Equal($"wall:{w.M.Grid.WallAt(0)!.Id}", Pick(0));
        Assert.Equal("tank", Pick(9));
        Assert.Equal("backWall:scrap", Pick(11));
        Assert.Equal("pipe", Pick(11, "pipes"));
        Assert.Null(Pick(10, "pipes"));
        w.Step(new PlacePlan(12, new Placement("structure", "door")));
        Assert.Equal("door", Pick(12));
        // Under the power overlay, the cable on a node a cable ends at; nothing on one with none.
        Assert.Equal("cable", Pick(0, Tools.Power));
        Assert.Null(Pick(14, Tools.Power));
    }

    [Fact]
    public void ShowsAndPicksOnlyTheCardsTheWorldKnowsTheCableAlways()
    {
        // The tank and the wall of the scrap the hull is of, in a world knowing only its start.
        var w = new TestWorld(Layout.Parse(["#######", "#TT ..#", "#TT...#", "#######"]) with { Known = Research.StartKnown });
        var known = Tools.All.Where(each => Tools.Known(each, w.M.Research)).Select(What).ToList();
        Assert.Contains("cable", known);
        Assert.Contains("furnace", known);
        Assert.Contains("analyzer", known);
        Assert.Contains("crate", known);
        Assert.False(Tools.Known(Card("tank"), w.M.Research));
        Assert.Null(Tools.CardAt(9, null, w.M.Grid, w.M.Building, w.M.Power, w.M.Research));
        Assert.Equal("wall:scrap", What(Tools.CardAt(0, null, w.M.Grid, w.M.Building, w.M.Power, w.M.Research)));
    }
}
