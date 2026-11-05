using Stillwake.Game.Features.Building;
using Stillwake.Game.I18n;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The plan's hint (build-hints): lines by what is in hand, a press and a few words each.</summary>
public class BuildHintsTests
{
    private static Tool Card(string id) => Tools.All.First(t => t.Id == id);

    private static HandNow Now(PlanMode mode = PlanMode.Build, Tool? held = null, bool layers = true, Focus focus = Focus.Front,
        bool typing = false, bool planned = false, Builder? builder = null) => new(mode, layers, focus, held, typing, planned, builder);

    private static string[] Says(HandNow now) => Hints.LinesOf(now).Select(l => l.Says).ToArray();

    [Fact]
    public void WithNothingInHandSaysPickTakeDownTheOtherLayerAndClose()
    {
        Assert.Equal(["hint.pick", "hint.takeDown", "hint.toBack", "hint.close"], Says(Now()));
        Assert.Equal("hint.toFront", Says(Now(focus: Focus.Back))[2]);
        var lines = Hints.LinesOf(Now());
        Assert.Equal(new MousePress(Mouse.Left), lines[0].Presses.Single());
        Assert.Equal(new MousePress(Mouse.RightDrag), lines[1].Presses.Single());
        Assert.Equal(new MousePress(Mouse.Middle), lines[2].Presses.Single());
        Assert.Equal(new KeyPress(Bind.Build), lines[3].Presses.Single());
    }

    [Fact]
    public void OneLayerOffTheShipOrOnAPlanetHasNoLayerLine() =>
        Assert.Equal(["hint.pick", "hint.takeDown", "hint.close"], Says(Now(layers: false)));

    [Fact]
    public void ACardSaysHowItsDragLaysIt()
    {
        Assert.Equal(["hint.place", "hint.fill", "hint.takeDown", "hint.close"], Says(Now(held: Card("wallScrap"))));
        Assert.Equal("hint.fill", Says(Now(held: Card("backWallScrap")))[1]);
        Assert.Equal("hint.row", Says(Now(held: Card("ladder")))[1]);
        Assert.Equal("hint.row", Says(Now(held: Card("floor")))[1]);
        // A machine goes where the click is: no drag to tell.
        Assert.Equal(["hint.place", "hint.takeDown", "hint.close"], Says(Now(held: Card("lamp"))));
    }

    [Fact]
    public void WiringAndPipingSayTheirButtons()
    {
        Assert.Equal(["hint.cable", "hint.peg", "hint.cut", "hint.close"], Says(Now(PlanMode.Wiring, Tools.Cable)));
        Assert.Equal([new CapPress("Ctrl"), new MousePress(Mouse.LeftDrag)], Hints.LinesOf(Now(PlanMode.Wiring, Tools.Cable))[1].Presses);
        Assert.Equal(["hint.pipe", "hint.pipeOff", "hint.close"], Says(Now(PlanMode.Piping, Tools.Pipe)));
        Assert.Equal(["hint.valve", "hint.valveOff", "hint.close"], Says(Now(PlanMode.Piping, Tools.Valve)));
    }

    [Fact]
    public void WhilePlansStandTheLastLineSaysToTakeTheBuilder()
    {
        var line = Hints.LinesOf(Now(planned: true, builder: new Builder("screwdriver", 1)))[^1];
        Assert.Equal("hint.build", line.Says);
        Assert.Equal([new CapPress("1"), new ItemPress("screwdriver"), new MousePress(Mouse.LeftHold)], line.Presses);
        Assert.False(line.Short);
        // In the bag, no digit; none carried, red.
        Assert.Equal([new ItemPress("powerDriver"), new MousePress(Mouse.LeftHold)], Hints.LinesOf(Now(planned: true, builder: new Builder("powerDriver", null)))[^1].Presses);
        var none = Hints.LinesOf(Now(planned: true))[^1];
        Assert.Equal(("hint.noBuilder", true), (none.Says, none.Short));
    }

    [Fact]
    public void TypingASearchSaysHowToEndIt() =>
        Assert.Equal(["hint.typed", "hint.clear"], Says(Now(typing: true, planned: true)));

    [Fact]
    public void NoStateSaysMoreThanFourLines()
    {
        foreach (var mode in Tools.Modes)
            foreach (var held in Tools.All.Concat<Tool?>(Tools.CardsOf(mode)).Append(null))
                foreach (bool planned in (bool[])[false, true])
                    Assert.InRange(Hints.LinesOf(Now(mode, held, planned: planned)).Count, 1, 4);
    }

    [Fact]
    public void TheBuilderIsThePowerDriverFirstAndTheHotbarsSlotFromOne()
    {
        Assert.Equal(new Builder("screwdriver", 3), Hints.BuilderOf([null, "wire", "screwdriver"], hotbar: 9));
        Assert.Equal(new Builder("powerDriver", null), Hints.BuilderOf(["screwdriver", .. Enumerable.Repeat<string?>(null, 9), "powerDriver"], hotbar: 9));
        Assert.Null(Hints.BuilderOf(["wire"], hotbar: 9));
    }

    [Fact]
    public void ThePointerSaysWhatTheRightOrDropDoesOverIt()
    {
        Assert.Equal(("hint.cancel", (Press)new MousePress(Mouse.Right)), Pair(Hints.PointerOf(PlanMode.Build, Over.Plan)));
        Assert.Equal(("hint.same", (Press)new KeyPress(Bind.Drop)), Pair(Hints.PointerOf(PlanMode.Build, Over.Built)));
        Assert.Null(Hints.PointerOf(PlanMode.Build, Over.Nothing));
        Assert.Null(Hints.PointerOf(PlanMode.Piping, Over.Built));
    }

    [Fact]
    public void EveryLineHasWordsInBothLanguages()
    {
        var lines = Tools.Modes.SelectMany(mode => Tools.All.Concat<Tool?>(Tools.CardsOf(mode)).Append(null)
            .SelectMany(held => Hints.LinesOf(Now(mode, held, planned: true)).Concat(Hints.LinesOf(Now(mode, held))).Concat(Hints.LinesOf(Now(mode, held, typing: true)))))
            .Concat(Hints.LinesOf(Now(planned: true, builder: new Builder("screwdriver", 1))))
            .Concat(Enum.GetValues<Over>().SelectMany(o => Tools.Modes.Select(m => Hints.PointerOf(m, o))).OfType<KeyHint>());
        foreach (string says in lines.Select(l => l.Says).Distinct())
        {
            Assert.True(En.Table.ContainsKey(says), says);
            Assert.True(Ru.Table.ContainsKey(says), says);
        }
    }

    private static (string, Press) Pair(KeyHint? line) => (line!.Says, line.Presses.Single());
}
