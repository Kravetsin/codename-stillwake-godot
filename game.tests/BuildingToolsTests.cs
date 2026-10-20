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

/// <summary>The build menu, the hand and the tab as the mode (0030): from apps/game/src/features/building/tools.test.ts.</summary>
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
    public void HasACardForEverythingTheShipIsBuiltOfEachOnOneTabButWhatIsBuiltIn()
    {
        var carded = Tools.Tabs.SelectMany(tab => tab.Tools.Select(What)).ToList();
        Assert.Equal(carded.Count, carded.Distinct().Count());
        Assert.All(Tools.Tabs, tab => Assert.NotEmpty(tab.Tools));
        // Walls of every material but bedrock, back walls of the hull materials only (0077).
        var expected = Materials.All.Where(m => m.Built).Select(m => $"wall:{m.Id}").Concat(Materials.All.Where(m => !m.Natural).Select(m => $"backWall:{m.Id}"))
            .Concat(Builds.All.Select(b => b.Id))
            .Concat(Machines.All.Where(m => !m.Fixed).Select(m => m.Kind))
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
    public void ShowsTheOpenTabsOverlayAndWithTheMenuClosedOnlyAirIfWanted()
    {
        foreach (bool airOn in new[] { false, true })
            foreach (var tab in Tools.Tabs.Select(t => t.Id))
            {
                string? air = airOn ? Tools.Air : null;
                // Closed, whatever the tab: air or nothing.
                Assert.Equal(air, Tools.OverlayShown(false, tab, airOn));
                string? own = tab switch { "air" => Tools.Pipes, "power" => Tools.Power, _ => null };
                Assert.Equal(own ?? air, Tools.OverlayShown(true, tab, airOn));
            }
    }

    [Fact]
    public void ReadsTheOverlayAnewFromThePlanItsTabAndAirNeverKeepingIt()
    {
        var ui = new Ui();
        var plan = new PlanHand(ui);
        ui.AirOn.Set(true);
        Assert.Equal(Tools.Air, ui.OverlayOn);
        plan.Toggle();
        plan.Tab.Set("air");
        Assert.Equal(Tools.Pipes, ui.OverlayOn);
        plan.Tab.Set("power");
        Assert.Equal(Tools.Power, ui.OverlayOn);
        plan.PutPlanAway();
        Assert.Equal(Tools.Air, ui.OverlayOn);
        ui.AirOn.Set(false);
        Assert.Null(ui.OverlayOn);
        // B again opens on the tab it was closed on.
        plan.Toggle();
        Assert.Equal(("power", Tools.Power), (plan.Tab.Get(), ui.OverlayOn));
    }

    [Fact]
    public void LetsACardGoWithAnotherTabAndPicksOneOntoItsOwnTab()
    {
        var ui = new Ui();
        var plan = new PlanHand(ui);
        plan.Toggle();
        plan.Pick(Card("lamp"));
        Assert.Equal(("power", "lamp"), (plan.Tab.Get(), What(plan.Selected.Get())));
        plan.Tab.Set("structure");
        Assert.Equal(((Tool?)null, Holding.Plan), (plan.Selected.Get(), ui.Hand.Get()));
        plan.Pick(Card("pipe"));
        Assert.Equal(("air", Tools.Pipes), (plan.Tab.Get(), ui.OverlayOn));
        // The same card again lets it go and keeps the tab.
        plan.Pick(Card("pipe"));
        Assert.Equal(((Tool?)null, "air"), (plan.Selected.Get(), plan.Tab.Get()));
    }

    [Fact]
    public void SaysWhatTheButtonsDoOnEveryTabInWords()
    {
        foreach (var tab in Tools.Tabs) Assert.True(En.Table.ContainsKey(Tools.HintOf(tab.Id)), tab.Id);
        Assert.NotEqual(Tools.HintOf("power"), Tools.HintOf("structure"));
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
        plan.Toggle();
        plan.Pick(Card("lamp"));
        Assert.Equal((Holding.Card, true), (ui.Hand.Get(), plan.PlanOut.Get()));
        plan.Pick(Card("cable"));
        Assert.Equal(Holding.Cable, ui.Hand.Get());
    }

    [Fact]
    public void PutsThePlanAwayWhenFreedFromElsewhereAHotbarSlotPicked()
    {
        var ui = new Ui();
        var plan = new PlanHand(ui);
        plan.Toggle();
        plan.Pick(Card("floor"));
        // 1–9 or the wheel with the menu open: the plan away, its overlay with it.
        ui.Hand.Set(Holding.Free);
        Assert.Equal((Holding.Free, false, (Tool?)null), (ui.Hand.Get(), plan.PlanOut.Get(), plan.Selected.Get()));
    }

    [Fact]
    public void RemovesAndPicksPipesAndMachinesOnTheAirTabAndWhatTheShipIsBuiltOfElsewhere()
    {
        Assert.Equal(["conduit", "machine"], Tools.LayersFor("pipes"));
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
