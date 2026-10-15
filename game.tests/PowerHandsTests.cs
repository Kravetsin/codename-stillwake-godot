using Stillwake.Game.Features.Power;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;
using Xunit;
using Move = Stillwake.Sim.Power.Move;

namespace Stillwake.Game.Tests;

/// <summary>The hands on cables on the power tab (0030): no card carries, the cable card lays, the middle button cuts.</summary>
public class PowerHandsTests
{
    // Nine wide: a lamp at (1, 2) cabled to a peg on the ceiling at (4, 1).
    private const int W = 9;
    private static int At(int x, int y) => y * W + x;

    private static TestWorld World() => new(Layout.Parse(
        ["         ", "#########", "#L......#", "#########"], null, [((1, 2), (4, 1))]));

    private static List<Line> Lines(IGridQueries grid, IPowerQueries power) =>
        power.Cables.Select(c => new Line(c.A, c.B, [CableShape.EndOf(grid, c.A), CableShape.EndOf(grid, c.B)])).ToList();

    /// <summary>A left press at <paramref name="from"/> and a release at <paramref name="to"/>, then what was sent.</summary>
    private static List<Command> Drag(TestWorld w, Holding holds, (double X, double Y) from, (double X, double Y) to)
    {
        var hands = new PowerHands();
        var buttons = new Buttons();
        var sent = new List<Command>();
        var lines = Lines(w.M.Grid, w.M.Power);
        buttons.OnPointer(true, Buttons.Left, from);
        hands.Update(holds, buttons, [], from, lines, w.M.Grid, w.M.Power, 0, sent.Add, 16);
        buttons.OnPointer(false, 0, to);
        hands.Update(holds, buttons, [], to, lines, w.M.Grid, w.M.Power, 0, sent.Add, 16);
        return sent;
    }

    [Fact]
    public void CarriesAPegWithNoCardInHandAndLaysNothing()
    {
        var w = World();
        var sent = Drag(w, Holding.Plan, CableShape.EndOf(w.M.Grid, At(4, 1)), (6.5, 1.5));
        var move = Assert.IsType<Move>(Assert.Single(sent));
        Assert.Equal((At(4, 1), At(6, 1)), (move.Node, move.To));
    }

    [Fact]
    public void LaysFromWhatIsPressedWithTheCableCardEvenOnAPeg()
    {
        var w = World();
        w.Step(new SetCreative(true));
        var sent = Drag(w, Holding.Cable, CableShape.EndOf(w.M.Grid, At(4, 1)), (6.5, 1.5));
        var lay = Assert.IsType<Lay>(Assert.Single(sent));
        Assert.Equal((At(4, 1), At(6, 1)), (lay.A, lay.B));
    }

    [Fact]
    public void CutsTheCableAMiddlePressIsNearAndNoneFurtherOff()
    {
        var w = World();
        var lines = Lines(w.M.Grid, w.M.Power);
        var line = Assert.Single(lines);
        var mid = ((line.Points[0].X + line.Points[1].X) / 2, (line.Points[0].Y + line.Points[1].Y) / 2);
        Assert.NotNull(PowerHands.Nearest(lines, mid));
        Assert.Null(PowerHands.Nearest(lines, (mid.Item1, mid.Item2 + 1)));
        var sent = new List<Command>();
        new PowerHands().Update(Holding.Plan, new Buttons(), [new ButtonEvent(true, mid)], mid, lines, w.M.Grid, w.M.Power, 0, sent.Add, 16);
        Assert.Equal(new Cut(line.A, line.B), Assert.Single(sent));
    }
}
