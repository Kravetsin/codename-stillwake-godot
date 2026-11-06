using Stillwake.Content;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Landing;
using Xunit;
using Hero = Stillwake.Game.Features.Crew.Hero;
using Spec = Stillwake.Content.Hero;

namespace Stillwake.Game.Tests;

/// <summary>
/// The treadmill wheel in the client (power-chain): E gets in, seated it holds the hero on its lowest tread and says how to run,
/// and a move key held is what the hero reports as running.
/// </summary>
public class TreadWheelTests
{
    /// <summary>A room four characters tall, the wheel standing on its floor.</summary>
    private static readonly string[] Rows =
    [
        "##########",
        "#........#",
        "#.www....#",
        "#.www....#",
        "#.www....#",
        "##########",
    ];

    private const int Width = 20;
    private static int At(int x, int y) => y * Width + x;

    /// <summary>A hero standing on the floor beside the wheel, its feet on cell (11, 9)'s bottom.</summary>
    private static readonly Hero Beside = new(11 * Hero.Unit, 10 * Hero.Unit, 0, 0, false, 1);

    private static (TestWorld W, int Wheel) World()
    {
        var w = new TestWorld(Layout.Parse(Rows).Doubled());
        w.Step(new Enter(At(11, 9)));
        return (w, w.M.Grid.MachinesOf("treadWheel").Single().Id);
    }

    [Fact]
    public void GetsInOnEInItsOwnWordsAndOpensNothing()
    {
        var (w, wheel) = World();
        var aim = Aiming.AimAt(w.M, 0, Beside, (7.5, 6.5));
        Assert.Equal(new Use(new Board(wheel), null), aim?.Press);
        Assert.Null(aim?.Crate);
        Assert.Equal(new Prompt("use.board.treadWheel", null), Prompts.Of(w.M, aim));
        Assert.Equal("[E] Get in the treadmill wheel", I18n.En.T("use.board.treadWheel"));
    }

    [Fact]
    public void HoldsItsRiderOnItsLowestTreadInItsMiddleAndSaysHowToRun()
    {
        var (w, wheel) = World();
        w.Step(new Board(wheel));
        // The wheel's six cells from x 4, its six rows from y 4: the hero's middle at x 7, its feet on row 9's bottom.
        var held = Hero.At(Ship.Of(w.M.Grid, w.M.Rooms), Seating.SeatOf(w.M, 0)!.Value);
        Assert.Equal((7 * Hero.Unit, 10 * Hero.Unit), (held.X, held.Y));
        // A wheel holds no air of its own: the rider hears what the room around it holds, and its move keys run it.
        Assert.Equal((true, false, "treadWheel"), (Seating.InWheel(w.M, 0), Seating.InPod(w.M, 0), Seating.KindOf(w.M, 0)));
        Assert.Equal("[A/D] Run · [E] Get out", I18n.En.T(Prompts.SeatedIn("treadWheel").Action));
        Assert.Equal("use.alight", Prompts.SeatedIn("dropPod").Action);
    }

    /// <summary>Seated, the key held says whether the hero runs, whatever its body does or where the wheel stands.</summary>
    [Fact]
    public void RunsSeatedWhileAMoveKeyIsHeldAnywhere()
    {
        var still = new Hero(0, 0, 0, 0, false, 1);
        Assert.True(Report.Exerting(still, inSpace: true, runs: true));
        Assert.False(Report.Exerting(still with { Vx = Spec.Run }, inSpace: false, runs: false));
        Assert.True(Report.Exerting(still with { Vx = Spec.Run }, inSpace: false));
    }
}
