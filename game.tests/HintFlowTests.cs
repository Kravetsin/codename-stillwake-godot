using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A hint line laid out (build-hints): its presses joined by "+", its words flowed into rows, the rows after the first indented.</summary>
public class HintFlowTests
{
    private const float Press = 10;

    private static List<HintPiece> Pieces(KeyHint line, string words) => HintFlow.PiecesOf(line, words, _ => Press, w => w.Length);

    [Fact]
    public void JoinsPressesHeldTogetherByAPlusButNotAnItemToTake()
    {
        var held = Pieces(new KeyHint([new CapPress("Ctrl"), new MousePress(Mouse.LeftDrag)], "x"), "move peg");
        Assert.Equal([false, true, false, false, false], held.Select(p => p.Plus).ToArray());
        Assert.Equal(["move", "peg"], held.Skip(3).Select(p => p.Word!).ToArray());

        var item = Pieces(new KeyHint([new CapPress("1"), new ItemPress("screwdriver"), new MousePress(Mouse.LeftHold)], "x"), "build");
        Assert.DoesNotContain(item, p => p.Plus);
    }

    [Fact]
    public void FlowsWordsOnToAnIndentedRowWhenTheyDoNotFit()
    {
        // A press of 10, then words of 5: 10 + 3 + 5 = 18 fits in 20; the next word goes down, indented.
        var placed = HintFlow.Flow(Pieces(new KeyHint([new MousePress(Mouse.Left)], "x"), "aaaaa bbbbb"), 20, 4);
        Assert.Equal([(0, 0f), (0, 13f), (1, 4f)], placed.Select(p => (p.Row, p.X)).ToArray());
    }

    [Fact]
    public void KeepsAPlusWithThePressAfterIt()
    {
        // Ctrl (10) + "+" (1) + drag (10) is 23 with its gaps: in 20 the plus and the drag go down together.
        var placed = HintFlow.Flow(Pieces(new KeyHint([new CapPress("Ctrl"), new MousePress(Mouse.LeftDrag)], "x"), ""), 20, 4);
        Assert.Equal([0, 1, 1], placed.Select(p => p.Row).ToArray());
        Assert.Equal(4f, placed[1].X);
        Assert.Equal(4f + 1 + HintFlow.PlusGap, placed[2].X);
    }

    [Fact]
    public void PlacesAPieceTooWideForAnyRowOnARowOfItsOwn()
    {
        var placed = HintFlow.Flow(Pieces(new KeyHint([new MousePress(Mouse.Left)], "x"), "aaaaaaaaaaaaaaaaaaaaaaaaa"), 20, 4);
        Assert.Equal([(0, 0f), (1, 4f)], placed.Select(p => (p.Row, p.X)).ToArray());
    }
}
