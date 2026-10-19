using Stillwake.Game.Features.Building;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Walls planned cut to a shape (0069, part 2): R in hand, the shape on every plan of a drag, how a cut wall joins.</summary>
public class PlanShapeTests
{
    private static PlaceTool Card(string id) => Tools.All.OfType<PlaceTool>().First(each => each.Id == id);

    private static PlanHand Holding(string id)
    {
        var plan = new PlanHand(new Ui());
        plan.Toggle();
        plan.Pick(Card(id));
        return plan;
    }

    [Fact]
    public void RTurnsAWallCardWholeThenThroughTheFourSlopesAndRoundShiftRBack()
    {
        var plan = Holding("wallScrap");
        var seen = new List<int> { plan.Shape.Get() };
        for (int i = 0; i < 5; i++)
        {
            Assert.True(plan.Turn(1));
            seen.Add(plan.Shape.Get());
        }
        Assert.Equal([Shape.None, Shape.BottomRight, Shape.BottomLeft, Shape.TopLeft, Shape.TopRight, Shape.None], seen);
        plan.Turn(-1);
        Assert.Equal(Shape.TopRight, plan.Shape.Get());
        Assert.Equal(Shape.TopRight, ((PlaceTool)plan.InHand!).Placement.Shape);
    }

    [Fact]
    public void RDoesNothingWithAnyCardButAHullWallsAndTheShapeSticksTillTurnedAgain()
    {
        var plan = Holding("wallIron");
        plan.Turn(1);
        foreach (string id in new[] { "floor", "ladder", "door", "backWallScrap" })
        {
            plan.Pick(Card(id));
            Assert.False(plan.Turn(1));
            // What it places is never cut.
            Assert.Equal(Shape.None, ((PlaceTool)plan.InHand!).Placement.Shape);
        }
        plan.Pick(Card("lamp"));
        Assert.False(plan.Turn(1));
        plan.Selected.Set(null);
        Assert.False(plan.Turn(1));
        // Another wall's card, any material, takes the shape turned to.
        plan.Pick(Card("wallScrap"));
        Assert.Equal(Placement.Wall("scrap") with { Shape = Shape.BottomRight }, ((PlaceTool)plan.InHand!).Placement);
        // The card itself stays the menu's own.
        Assert.Same(Card("wallScrap"), plan.Selected.Get());
    }

    [Fact]
    public void ADragPlansEveryCellOfItsLineCutToTheShapeInHand()
    {
        var w = new TestWorld(Layout.Parse(["#######", "#.....#", "#######"]));
        var tool = Tools.Shaped(Card("wallScrap"), Shape.TopLeft)!;
        var verdicts = Drag.Judge(tool, Drag.LineOf(7, tool, new Spot(1, 1), new Spot(5, 1)), w.M.Building);
        Assert.Equal(5, verdicts.Count);
        Assert.All(verdicts, v => Assert.Equal([new PlacePlan(v.Cell, Placement.Wall("scrap") with { Shape = Shape.TopLeft })], v.Commands));
    }

    [Fact]
    public void AWallJoinsAWallBesideItOnlyWhereThatOnesFacingSideIsWholePlannedOrBuilt()
    {
        // ◢ built left of the middle, ◣ right of it: their facing sides, right and left, are whole.
        var w = new TestWorld(Layout.Parse(["#####", "#/.\\#", "#...#", "#####"]));
        var wall = Placement.Wall();
        IEnumerable<Placement> None(int c) => [];
        Assert.Equal(Render.Blueprint.Up | Render.Blueprint.Right | Render.Blueprint.Left, Tools.LinksOf(wall, 7, 5, None, w.M.Grid));
        // Turned the other way the slopes face it with their open sides: nothing to run on into but the wall above.
        var turned = new TestWorld(Layout.Parse(["#####", "#\\./#", "#...#", "#####"]));
        Assert.Equal(Render.Blueprint.Up, Tools.LinksOf(wall, 7, 5, None, turned.M.Grid));
        // A planned ◤ below: its top is whole; a planned ◢ below is not.
        Assert.Equal(Render.Blueprint.Up | Render.Blueprint.Down,
            Tools.LinksOf(wall, 7, 5, c => c == 12 ? [wall with { Shape = Shape.TopLeft }] : [], turned.M.Grid));
        Assert.Equal(Render.Blueprint.Up,
            Tools.LinksOf(wall, 7, 5, c => c == 12 ? [wall with { Shape = Shape.BottomRight }] : [], turned.M.Grid));
    }

    [Fact]
    public void TellsABuiltWallsShapeAndThePipettePicksIt()
    {
        var w = new TestWorld(Layout.Parse(["#####", "#/.r#", "#####"]));
        Assert.Equal(Placement.Wall() with { Shape = Shape.BottomRight }, Tools.BuiltAt(6, "structure", w.M.Grid));
        Assert.Equal(Shape.TopLeft, Tools.ShapePicked(8, null, w.M.Grid, w.M.Building));
        Assert.Equal(Shape.None, Tools.ShapePicked(5, null, w.M.Grid, w.M.Building));
        Assert.Equal("wallScrap", Tools.CardAt(6, null, w.M.Grid, w.M.Building, w.M.Power, w.M.Research)?.Id);
    }
}
