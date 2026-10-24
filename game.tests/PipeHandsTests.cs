using Stillwake.Game.Features.Pipes;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The piping's hands (0109): a left drag lays a span, a right press takes a span away; the valve card makes and takes valves (0110).</summary>
public class PipeHandsTests
{
    // Drawn a cell a character, sixteen wide: a vent (2×2) at (0, 0), its port its top-left.
    private const int W = 16;
    private static int At(int x, int y) => y * W + x;
    private static (double, double) Middle(int x, int y) => (x + 0.5, y + 0.5);

    private static TestWorld World()
    {
        var w = new TestWorld(Layout.Parse(["VV..............", "VV..............", "................", "................"]));
        w.Step(new SetCreative(true));
        return w;
    }

    private static List<Command> Do(TestWorld w, IReadOnlyList<ButtonEvent> left, bool valves = false, IReadOnlyList<ButtonEvent>? cuts = null, PipeHands? hands = null)
    {
        var sent = new List<Command>();
        (hands ?? new PipeHands()).Update(left, valves, cuts ?? [], left.Count > 0 ? left[^1].At : null, w.M.Grid, w.M.Pipes, 0, sent.Add);
        return sent;
    }

    [Fact]
    public void ADragLaysASpanFromAMachinesPortAnyAngleToTheCellLetGo()
    {
        var w = World();
        // Pressed on the vent's far cell: its port, the nearest it has.
        Assert.Equal(At(0, 0), PipeHands.CellAt(w.M.Grid, Middle(1, 1)));
        var hands = new PipeHands();
        Assert.Empty(Do(w, [new ButtonEvent(true, Middle(1, 1))], hands: hands));
        hands.Update([], false, [], Middle(6, 3), w.M.Grid, w.M.Pipes, 0, _ => { });
        Assert.Equal((At(0, 0), At(6, 3), (string?)null), hands.Laying);
        Assert.Equal([new LayPipe(At(0, 0), At(6, 3))], Do(w, [new ButtonEvent(false, Middle(6, 3))], hands: hands));
        Assert.Null(hands.Laying);
    }

    [Fact]
    public void WithTheValveCardTheLeftMakesAValveOnAJointAndTheRightTakesItOffNeverASpan()
    {
        var w = World();
        w.Step(new LayPipe(At(0, 0), At(5, 0)));
        var press = new ButtonEvent(true, Middle(5, 0));
        Assert.Equal([new MakeValve(At(5, 0), true)], Do(w, [press, new ButtonEvent(false, Middle(5, 0))], valves: true));
        // No valve there yet: the right takes nothing, the span stays.
        Assert.Empty(Do(w, [], valves: true, cuts: [press]));
        w.Step(new MakeValve(At(5, 0), true));
        // A valve already: the left makes no second; the right takes it off.
        Assert.Empty(Do(w, [press], valves: true));
        Assert.Equal([new MakeValve(At(5, 0), false)], Do(w, [], valves: true, cuts: [press]));
    }

    [Fact]
    public void HoveringASpanWithThePipeCardLightsTheWholeSpanTheRightWouldTake()
    {
        var w = World();
        w.Step(new LayPipe(At(0, 0), At(6, 3)));
        var hands = new PipeHands();
        hands.Update([], false, [], Middle(3, 1), w.M.Grid, w.M.Pipes, 0, _ => { });
        Assert.Equal((At(0, 0), At(6, 3)), (hands.Hovered!.A, hands.Hovered.B));
        hands.Update([], true, [], Middle(3, 1), w.M.Grid, w.M.Pipes, 0, _ => { });
        Assert.Null(hands.Hovered);
        hands.Update([], false, [], Middle(12, 0), w.M.Grid, w.M.Pipes, 0, _ => { });
        Assert.Null(hands.Hovered);
    }


    [Fact]
    public void ARightPressTakesAwayTheSpanNearItAndNoneFurtherOff()
    {
        var w = World();
        w.Step(new LayPipe(At(0, 0), At(6, 3)));
        var near = (3.5, 2.0);
        Assert.True(PipeHands.DistanceTo(At(0, 0), At(6, 3), W, near) <= PipeHands.CutReach);
        Assert.Null(PipeHands.Nearest(w.M.Pipes.Spans, W, (10.5, 0.5)));
        Assert.Equal([new CutPipe(At(0, 0), At(6, 3))], Do(w, [], cuts: [new ButtonEvent(true, near)]));
    }

    [Fact]
    public void LaysNothingTheSimWouldRefuseAndSaysWhy()
    {
        var w = World();
        var hands = new PipeHands();
        Do(w, [new ButtonEvent(true, Middle(0, 0))], hands: hands);
        Assert.Empty(Do(w, [new ButtonEvent(false, Middle(15, 3))], hands: hands));
        Do(w, [new ButtonEvent(true, Middle(0, 0)), new ButtonEvent(false, Middle(15, 3)), new ButtonEvent(true, Middle(0, 0))], hands: hands);
        hands.Update([], false, [], Middle(15, 3), w.M.Grid, w.M.Pipes, 0, _ => { });
        Assert.Equal("pipes.tooLong", hands.Laying?.Why);
    }
}
