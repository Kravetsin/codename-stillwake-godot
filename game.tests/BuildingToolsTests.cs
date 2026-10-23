using System.Text.RegularExpressions;
using Stillwake.Content;
using Stillwake.Game.Features.Building;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;
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
        RemoveTool r => $"remove:{r.Tab}",
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
        // A card a material but bedrock (0077, 0084), its wall and its back wall (0107); a removal card a tab.
        var expected = Materials.All.Where(m => m.Built).Select(m => $"wall:{m.Id}")
            .Concat(Tools.Tabs.Select(t => $"remove:{t.Id}"))
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
        // Drawn a character a cell, the tank its whole 4×4 (0104); twelve cells wide: cell (x, y) is y * 12 + x. A tank at 13;
        // a pipe at 18; open cells behind walls; a cable along the top, from the hull's corner at 0 to 11.
        string[] rows = ["############", "#TTTT -....#", "#TTTT......#", "#TTTT......#", "#TTTT......#", "############"];
        var w = new TestWorld(Layout.Parse(rows, null, [((0, 0), (11, 0))]));
        string? Pick(int cell, string? overlay = null) =>
            What(Tools.CardAt(cell, overlay, w.M.Grid, w.M.Building, w.M.Power, w.M.Research));
        Assert.Equal($"wall:{w.M.Grid.WallAt(0)!.Id}", Pick(0));
        Assert.Equal("tank", Pick(14));
        // A back wall's card is its material's (0107).
        Assert.Equal("wall:scrap", Pick(18));
        Assert.Equal("pipe", Pick(18, "pipes"));
        Assert.Null(Pick(17, "pipes"));
        // A ladder's piece, two by two (0104), from 19.
        w.Step(new PlacePlan(19, new Placement("structure", "ladder")));
        Assert.Equal("ladder", Pick(32));
        // Under the power overlay, the cable on a node a cable ends at; nothing on one with none.
        Assert.Equal("cable", Pick(0, Tools.Power));
        Assert.Null(Pick(24, Tools.Power));
    }

    [Fact]
    public void ShowsAndPicksOnlyTheCardsTheWorldKnowsTheCableAlways()
    {
        // The tank and the wall of the scrap the hull is of, in a world knowing only its start.
        var w = new TestWorld(Layout.Parse(["############", "#TTTT .....#", "#TTTT......#", "#TTTT......#", "#TTTT......#", "############"])
            with
        { Known = Research.StartKnown });
        var known = Tools.All.Where(each => Tools.Known(each, w.M.Research)).Select(What).ToList();
        Assert.Contains("cable", known);
        Assert.Contains("furnace", known);
        Assert.Contains("analyzer", known);
        Assert.Contains("crate", known);
        Assert.False(Tools.Known(Card("tank"), w.M.Research));
        Assert.Null(Tools.CardAt(14, null, w.M.Grid, w.M.Building, w.M.Power, w.M.Research));
        Assert.Equal("wall:scrap", What(Tools.CardAt(0, null, w.M.Grid, w.M.Building, w.M.Power, w.M.Research)));
    }

    [Fact]
    public void TheLeftButtonIsTheFrontLayerAndTheRightTheBackOnlyEverythingElseFrontOnly()
    {
        // The hands: the right button alone is the back; both held, the front.
        Assert.False(Layering.Back(left: true, right: false));
        Assert.True(Layering.Back(left: false, right: true));
        Assert.False(Layering.Back(left: true, right: true));
        Assert.False(Layering.Back(left: false, right: false));
        // A material's card: its wall on the left, its back wall of the same material on the right.
        foreach (var card in Tools.All.OfType<PlaceTool>().Where(c => c.Placement.Kind == "wall"))
        {
            Assert.Equal(card.Placement, Assert.IsType<PlaceTool>(Tools.ActOf(card, right: false, null)).Placement);
            var back = Assert.IsType<PlaceTool>(Tools.ActOf(card, right: true, null));
            Assert.Equal(Placement.BackWall(card.Placement.Material!), back.Placement);
        }
        // Doors, ladders, floors, pipes and machines are front only: the right button does nothing with them; nor the cable.
        foreach (var kind in (string[])["door", "ladder", "floor", "pipe", "tank", "furnace", "cable"])
            Assert.Null(Tools.ActOf(Card(kind), right: true, null));
        Assert.Equal("door", Assert.IsType<PlaceTool>(Tools.ActOf(Card("door"), right: false, null)).Placement.Kind);
        Assert.Null(Tools.ActOf(Card("cable"), right: false, Tools.Power));
        Assert.Null(Tools.ActOf(null, right: false, null));
    }

    [Fact]
    public void TheRemovalCardMarksTheTabsFrontLayersOnTheLeftAndTheBackWallsOnTheRight()
    {
        var removal = Tools.Tabs.Select(t => Assert.IsType<RemoveTool>(t.Tools[0])).ToList();
        Assert.Equal(Tools.Tabs.Select(t => t.Id), removal.Select(r => r.Tab));
        Assert.All(removal, r => Assert.Equal(Tools.TabOf(r), r.Tab));
        var card = removal[0];
        Assert.Equal(["machine", "structure"], Assert.IsType<Remove>(Tools.ActOf(card, right: false, null)).Layers);
        Assert.Equal(["conduit", "machine"], Assert.IsType<Remove>(Tools.ActOf(card, right: false, Tools.Pipes)).Layers);
        Assert.Equal(["back"], Assert.IsType<Remove>(Tools.ActOf(card, right: true, null)).Layers);
        Assert.Equal(["back"], Assert.IsType<Remove>(Tools.ActOf(card, right: true, Tools.Pipes)).Layers);
        // Free, known everywhere, and its own hint.
        Assert.Empty(Tools.CostOf(card, _ => throw new InvalidOperationException("a removal places nothing")));
        Assert.True(Tools.Allowed(card, Surfaces.TestStrip));
        Assert.Equal("hint.remove", Tools.HintOf("structure", card));
        Assert.Equal("hint.removePower", Tools.HintOf("power", removal[2]));
        Assert.True(En.Table.ContainsKey("hint.remove") && En.Table.ContainsKey("hint.removePower"));
        // Searched for, one of them.
        Assert.Single(Tools.Matching(Tools.All, "remove"));
    }

    [Fact]
    public void TheRemovalCardsLeftTakesTheFrontAndItsRightTheBackWallsBehindAMachineToo()
    {
        // A tank (4×4) on scrap back walls, a wall above it.
        var w = new TestWorld(Layout.Parse(["############", "#TTTT .....#", "#TTTT......#", "#TTTT......#", "#TTTT......#", "############"]));
        var card = Tools.Tabs[0].Tools.OfType<RemoveTool>().Single();
        Sim.Command[] Judged(bool right, int from, int to) =>
            [.. Drag.Judge(Tools.ActOf(card, right, null)!, Drag.CellsOf(12, Tools.ActOf(card, right, null)!, new(from % 12, from / 12), new(to % 12, to / 12)),
                w.M.Building).SelectMany(v => v.Commands)];
        // Begun on the tank, the left marks it once; the right, the back walls behind it, never the tank.
        Assert.Equal([new Deconstruct(13, "machine")], Judged(right: false, 13, 14));
        Assert.Equal([new Deconstruct(13, "back"), new Deconstruct(14, "back")], Judged(right: true, 13, 14));
        // On a back-wall plan, only the right cancels it; the left begun there marks the front.
        w.Step(new Deconstruct(13, "back"));
        Assert.Empty(Judged(right: false, 13, 13).OfType<CancelPlan>());
        Assert.Single(Judged(right: true, 13, 13).OfType<CancelPlan>());
    }
}
