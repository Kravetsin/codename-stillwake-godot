using Stillwake.Content;
using Stillwake.Game.Features.Pipes;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
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

    /// <summary>
    /// A vaporizer taking liquid at (0, 2) and giving air at (3, 0); a tank giving liquid at (8, 2); two vents taking air,
    /// (0, 4) and (8, 4) (liquid-oxygen).
    /// </summary>
    private static TestWorld Machines() => new(Layout.Parse(["GGGG....TTTT....", "GGGG....TTTT....", "GGGG....TTTT....", "GGGG....TTTT....", "VV......VV......", "VV......VV......"]));

    private static int[] LitFrom(TestWorld w, (double, double) press)
    {
        var hands = new PipeHands();
        Do(w, [new ButtonEvent(true, press)], hands: hands);
        return [.. hands.Lit.Order()];
    }

    [Fact]
    public void ADragLightsOnlyThePortsItsSpanMayEndAtNeverItsOwnMachines()
    {
        var w = Machines();
        // From the vaporizer's output: the vents, not the tank, whose liquid is another fluid, nor its own input.
        Assert.Equal([At(0, 4), At(8, 4)], LitFrom(w, Middle(2, 0)));
        // From its input: the tank only, the vents taking too and air besides.
        Assert.Equal([At(8, 2)], LitFrom(w, Middle(1, 2)));
        // From a vent: the vaporizer's output, never the tank: a tank piped to a vent is refused.
        Assert.Equal([At(3, 0)], LitFrom(w, Middle(0, 4)));
        // From the tank: the vaporizer's input.
        Assert.Equal([At(0, 2)], LitFrom(w, Middle(9, 2)));
        // From a bare cell: every port.
        Assert.Equal([At(3, 0), At(0, 2), At(8, 2), At(0, 4), At(8, 4)], LitFrom(w, Middle(5, 0)));
        var hands = new PipeHands();
        Do(w, [new ButtonEvent(true, Middle(1, 2)), new ButtonEvent(false, Middle(1, 2))], hands: hands);
        Assert.Empty(hands.Lit);
    }

    [Fact]
    public void ACisternsLeftHalfTakesItsInputItsRightItsOutputAndADragFromItNeverLightsItsOwn()
    {
        // Two cisterns: inputs (0, 2) and (8, 2), outputs (3, 2) and (11, 2).
        var w = new TestWorld(Layout.Parse(["cccc....cccc....", "cccc....cccc....", "cccc....cccc....", "cccc....cccc...."]));
        Assert.Equal(At(0, 2), PipeHands.CellAt(w.M.Grid, Middle(1, 0)));
        Assert.Equal(At(3, 2), PipeHands.CellAt(w.M.Grid, Middle(2, 3)));
        Assert.Equal([At(8, 2)], LitFrom(w, Middle(3, 2)));
        Assert.Equal([At(3, 2)], LitFrom(w, Middle(8, 2)));
    }

    [Fact]
    public void ADragLightsAPortBuiltWhileItLasts()
    {
        var w = World();
        var hands = new PipeHands();
        Do(w, [new ButtonEvent(true, Middle(10, 0))], hands: hands);
        Assert.Equal([At(0, 0)], hands.Lit);
        w.Step(new SetInstant(true));
        w.Step(new PlacePlan(At(12, 0), new Placement("machine", "vent")));
        hands.Update([], false, [], Middle(10, 0), w.M.Grid, w.M.Pipes, 0, _ => { });
        Assert.Equal([At(0, 0), At(12, 0)], hands.Lit.Order());
    }

    [Fact]
    public void ASocketPointsOutOfAMachineThatGivesAndIntoOneThatTakesInItsFluidsColour()
    {
        var tank = Stillwake.Content.Machines.Get("tank");
        var vaporizer = Stillwake.Content.Machines.Get("generator");
        var vent = Stillwake.Content.Machines.Get("vent");
        Assert.Equal((-1, 0), Sockets.ArrowOf(tank.Ports[0], tank.Width, tank.Height));
        // The vaporizer's liquid in on its left, its air out of its top right corner, sideways.
        Assert.Equal((1, 0), Sockets.ArrowOf(vaporizer.Ports[0], vaporizer.Width, vaporizer.Height));
        Assert.Equal((1, 0), Sockets.ArrowOf(vaporizer.Ports[1], vaporizer.Width, vaporizer.Height));
        Assert.Equal((1, 0), Sockets.ArrowOf(vent.Ports[0], vent.Width, vent.Height));
        // A port in a top or bottom row points up or down; one at the very middle, down when it gives.
        Assert.Equal((0, -1), Sockets.ArrowOf(new(1, 0, Way.Out, Fluids.Air), 4, 4));
        Assert.Equal((0, 1), Sockets.ArrowOf(new(1, 0, Way.In, Fluids.Air), 4, 4));
        Assert.Equal((0, 1), Sockets.ArrowOf(new(1, 1, Way.Out, Fluids.Air), 3, 3));
        Assert.Equal(0x7fc4f0u, Sockets.ColourOf(Fluids.Air));
        Assert.Equal(0x2fa3b8u, Sockets.ColourOf(Fluids.Water));
        Assert.Equal(0xa8d8ffu, Sockets.ColourOf(Fluids.LiquidOxygen));
        Assert.Equal(Fluids.All.Count, Fluids.All.Select(Sockets.ColourOf).Distinct().Count());
    }
}
