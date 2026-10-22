using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Xunit;
using Enter = Stillwake.Sim.Crew.Enter;

namespace Stillwake.Game.Tests;

/// <summary>Telling the sim where the hero is: apps/game/src/features/crew/report.test.ts, one for one.</summary>
public class ReportTests
{
    private const int Unit = Hero.Unit, Block = HeroTests.Block;

    private static (Registry M, World World) WorldFrom(Layout layout)
    {
        var m = new Registry();
        return (m, World.Create(1, layout, m.All));
    }

    /// <summary>Nine characters wide: two rooms three tall either side of a doorway, space above; eighteen cells (0104).</summary>
    private static (Registry M, World World) TwoRooms() =>
        WorldFrom(Layout.Parse([".........", "#########", "#...D...#", "#...D...#", "#...D...#", "#########"]).Doubled());

    /// <summary>The cell a body's middle is in in the block (x, y) of a map <paramref name="width"/> characters wide: its bottom right.</summary>
    private static int At(int x, int y, int width = 9) => (2 * y + 1) * 2 * width + 2 * x + 1;

    /// <summary>A hero standing on the floor of the block (x, y), in its middle.</summary>
    private static Hero HeroIn(int x, int y) => new(x * Block + Unit, (y + 1) * Block, 0, 0, false, 1);

    /// <summary>A cell of the doorway, its block's top-left: a door two cells thick fills its blocks (0104).</summary>
    private const int Door = 7 * 18 + 8;

    /// <summary>What <see cref="Report.Of"/> says at world tick <paramref name="tick"/>, the last report sent at <paramref name="reported"/>: by default just now.</summary>
    private static Command? ReportOf(Registry m, Hero hero, int tick = 0, int reported = 0) =>
        Report.Of(m.Crew, m.Rooms, m.Atmosphere, m.Grid.Width, 0, hero, tick, reported);

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
        Assert.Equal(At(2, 3), HeroIn(2, 4).BodyCell(18));
        // The middle a thousandth above the line between rows 2 and 3.
        Assert.Equal(At(2, 2), (HeroIn(2, 4) with { Y = 3 * Block + Hero.MiddleUp - 1 }).BodyCell(18));
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
            [((0, 0), (1, 3))]).Doubled());
        var air = s.M.Atmosphere;
        for (int i = 0; i < 600 && !air.AiredAt(At(5, 3)); i++) s.World.Step([]);
        Assert.True(air.AiredAt(At(5, 3)));
        Assert.False(air.AiredAt(At(7, 3)));
        Tell(s, HeroIn(3, 4));
        Assert.Null(ReportOf(s.M, HeroIn(2, 4)));
        Assert.Equal(new Enter(At(7, 3)), ReportOf(s.M, HeroIn(7, 4)));
    }

    [Fact]
    public void EntersAgainOnComingIntoOrLeavingTheLifepodsReachInsideOneRoomWithoutAir()
    {
        // A room with no air, the lifepod at its left end: the pod's reach runs to column 4.
        var s = WorldFrom(Layout.Parse(["............", "############", "#OOO.......#", "#OOO.......#", "#OOO.......#", "############"]).Doubled());
        int At12(int x, int y) => At(x, y, 12);
        Tell(s, HeroIn(9, 4));
        Assert.Null(Report.Of(s.M.Crew, s.M.Rooms, s.M.Atmosphere, 24, 0, HeroIn(8, 4), 0, 0));
        Assert.Equal(new Enter(At12(4, 3)), Report.Of(s.M.Crew, s.M.Rooms, s.M.Atmosphere, 24, 0, HeroIn(4, 4), 0, 0));
        Tell(s, HeroIn(4, 4));
        Assert.Equal(new Enter(At12(5, 3)), Report.Of(s.M.Crew, s.M.Rooms, s.M.Atmosphere, 24, 0, HeroIn(5, 4), 0, 0));
    }

    [Fact]
    public void NoticesACrossingThroughADoorThatIsThenShutThoughTheRoomsAreRenamed()
    {
        var s = TwoRooms();
        Tell(s, HeroIn(6, 4));
        s.World.Step([new SetDoor(Door, true)]);
        // The open doorway makes one room: walking into the left part is no change of room.
        Tell(s, HeroIn(2, 4));
        s.World.Step([new SetDoor(Door, false)]);
        // Shut, the left room is a room of its own again, with the id the joined room had.
        Assert.Equal(new Enter(At(2, 3)), ReportOf(s.M, HeroIn(2, 4)));
    }

    // The report past the field (0050).

    [Fact]
    public void ReportsAMoveWithinARoomOnceReportEveryTicksHaveGoneSinceTheLast()
    {
        var s = TwoRooms();
        Tell(s, HeroIn(1, 4));
        int every = Stillwake.Content.Crew.ReportEvery;
        Assert.Null(ReportOf(s.M, HeroIn(3, 4), tick: 100, reported: 100 - every + 1));
        Assert.Equal(new Enter(At(3, 3)), ReportOf(s.M, HeroIn(3, 4), tick: 100, reported: 100 - every));
        // Where the sim already has it, nothing however long ago.
        Assert.Null(ReportOf(s.M, HeroIn(1, 4), tick: 1000, reported: 0));
        // The room path waits for no clock.
        Assert.Equal(new Enter(At(6, 3)), ReportOf(s.M, HeroIn(6, 4), tick: 100, reported: 100));
    }

    [Fact]
    public void TellsTheSimWhereAHeroFlyingOutIsAboutOnceASecond()
    {
        // Out in space, where no room or air changes: only the clock sends.
        var s = WorldFrom(Layout.Parse([new string('.', 60), new string('.', 60), new string('.', 60)]).Doubled());
        int reported = -Stillwake.Content.Crew.ReportEvery, sent = 0;
        for (int x = 0; x < 50; x++)
        {
            var hero = HeroIn(x, 2);
            var said = Report.Of(s.M.Crew, s.M.Rooms, s.M.Atmosphere, 120, 0, hero, s.World.Tick, reported);
            if (said != null) (reported, sent) = (s.World.Tick, sent + 1);
            s.World.Step(said != null ? [said] : []);
        }
        // A block a tick for 50 ticks: the first report brings it in, then one every 20 ticks.
        Assert.Equal(3, sent);
        Assert.Equal(At(40, 1, 60), s.M.Crew.HeroOf(0)!.Cell);
    }

    [Fact]
    public void DrawsNoFrostInsideTheFieldAndFullFrostAtTheEdgeEasedOutBetween()
    {
        var m = new Registry();
        World.Create(1, Layout.Parse(["S"]).InWorld(), m.All);
        int width = m.Grid.Width, midY = m.Grid.Height / 2;
        // The field's left edge is 384 cells from the grid's: the zone's 512 less the field's 128.
        float Cold(int x) => Report.Cold(m.Crew, midY * width + x);
        Assert.Equal(0, Cold(384));
        Assert.Equal(0, Cold(width / 2));
        Assert.Equal(1, Cold(0));
        Assert.Equal(1, Report.Cold(m.Crew, -1));
        // A unit of 45 a breath, eased out, shows a little; 12 of 45 at half depth nearly half.
        Assert.InRange(Cold(383), 0.04f, 0.05f);
        Assert.InRange(Cold(192), 0.45f, 0.47f);
        for (int x = 383; x > 0; x--) Assert.True(Cold(x - 1) >= Cold(x), $"{x}");
    }
}
