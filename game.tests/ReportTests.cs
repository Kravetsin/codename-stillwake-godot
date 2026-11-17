using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Players;
using Xunit;
using ChooseLook = Stillwake.Sim.Crew.ChooseLook;
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
    public void ChoosesTheLookOnceAJoinBeforeTheEnterThatPrintsTheHeroAndHoldsItWithNone()
    {
        var s = TwoRooms();
        var look = new Look(4, 3, 2, 1, 1);
        Assert.Equal((null, true), Report.LookFirst(s.M.Crew, 0, null, sent: false));
        Assert.Equal((new ChooseLook(look), false), Report.LookFirst(s.M.Crew, 0, look, sent: false));
        Assert.Equal((null, false), Report.LookFirst(s.M.Crew, 0, look, sent: true));
        s.World.Step([new ChooseLook(look), ReportOf(s.M, HeroIn(1, 4))!]);
        Assert.Equal(look, s.M.Players.LookOf(0));
        // Its hero made, nothing more goes before its reports, nor holds them.
        Assert.Equal((null, false), Report.LookFirst(s.M.Crew, 0, null, sent: false));
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
        // A vaporizer cabled to a panel above, a tank under the floor piped up to it and its air to the vent beside its top
        // (liquid-oxygen): the air spreads from the vent, nearest cells first.
        var s = WorldFrom(Layout.Parse(
            ["PPP      ", "#########", "#..V....#", "#GG.....#", "#GG.....#", "#########", " TT", " TT"],
            ["", "", "   -", "  --", " -", " ---", "  o-", "  --"],
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
        for (int i = 0; i < Stillwake.Content.Doors.SwingTicks; i++) s.World.Step([]);
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

    // Threatened: Terraria's way (2026-10-26-creatures-are-composed).

    [Fact]
    public void ReportsEveryTickItMovesWhileThreatenedAndOnceASecondOtherwise()
    {
        Assert.Equal(1, Report.Every(threatened: true));
        Assert.Equal(Stillwake.Content.Crew.ReportEvery, Report.Every(threatened: false));
        var s = TwoRooms();
        Tell(s, HeroIn(1, 4));
        Assert.Equal(new Enter(At(3, 3)), Report.Of(s.M.Crew, s.M.Rooms, s.M.Atmosphere, s.M.Grid.Width, 0, HeroIn(3, 4), 101, 100, Report.Every(true)));
        Assert.Null(Report.Of(s.M.Crew, s.M.Rooms, s.M.Atmosphere, s.M.Grid.Width, 0, HeroIn(3, 4), 100, 100, Report.Every(true)));
        Assert.Null(Report.Of(s.M.Crew, s.M.Rooms, s.M.Atmosphere, s.M.Grid.Width, 0, HeroIn(1, 4), 101, 100, Report.Every(true)));
    }

    /// <summary>
    /// How far behind a hero sprinting across open ground the sim's cell of it lags, its reports landing <paramref name="trip"/>
    /// ticks after they go: the widest gap over three seconds, in cells.
    /// </summary>
    private static int Lag(bool threatened, int trip)
    {
        var s = WorldFrom(Layout.Parse([new string('.', 80), new string('.', 80), new string('#', 80)]).Doubled());
        int width = s.M.Grid.Width, reported = -Stillwake.Content.Crew.ReportEvery, widest = 0;
        // A sprint: the controller's run a step, three steps a tick.
        int perTick = 3 * Stillwake.Content.Hero.Run;
        var landing = new Dictionary<int, List<Command>>();
        for (int t = 0; t < 120; t++)
        {
            var hero = new Hero(2 * Unit + t * perTick, 4 * Unit, perTick, 0, false, 1);
            if (Report.Of(s.M.Crew, s.M.Rooms, s.M.Atmosphere, width, 0, hero, t, reported, Report.Every(threatened)) is { } said)
            {
                reported = t;
                (landing.TryGetValue(t + trip, out var due) ? due : landing[t + trip] = []).Add(said);
            }
            s.World.Step(landing.GetValueOrDefault(t) ?? []);
            if (t > 30 && s.M.Crew.HeroOf(0) is { } known) widest = Math.Max(widest, hero.BodyCell(width) % width - known.Cell % width);
        }
        return widest;
    }

    [Fact]
    public void AThreatenedSprintingHeroIsKnownWithinARoundTripNotASecond()
    {
        // A round trip of some 0.2 s on a 100 ms link: 26.4 cells a second is about 5 behind, the 6 the plan allows.
        Assert.InRange(Lag(threatened: true, trip: 4), 1, 6);
        Assert.True(Lag(threatened: false, trip: 4) >= 25, "once a second, a sprint is some 26 cells ahead of the sim");
    }


    [Fact]
    public void DrawsTheBodysFrostAndHazeByItsShareOfTheWayToEachDeathEasedOut()
    {
        // (Normal − body)/(Normal − ColdDeath) and (body − Normal)/(HeatDeath − Normal), each eased out (body-temperature).
        Assert.Equal((0f, 0f), (Report.ColdOf(37), Report.HeatOf(37)));
        Assert.Equal((0.75f, 0f), (Report.ColdOf(32.5f), Report.HeatOf(32.5f)));
        Assert.Equal((1f, 0f), (Report.ColdOf(28), Report.HeatOf(28)));
        Assert.Equal((1f, 0f), (Report.ColdOf(10), Report.HeatOf(10)));
        Assert.Equal((0f, 0.75f), (Report.ColdOf(39.5f), Report.HeatOf(39.5f)));
        Assert.Equal((0f, 1f), (Report.ColdOf(42), Report.HeatOf(42)));
        for (float body = 37; body > 28; body -= 0.1f) Assert.True(Report.ColdOf(body - 0.1f) >= Report.ColdOf(body), $"{body}");
    }

}
