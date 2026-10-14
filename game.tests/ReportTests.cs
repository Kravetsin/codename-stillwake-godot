using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using Enter = Stillwake.Sim.Crew.Enter;

namespace Stillwake.Game.Tests;

/// <summary>Telling the sim where the hero is: apps/game/src/features/crew/report.test.ts, one for one.</summary>
public class ReportTests
{
    private const int Unit = Hero.Unit;

    private static (Registry M, World World) WorldFrom(Layout layout)
    {
        var m = new Registry();
        return (m, World.Create(1, layout, m.All));
    }

    /// <summary>Nine wide: two rooms three tall either side of a doorway, space above.</summary>
    private static (Registry M, World World) TwoRooms() =>
        WorldFrom(Layout.Parse([".........", "#########", "#...D...#", "#...D...#", "#...D...#", "#########"]));

    private static int At(int x, int y) => y * 9 + x;

    /// <summary>A hero standing on the floor of cell (x, y).</summary>
    private static Hero HeroIn(int x, int y) => new(x * Unit + Unit / 2, (y + 1) * Unit, 0, 0, false, 1);

    private static Command? ReportOf(Registry m, Hero hero) => Report.Of(m.Crew, m.Rooms, m.Atmosphere, m.Grid.Width, 0, hero);

    /// <summary>Sends what <see cref="Report.Of"/> says, if anything, and runs the tick.</summary>
    private static void Tell((Registry M, World World) s, Hero hero)
    {
        var said = ReportOf(s.M, hero);
        s.World.Step(said != null ? [said] : []);
    }

    [Fact]
    public void NamesTheCellUnderTheMiddleOfTheBody()
    {
        // Standing in row 4, the hero's middle is in row 3.
        Assert.Equal(At(2, 3), HeroIn(2, 4).BodyCell(9));
        // The middle a unit above the line between rows 2 and 3.
        Assert.Equal(At(2, 2), (HeroIn(2, 4) with { Y = 3 * Unit + Hero.MiddleUp - 1 }).BodyCell(9));
    }

    [Fact]
    public void EntersAHeroTheSimDoesNotKnowYetThenOnlyWhenItsRoomChanges()
    {
        var s = TwoRooms();
        Assert.Equal(new Enter(At(1, 3)), ReportOf(s.M, HeroIn(1, 4)));
        Tell(s, HeroIn(1, 4));
        Assert.Null(ReportOf(s.M, HeroIn(3, 4)));
        Assert.Equal(new Enter(At(6, 3)), ReportOf(s.M, HeroIn(6, 4)));
    }

    [Fact]
    public void EntersAgainOnCrossingTheEdgeOfTheAirInsideOneRoom()
    {
        // A generator piped to a vent and cabled to a panel above: the air spreads from the vent,
        // nearest cells first.
        var s = WorldFrom(Layout.Parse(
            ["PPP      ", "#########", "#V......#", "#GG.....#", "#GG.....#", "#########"],
            ["", "", " -", " -", " -"],
            [((0, 0), (1, 3))]));
        var air = s.M.Atmosphere;
        for (int i = 0; i < 600 && !air.AiredAt(At(5, 3)); i++) s.World.Step([]);
        Assert.True(air.AiredAt(At(5, 3)));
        Assert.False(air.AiredAt(At(7, 3)));
        Tell(s, HeroIn(3, 4));
        Assert.Null(ReportOf(s.M, HeroIn(2, 4)));
        Assert.Equal(new Enter(At(7, 3)), ReportOf(s.M, HeroIn(7, 4)));
    }

    [Fact]
    public void NoticesACrossingThroughADoorThatIsThenShutThoughTheRoomsAreRenamed()
    {
        var s = TwoRooms();
        Tell(s, HeroIn(6, 4));
        s.World.Step([new ToggleDoor(At(4, 3))]);
        // The open doorway makes one room: walking into the left part is no change of room.
        Tell(s, HeroIn(2, 4));
        s.World.Step([new ToggleDoor(At(4, 3))]);
        // Shut, the left room is a room of its own again, with the id the joined room had.
        Assert.Equal(new Enter(At(2, 3)), ReportOf(s.M, HeroIn(2, 4)));
    }
}
