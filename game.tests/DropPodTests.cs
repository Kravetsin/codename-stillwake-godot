using Stillwake.Content;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Landing;
using Xunit;
using Hero = Stillwake.Game.Features.Crew.Hero;

namespace Stillwake.Game.Tests;

/// <summary>The drop pod in the client: E boards it and opens its slot, refused when the sim would refuse; seated, it holds the hero.</summary>
public class DropPodTests
{
    /// <summary>A room four characters tall, a drop pod standing on a run of two platform pieces in its floor.</summary>
    private static readonly string[] Rows =
    [
        "##########",
        "#........#",
        "#..ZZ....#",
        "#..ZZ....#",
        "#..ZZ....#",
        "###__#####",
        "          ",
    ];

    private const int Width = 20;
    private static int At(int x, int y) => y * Width + x;

    /// <summary>A hero standing on the floor beside the pod, its feet on cell (11, 9)'s bottom.</summary>
    private static readonly Hero Beside = new(11 * Hero.Unit, 10 * Hero.Unit, 0, 0, false, 1);

    private static (TestWorld W, int Pod) World()
    {
        var w = new TestWorld(Layout.Parse(Rows).Doubled());
        w.Step(new Enter(At(11, 9)), new Enter(At(12, 9)) { From = 1 });
        return (w, w.M.Grid.MachinesWith(MachineTrait.Seats).Single().Id);
    }

    [Fact]
    public void BoardsThePodOnEAndOpensItsSlotWithIt()
    {
        var (w, pod) = World();
        var aim = Aiming.AimAt(w.M, 0, Beside, (8.5, 6.5));
        Assert.Equal(new Use(new Board(pod), null), aim?.Press);
        Assert.Equal(pod, aim?.Crate);
        Assert.Equal(new Prompt("use.board", null), Prompts.Of(w.M, aim));
        var uses = Aiming.UsesOf(aim, pressed: true, held: false, tick: 1, workedOnTick: -1);
        Assert.Equal([new Board(pod)], uses.Commands);
        Assert.Equal(pod, uses.Open);
    }

    [Fact]
    public void NeitherBoardsNorOpensAPodTakenAndSaysWhy()
    {
        var (w, pod) = World();
        w.Step(new Board(pod) { From = 1 });
        var aim = Aiming.AimAt(w.M, 0, Beside, (8.5, 6.5));
        Assert.Equal(new Prompt("use.board", "landing.taken"), Prompts.Of(w.M, aim));
        var uses = Aiming.UsesOf(aim, pressed: true, held: false, tick: 1, workedOnTick: -1);
        Assert.Empty(uses.Commands);
        Assert.Null(uses.Open);
    }

    [Fact]
    public void HoldsASeatedHeroOnThePodsFloorInItsMiddle()
    {
        var (w, pod) = World();
        Assert.Null(Seating.SeatOf(w.M, 0));
        w.Step(new Board(pod));
        var seat = Seating.SeatOf(w.M, 0)!.Value;
        // The pod's four cells from x 6, its six rows from y 4: the hero's middle at x 8, its feet on row 9's bottom.
        var held = Hero.At(Ship.Of(w.M.Grid, w.M.Rooms), seat);
        Assert.Equal((8 * Hero.Unit, 10 * Hero.Unit), (held.X, held.Y));
        Assert.Equal("[E] Leave drop pod", I18n.En.T(Prompts.Seated.Action));
    }
}
