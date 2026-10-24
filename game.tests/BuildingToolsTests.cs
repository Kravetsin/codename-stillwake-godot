using System.Text.RegularExpressions;
using Stillwake.Content;
using Stillwake.Game.Features.Building;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Research;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The build menu, the hand and the tab as the mode (0030): from apps/game/src/features/building/tools.test.ts.</summary>
public class BuildingToolsTests
{
    /// <summary>What a card puts in the world, by the name the grid gives it, a wall by its material.</summary>
    private static string? What(Tool? tool) => tool switch
    {
        null => null,
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
        Assert.All(Tools.Tabs.Where(tab => !MenuTabs.ShownEmpty.Contains(tab.Id)), tab => Assert.NotEmpty(tab.Tools));
        // A card a material but bedrock (0077, 0084) for its wall, and one for its back wall (0108).
        var expected = Materials.All.Where(m => m.Built).Select(m => $"wall:{m.Id}")
            .Concat(Materials.All.Where(m => Blueprints.Has(Placement.BackWall(m.Id))).Select(m => $"backWall:{m.Id}"))
            .Concat(Builds.All.Select(b => b.Id))
            .Concat(Machines.All.Where(m => !m.Fixed).Select(m => m.Kind));
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
    public void PricesEachCardAsTheSimDoes()
    {
        var q = new TestWorld("#.#").M.Building;
        Assert.Equal(Builds.Get("door").Cost, Tools.CostOf(Card("door"), q.CostFor));
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
    public void ShowsTheOpenTabsOverlayAndWithTheMenuClosedTheStrippersCablesOrAirIfWanted()
    {
        foreach (bool airOn in new[] { false, true })
            foreach (var tab in Tools.Tabs.Select(t => t.Id))
            {
                string? air = airOn ? Tools.Air : null;
                // Closed, whatever the tab: air or nothing.
                Assert.Equal(air, Tools.OverlayShown(false, tab, airOn));
                Assert.Equal(air, Tools.OverlayShown(false, tab, airOn, "fireAxe"));
                // The stripper held, the cables (0108), the wrench the pipes (0109); the plan out, air or nothing whatever is held.
                Assert.Equal(Tools.Power, Tools.OverlayShown(false, tab, airOn, HandTools.Stripper));
                Assert.Equal(Tools.Pipes, Tools.OverlayShown(false, tab, airOn, HandTools.Wrench));
                Assert.Equal(air, Tools.OverlayShown(true, tab, airOn));
                Assert.Equal(air, Tools.OverlayShown(true, tab, airOn, HandTools.Wrench));
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
        Assert.Equal(Tools.Air, ui.OverlayOn);
        plan.Tab.Set("power");
        Assert.Equal(Tools.Air, ui.OverlayOn);
        plan.PutPlanAway();
        Assert.Equal(Tools.Air, ui.OverlayOn);
        ui.Wielded.Set(HandTools.Stripper);
        Assert.Equal(Tools.Power, ui.OverlayOn);
        ui.Wielded.Set(HandTools.Wrench);
        Assert.Equal(Tools.Pipes, ui.OverlayOn);
        ui.Wielded.Set(null);
        ui.AirOn.Set(false);
        Assert.Null(ui.OverlayOn);
        // B again opens on the tab it was closed on.
        plan.Toggle();
        Assert.Equal(("power", (string?)null), (plan.Tab.Get(), ui.OverlayOn));
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
        plan.Pick(Card("vent"));
        Assert.Equal("air", plan.Tab.Get());
        // The same card again lets it go and keeps the tab.
        plan.Pick(Card("vent"));
        Assert.Equal(((Tool?)null, "air"), (plan.Selected.Get(), plan.Tab.Get()));
    }

    [Fact]
    public void SaysWhatTheButtonsDoForEveryCardInWords()
    {
        foreach (var card in Tools.All.Concat<Tool>(Tools.CardsOf(HandTools.Stripper)).Concat(Tools.CardsOf(HandTools.Wrench)))
        {
            Assert.True(En.Table.ContainsKey(Tools.HintOf(card)), card.Id);
            Assert.True(En.Table.ContainsKey($"tool.{card.Id}.about"), card.Id);
        }
        Assert.Equal("hint.build", Tools.HintOf(null));
        Assert.Equal("hint.valve", Tools.HintOf(Tools.Valve));
        foreach (var tool in (string[])[HandTools.Stripper, HandTools.Wrench]) Assert.True(En.Table.ContainsKey($"build.tab.{tool}"), tool);
    }

    [Fact]
    public void TheStripperOffersACableAndTheWrenchAPipeAndAValveEachAtItsCost()
    {
        Assert.Equal(["cable"], Tools.CardsOf(HandTools.Stripper).Select(c => c.Id));
        Assert.Equal(["pipe", "valve"], Tools.CardsOf(HandTools.Wrench).Select(c => c.Id));
        Assert.Empty(Tools.CardsOf(null));
        Assert.Empty(Tools.CardsOf("axe"));
        Func<Placement, IReadOnlyList<(string Item, int Count)>> none = _ => [];
        Assert.Equal(Stillwake.Content.Power.CableCost, Tools.CostOf(Tools.Cable, none));
        Assert.Equal([(Piping.Item, 1)], Tools.CostOf(Tools.Pipe, none));
        Assert.Equal(Piping.ValveCost, Tools.CostOf(Tools.Valve, none));
        // None of them is a plan card: the menu's tabs never hold them.
        Assert.DoesNotContain(Tools.All, t => t is HandCard);
    }

    [Fact]
    public void TheHandToolsMenuFollowsTheToolInHandItsFirstCardPickedAndGoneWithIt()
    {
        var ui = new Ui();
        var plan = new PlanHand(ui);
        Assert.Empty(plan.ToolCards);
        ui.Wielded.Set(HandTools.Wrench);
        Assert.Equal(Tools.Pipe, plan.ToolCard.Get());
        plan.PickToolCard(Tools.Valve);
        Assert.Equal(Tools.Valve, plan.ToolCard.Get());
        // A card another tool offers is not taken.
        plan.PickToolCard(Tools.Cable);
        Assert.Equal(Tools.Valve, plan.ToolCard.Get());
        ui.Wielded.Set(HandTools.Stripper);
        Assert.Equal(Tools.Cable, plan.ToolCard.Get());
        ui.Wielded.Set(null);
        Assert.Null(plan.ToolCard.Get());
        Assert.Empty(plan.ToolCards);
    }

    [Fact]
    public void TheMiddleButtonTogglesTheTwoLayersWithThePlanOutAndACardPickedSetsItsOwn()
    {
        Assert.Equal(Focus.Back, Tools.Toggled(Focus.Front));
        Assert.Equal(Focus.Front, Tools.Toggled(Focus.Back));
        var plan = new PlanHand(new Ui());
        Assert.Equal(Focus.Front, plan.Focus.Get());
        // With the plan away it toggles nothing.
        plan.ToggleFocus();
        Assert.Equal(Focus.Front, plan.Focus.Get());
        Assert.Null(plan.Shown);
        plan.Toggle();
        plan.ToggleFocus();
        Assert.Equal(Focus.Back, plan.Shown);
        plan.Pick(Card("wall:scrap"));
        Assert.Equal(Focus.Front, plan.Focus.Get());
        plan.Pick(Card("backWall:iron"));
        Assert.Equal(Focus.Back, plan.Focus.Get());
        plan.Pick(Card("door"));
        Assert.Equal(Focus.Front, plan.Focus.Get());
        // It toggles with a card in hand too: the card is kept for its layer and comes back with it, one card a layer.
        plan.Pick(Card("tank"));
        plan.ToggleFocus();
        Assert.Equal((Focus.Back, "backWall:iron"), (plan.Focus.Get(), What(plan.Selected.Get())));
        plan.ToggleFocus();
        Assert.Equal((Focus.Front, "tank"), (plan.Focus.Get(), What(plan.Selected.Get())));
        plan.FocusOn(Focus.Back);
        Assert.Equal((Focus.Back, "backWall:iron"), (plan.Focus.Get(), What(plan.Selected.Get())));
        // Put away, the plan forgets both.
        plan.Toggle();
        plan.Toggle();
        plan.ToggleFocus();
        Assert.Equal((Focus.Front, null), (plan.Focus.Get(), What(plan.Selected.Get())));
        // Only the focused layer is whole.
        Assert.Equal((1f, 1f), Tools.AlphasOf(null));
        Assert.Equal((Tools.Faded, 1f), Tools.AlphasOf(Focus.Back));
        Assert.Equal((1f, Tools.Faded), Tools.AlphasOf(Focus.Front));
    }

    [Fact]
    public void AwayFromTheShipNothingFadesAndTheHandToolsHaveNoMenu()
    {
        var ui = new Ui();
        var plan = new PlanHand(ui);
        // Aboard, the axe's dig fades the front round the pointer with the plan away, not with it out.
        Assert.True(plan.SeesThrough(digsBack: true));
        Assert.False(plan.SeesThrough(digsBack: false));
        ui.Wielded.Set(HandTools.Stripper);
        Assert.Equal(Tools.Cable, plan.ToolCard.Get());
        plan.Aboard.Set(false);
        Assert.Empty(plan.ToolCards);
        Assert.Null(plan.ToolCard.Get());
        Assert.False(plan.SeesThrough(digsBack: true));
        ui.Wielded.Set(null);
        plan.Toggle();
        plan.ToggleFocus();
        // The plan's layers turn away too; nothing fades.
        Assert.Equal(Focus.Back, plan.Focus.Get());
        Assert.Null(plan.Shown);
        Assert.False(plan.SeesThrough(digsBack: true));
        // A back wall's card still sets what the right button takes; the world draws whole.
        plan.Pick(Card("backWall:iron"));
        Assert.Equal(Focus.Back, plan.Focus.Get());
        Assert.Equal((1f, 1f), Tools.AlphasOf(plan.Shown));
        plan.Aboard.Set(true);
        Assert.Equal(Focus.Back, plan.Shown);
    }

    [Fact]
    public void HoldsThePlanWhileItIsOutACardOrNone()
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
    public void PicksInWhatTheShipIsBuiltOfAndMarksItsFrontOnTheRight()
    {
        Assert.Equal(["machine", "structure", "back"], Tools.Layers);
        Assert.Equal(["machine", "structure"], Tools.FrontLayers);
        // No pipe card: the wrench lays pipes (0109).
        Assert.DoesNotContain(Tools.All, t => t is PlaceTool { Placement.Layer: "conduit" });
    }

    [Fact]
    public void PicksTheCardOfWhatIsUnderThePointerAPlanFirstThenWhatIsBuiltInTheOverlaysLayers()
    {
        // Drawn a character a cell, the tank its whole 4×4 (0104); twelve cells wide: cell (x, y) is y * 12 + x. A tank at 13;
        // open cells behind walls; a cable along the top, from the hull's corner at 0 to 11.
        string[] rows = ["############", "#TTTT .....#", "#TTTT......#", "#TTTT......#", "#TTTT......#", "############"];
        var w = new TestWorld(Layout.Parse(rows, null, [((0, 0), (11, 0))]));
        string? Pick(int cell) => What(Tools.CardAt(cell, w.M.Grid, w.M.Building, w.M.Research));
        Assert.Equal($"wall:{w.M.Grid.WallAt(0)!.Id}", Pick(0));
        Assert.Equal("tank", Pick(14));
        // A back wall's card is its own (0108).
        Assert.Equal("backWall:scrap", Pick(18));
        // A ladder's piece, two by two (0104), from 19.
        w.Step(new PlacePlan(19, new Placement("structure", "ladder")));
        Assert.Equal("ladder", Pick(32));
    }

    [Fact]
    public void ShowsAndPicksOnlyTheCardsTheWorldKnows()
    {
        // The tank and the wall of the scrap the hull is of, in a world knowing only its start.
        var w = new TestWorld(Layout.Parse(["############", "#TTTT .....#", "#TTTT......#", "#TTTT......#", "#TTTT......#", "############"])
            with
        { Known = Research.StartKnown });
        var known = Tools.All.Where(each => Tools.Known(each, w.M.Research)).Select(What).ToList();
        Assert.Contains("furnace", known);
        Assert.Contains("analyzer", known);
        Assert.Contains("crate", known);
        Assert.False(Tools.Known(Card("tank"), w.M.Research));
        Assert.Null(Tools.CardAt(14, w.M.Grid, w.M.Building, w.M.Research));
        Assert.Equal("wall:scrap", What(Tools.CardAt(0, w.M.Grid, w.M.Building, w.M.Research)));
    }

    [Fact]
    public void EveryCardPlansOnTheLeftAndTheRightRemovesInTheFocusedLayerCardOrNone()
    {
        // The hands: the right button alone is the back; both held, the front (the axe's and the drill's).
        Assert.False(Layering.Back(left: true, right: false));
        Assert.True(Layering.Back(left: false, right: true));
        Assert.False(Layering.Back(left: true, right: true));
        Assert.False(Layering.Back(left: false, right: false));
        foreach (var card in Tools.All.OfType<PlaceTool>())
            foreach (var focus in Enum.GetValues<Focus>())
            {
                // The left places a card of the focused layer only (0110).
                var left = Tools.ActOf(card, false, focus);
                if (Tools.FocusOf(card) == focus) Assert.Equal(card.Placement, Assert.IsType<PlaceTool>(left).Placement);
                else Assert.Null(left);
                var removal = Assert.IsType<Remove>(Tools.ActOf(card, true, focus));
                Assert.Equal(Tools.RemovalIn(focus).Layers, removal.Layers);
            }
        Assert.Null(Tools.ActOf(null, false, Focus.Front));
        // With no card in hand the right still removes: in the focused layer.
        Assert.Equal(["back"], Assert.IsType<Remove>(Tools.ActOf(null, true, Focus.Back)).Layers);
        Assert.Equal(Tools.FrontLayers, Tools.RemovalIn(Focus.Front).Layers);
        // No removal card and no cable card: the stripper lays cables (0108).
        Assert.Empty(Tools.Matching(Tools.All, "remove"));
        Assert.DoesNotContain(Tools.Tabs.Single(t => t.Id == "power").Tools, t => t.Id == "cable");
        Assert.Null(Tools.Tabs.Single(t => t.Id == "power").Overlay);
    }

    [Fact]
    public void TheRightTakesOnlyTheFocusedLayerWhateverIsUnderWhereTheDragBegan()
    {
        // A tank (4×4) on scrap back walls, walls round it, a ladder at 19 (two by two).
        var w = new TestWorld(Layout.Parse(["############", "#TTTT .HH..#", "#TTTT..HH..#", "#TTTT......#", "#TTTT......#", "############"]));
        Sim.Command[] Judged(Focus focus, int from, int to) =>
            [.. Drag.Judge(Tools.RemovalIn(focus), Drag.CellsOf(12, Tools.RemovalIn(focus), new(from % 12, from / 12), new(to % 12, to / 12)),
                w.M.Building).SelectMany(v => v.Commands)];
        // Begun on the wall at 0, the front takes the walls, the tank and the ladder in the rectangle, each piece once, never a back wall.
        var front = Judged(Focus.Front, 0, 33).Cast<Deconstruct>().ToList();
        Assert.Contains(new Deconstruct(0, "structure"), front);
        Assert.Contains(new Deconstruct(13, "machine"), front);
        Assert.Contains(new Deconstruct(19, "structure"), front);
        Assert.Equal(front.Count, front.Distinct().Count());
        Assert.DoesNotContain(front, d => d.Layer == "back" || d.Cell is 20 or 31 or 32);
        // The back walls behind the tank, never the tank.
        Assert.Equal([new Deconstruct(13, "back"), new Deconstruct(14, "back")], Judged(Focus.Back, 13, 14));
        // On a back-wall mark only the back's focus cancels it; the front's takes the tank instead.
        w.Step(new Deconstruct(13, "back"));
        Assert.Equal([new Deconstruct(13, "machine")], Judged(Focus.Front, 13, 13));
        Assert.Single(Judged(Focus.Back, 13, 13).OfType<CancelPlan>());
    }

    [Fact]
    public void WithTheBackFocusedAFrontCardIsNotInHandAndPlacesNothing()
    {
        // The bug: the back focused by the middle button, a wall's card still placed walls the right could not take.
        var plan = new PlanHand(new Ui());
        plan.Toggle();
        plan.Pick(Card("wall:scrap"));
        plan.ToggleFocus();
        Assert.Equal((Focus.Back, null), (plan.Focus.Get(), What(plan.Selected.Get())));
        Assert.Null(Tools.ActOf(Card("wall:scrap"), false, Focus.Back));
        Assert.Equal(["back"], Assert.IsType<Remove>(Tools.ActOf(plan.Selected.Get(), true, plan.Focus.Get())).Layers);
    }

    [Fact]
    public void AfterAWallAndABackWallTheWallComesOffWithTheFrontFocusedAgain()
    {
        // The bug: a back wall's card picked after a wall's, the wall could not be taken without its card picked again.
        var w = new TestWorld(Layout.Parse(["############", "#..........#", "############"]));
        var plan = new PlanHand(new Ui());
        plan.Toggle();
        plan.Pick(Card("wall:scrap"));
        plan.Pick(Card("backWall:scrap"));
        Sim.Command[] Right() =>
            [.. Drag.Judge(Tools.ActOf(plan.Selected.Get(), true, plan.Focus.Get())!, [0], w.M.Building).SelectMany(v => v.Commands)];
        Assert.DoesNotContain(new Deconstruct(0, "structure"), Right());
        plan.ToggleFocus();
        Assert.Equal((Focus.Front, "wall:scrap"), (plan.Focus.Get(), What(plan.Selected.Get())));
        Assert.Equal([new Deconstruct(0, "structure")], Right());
    }
}
