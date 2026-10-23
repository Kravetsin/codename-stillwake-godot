using Stillwake.Sim.Fauna;
using Xunit;
using Tune = Stillwake.Content.Fauna;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The ground under a creature's feet (0102, 0069): its body stands on the top of a slope's whole cell and steps whole cells up
/// and down, but its feet follow the slope's diagonal, never moving up or down faster than it walks across: what the client
/// draws, so a walk over a hill never snaps. Slopes do not double (0104): these maps are drawn a character a cell.
/// </summary>
public class FaunaFootingTests
{
    private const int Unit = FaunaModule.Unit;

    /// <summary>
    /// A room 40 wide, its floor row 15, with a hill of slopes six rows high on it, ◢ up from the left at column 12 and ◣ down to
    /// the right, its top four cells wide at row 9.
    /// </summary>
    private static readonly Layout Hill = Layout.Parse([.. Enumerable.Range(0, 16).Select(y => new string([.. Enumerable.Range(0, 40).Select(x =>
        y == 0 || y == 15 || x == 0 || x == 39 ? '#'
        : y < 9 ? '.'
        : x == 12 + 14 - y ? '/'
        : x == 22 + y - 9 ? '\\'
        : x > 12 + 14 - y && x < 22 + y - 9 ? '#' : '.')]))]);

    public static TheoryData<Mode> Modes() => [Mode.Wander, Mode.Flee];

    [Theory]
    [MemberData(nameof(Modes))]
    public void OverAHillAndBackItsFeetNeverRiseOrFallFasterThanItWalksThoughItsBodyStepsWholeCells(Mode mode)
    {
        var w = FaunaTesting.Planet(Hill, cells: true).HeroAt(1, 14);
        var c = w.Loper(2, 14).Walking(1, mode);
        int steps = 0, highest = int.MaxValue;
        void Walk(int way, Func<bool> until)
        {
            c.Walking(way, mode);
            for (int tick = 0; tick < 2_000 && !until(); tick++)
            {
                var (x, y, vy, feet) = (c.X, c.Y, c.VY, w.M.Fauna.FeetOf(c));
                w.Run();
                var now = w.M.Fauna.FeetOf(c);
                if (vy != 0 || c.VY != 0) continue;
                int across = Math.Abs(c.X - x);
                Assert.True(Math.Abs(now.Back - feet.Back) <= across + 1 && Math.Abs(now.Front - feet.Front) <= across + 1,
                    $"{mode} at {c.X},{c.Y}: feet {feet} to {now}, {across} across");
                Assert.Equal(0, (c.Y - y) % Unit);
                steps += Math.Abs(c.Y - y) / Unit;
                highest = Math.Min(highest, Math.Min(now.Back, now.Front));
            }
        }
        Walk(1, () => c.X >= 34 * Unit);
        Walk(-1, () => c.X <= 6 * Unit);
        Assert.InRange(c.X, 6 * Unit - Tune.Loper.Flee, 6 * Unit);
        // Over the hill's top, its feet on the top's row. Its body stepped whole cells: walking, up and down six rows each way;
        // fleeing, it bounds down off the steps, so only the climb is stepped.
        Assert.Equal(9 * Unit, highest);
        Assert.InRange(steps, mode == Mode.Wander ? 24 : 12, 24);
    }

    [Fact]
    public void OnASlopeAFootStandsOnTheDiagonalAndOverAnEdgeItHangsTheTeeterUnderTheBody()
    {
        var w = FaunaTesting.Planet("""
            ##########
            #........#
            #........#
            #........#
            #........#
            #........#
            ######\..#
            ######...#
            ######...#
            ######...#
            ######...#
            ######...#
            ##########
            """, cells: true).HeroAt(1, 5);
        // On the ledge, its front foot over the ◣ at its end: on the diagonal, as far down it as the foot is across the cell.
        var c = w.Loper(4, 5).Still();
        var (back, front) = w.M.Fauna.FeetOf(c);
        Assert.Equal(6 * Unit, back);
        int frontX = c.X + Tune.Loper.Width * Unit * Tune.FootSpan / 1000;
        Assert.Equal(6, frontX / Unit);
        Assert.InRange(front - 6 * Unit - frontX % Unit, -1, 1);
        // Stood on the slope's cell with its front foot past the ledge's end, over the drop: that foot hangs under it.
        var edge = w.Loper(5, 5).Still();
        Assert.Equal(6 * Unit, edge.Y);
        Assert.Equal(edge.Y + Tune.Teeter, w.M.Fauna.FeetOf(edge).Front);
    }
}
