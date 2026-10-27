using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Xunit;
using Materials = Stillwake.Content.Materials;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/crew/crew.test.ts, ported.</summary>
public class CrewTests
{
    /// <summary>Nine wide, doubled to 18 (0104): a sealed room of 84 cells with a vent above a tank, the spawn inside it.</summary>
    private const string Room = """
        #########
        #V..S...#
        #TT.....#
        #TT.....#
        #########
        """;

    /// <summary>The cell at (<paramref name="x"/>, <paramref name="y"/>) of the doubled room.</summary>
    private static int Cell(int x, int y) => y * 18 + x;
    /// <summary>The cell of the drawn character at (<paramref name="x"/>, <paramref name="y"/>): its block's top-left.</summary>
    private static int At(int x, int y) => Cell(2 * x, 2 * y);
    /// <summary>
    /// A breach through the right wall, a cell tall and the doubled wall's two deep: a hole two cells tall would flood the
    /// room into space (Rooms).
    /// </summary>
    private static readonly int[] Breach = [Cell(16, 4), Cell(17, 4)];
    /// <summary>A corner of the hull: in no room, so no air.</summary>
    private static readonly int Outside = At(0, 0);
    /// <summary>The room's corner farthest from the vent: the bottom-right cell of the drawn (7, 3).</summary>
    private static readonly int Far = Cell(15, 7);
    /// <summary>Room's leak a minute: the scrap back walls behind its 84 cells and the 40 of its hull's inner layer, two cells thick (Rooms, 0104).</summary>
    private static int Leak => (84 + 40) * Materials.Get("scrap").Leak;

    /// <summary>A world from <see cref="Room"/>, its vent piped to its tank in creative mode, with <paramref name="canisters"/> loaded.</summary>
    private static (Scenario W, int Tank, int Room) SetUp(int canisters = 0)
    {
        var w = new Scenario(Room).Do(new Items.SetCreative(true));
        // Down the left column from the vent's port to the tank's, at (0, 2) of its 4×4.
        w.Pipe(Cell(2, 2), Cell(2, 6)).Run();
        int tank = w.M.Grid.Machines.FirstOrDefault(m => m.Kind == "tank")?.Id ?? 0;
        for (int i = 0; i < canisters; i++) w.Do(new LoadCanister(tank));
        // Off once built: in creative mode a suit never empties.
        w.Do(new Items.SetCreative(false)).Run();
        int room = w.M.Rooms.RoomAt(At(4, 2)) ?? throw new InvalidOperationException("room");
        return (w, tank, room);
    }

    private static Hero? HeroOf(Scenario w, int id = 0) => w.M.Crew.HeroOf(id);

    /// <summary>A hero as TypeScript's <c>toEqual</c> compares it: every field.</summary>
    private static (int Id, int Cell, int Suit, int Out, int Colour)? All(Scenario w, Hero? h) =>
        h == null ? null : (h.Id, h.Cell, h.Air, h.Out, w.M.Players.ColourOf(h.Id) ?? -1);

    private static IReadOnlyList<int> Aired(Scenario w) => w.M.Atmosphere.AiredCells;
    private static int AirClock(Scenario w) => w.Air().Clock;
    private static int[] Colours(Scenario w) => w.Crew().Heroes.Select(h => w.M.Players.ColourOf(h.Id) ?? -1).ToArray();
    private static Enter EnterAt(int cell, int from = 0) => new(cell) { From = from };

    /// <summary>Runs until room <paramref name="room"/> is aired.</summary>
    private static void AirUp(Scenario w, int room)
    {
        for (int i = 0; i < 1000 && w.M.Atmosphere.StatusOf(room) != RoomStatus.Aired; i++) w.Run();
        Assert.Equal(RoomStatus.Aired, w.M.Atmosphere.StatusOf(room));
    }

    /// <summary>Runs whole breaths: from just after one beat to just after the <paramref name="count"/>-th next.</summary>
    private static void Breaths(Scenario w, int count) =>
        w.Run(count * Content.Crew.BreathEvery - w.Crew().Clock % Content.Crew.BreathEvery);

    [Fact]
    public void WakesAtTheMapsSOrAtTheFirstCellOfAMapWithoutOne()
    {
        // S stands in its block's bottom-left cell.
        Assert.Equal(At(4, 1) + 18, new Scenario(Room).M.Crew.Spawn);
        Assert.Equal(0, new Scenario("#..#").M.Crew.Spawn);
    }

    [Fact]
    public void BringsAHeroInWithAFullSuitWhereItsClientSaysAndMovesItAfter()
    {
        var (w, _, _) = SetUp();
        w.Do(EnterAt(At(4, 1), 2)).Run();
        Assert.Equal((2, At(4, 1), Scenario.Full, 0, 0), All(w, HeroOf(w, 2)));
        Breaths(w, 3);
        w.Do(EnterAt(At(6, 2), 2)).Run();
        Assert.Equal(At(6, 2), HeroOf(w, 2)?.Cell);
        Assert.Equal(Scenario.Full - 3, HeroOf(w, 2)?.Air);
    }

    [Fact]
    public void BreathesFromTheSuitAUnitABreathInARoomWithNoAirOrNoneAtAll()
    {
        var (w, _, _) = SetUp();
        w.Do(EnterAt(At(4, 1))).Run();
        Breaths(w, 5);
        Assert.Equal(Scenario.Full - 5, HeroOf(w)?.Air);
    }

    [Fact]
    public void BreathesTwiceAsMuchFromTheSuitRunningAndOnceAgainOnceItStops()
    {
        var (w, _, _) = SetUp();
        w.Do(EnterAt(At(4, 1))).Do(new Exert(true) { From = 0 }).Run();
        Breaths(w, 5);
        Assert.Equal(Scenario.Full - 5 * Content.Crew.Exerted, HeroOf(w)?.Air);
        w.Do(new Exert(false) { From = 0 }).Run();
        int suit = HeroOf(w)!.Air;
        Breaths(w, 3);
        Assert.Equal(suit - 3, HeroOf(w)?.Air);
    }

    [Fact]
    public void PassesOutRunningOnALastUnitAndWakesBreathingCalmly()
    {
        var (w, _, _) = SetUp();
        w.Do(EnterAt(At(4, 1))).Do(new Exert(true) { From = 0 }).Run();
        Breaths(w, (Scenario.Full + 1) / Content.Crew.Exerted);
        var hero = HeroOf(w)!;
        Assert.Equal((0, Content.Crew.PassOut, false), (hero.Air, hero.Out, hero.Exerting));
    }

    [Fact]
    public void RefusesToExertAHeroNotAboard()
    {
        var w = new Scenario(Room).Do(new Exert(true) { From = 3 }).Run();
        Assert.Equal(["crew.noHero"], w.Refusals.Select(r => r.Reason));
    }

    [Fact]
    public void PassesOutWhenTheSuitRunsDryThenWakesAtTheSpawnWithAFullSuit()
    {
        var (w, _, _) = SetUp();
        w.Do(EnterAt(At(7, 3))).Run();
        Breaths(w, Scenario.Full);
        var hero = HeroOf(w)!;
        Assert.Equal((0, Content.Crew.PassOut, At(7, 3)), (hero.Air, hero.Out, hero.Cell));
        w.Run(Content.Crew.PassOut - 1);
        Assert.Equal(1, HeroOf(w)?.Out);
        w.Run();
        Assert.Equal((0, At(4, 1) + 18, Scenario.Full, 0, 0), All(w, HeroOf(w)));
    }

    [Fact]
    public void BreathesRoomAirStandingInItRefillingTheSuitPaidByTheTank()
    {
        var (w, tank, room) = SetUp(3);
        w.Do(EnterAt(Outside)).Run();
        Breaths(w, 10);
        AirUp(w, room);
        w.Do(EnterAt(At(4, 1))).Run();
        int suit = HeroOf(w)?.Air ?? 0;
        int oxygen = w.M.Atmosphere.OxygenIn(tank);
        int clock = AirClock(w);
        Breaths(w, 2);
        Assert.Equal(suit + 2 * Content.Crew.Refill, HeroOf(w)?.Air);
        // The tank pays for the breaths, and its leak on a leak beat; the room stays whole.
        Assert.Equal(
            oxygen - 2 * (1 + Content.Crew.Refill) - AirTesting.Leaked(Leak, clock, AirClock(w)),
            w.M.Atmosphere.OxygenIn(tank));
        Assert.Equal(84, Aired(w).Count);
        // Full, it takes only its breath.
        Breaths(w, 10);
        Assert.Equal(Scenario.Full, HeroOf(w)?.Air);
    }

    [Fact]
    public void BreathesTheFarEdgeOfTheAirWhenNoTankPays()
    {
        var (w, _, room) = SetUp(1);
        AirUp(w, room);
        // The vent's pipe cut: nothing pays, nothing fills.
        w.Do(new Pipes.CutPipe(Cell(2, 2), Cell(2, 6))).Run();
        w.Do(EnterAt(At(4, 1))).Run();
        // Counted from here: the leak of the ticks since the cut, a fifth of a cell a tick, is the room's already.
        int clock = AirClock(w), aired = Aired(w).Count;
        Breaths(w, 1);
        Assert.Equal(Scenario.Full, HeroOf(w)?.Air);
        // The corner farthest from the vent went first; the back walls leak the room's air too.
        Assert.DoesNotContain(Far, Aired(w));
        Assert.Equal(aired - 1 - AirTesting.Leaked(Leak, clock, AirClock(w)), Aired(w).Count);
    }

    [Fact]
    public void BreathesRoomAirOnlyInACellHoldingItPastTheEdgeOfTheAirFromTheSuit()
    {
        var (w, _, room) = SetUp(1);
        AirUp(w, room);
        w.Do(new Pipes.CutPipe(Cell(2, 2), Cell(2, 6))).Run();
        // At the far end, where the air goes first.
        w.Do(EnterAt(Far)).Run();
        for (int i = 0; i < 30 && Aired(w).Contains(Far); i++) Breaths(w, 1);
        Assert.DoesNotContain(Far, Aired(w));
        int suit = HeroOf(w)?.Air ?? 0;
        Breaths(w, 2);
        Assert.Equal(suit - 2, HeroOf(w)?.Air);
        // The room still holds air nearer the vent.
        Assert.True(Aired(w).Count > 40, $"{Aired(w).Count} aired");
    }

    [Fact]
    public void FallsBackOnTheSuitOnceTheRoomStartsVenting()
    {
        var (w, _, room) = SetUp(3);
        AirUp(w, room);
        w.Do(EnterAt(At(4, 1))).Run();
        Breaths(w, 1);
        foreach (int cell in Breach) w.Do(new ClearAt(cell, "structure"));
        w.Run();
        Assert.Equal(RoomStatus.Venting, w.M.Atmosphere.StatusOf(room));
        Breaths(w, 2);
        Assert.Equal(Scenario.Full - 2, HeroOf(w)?.Air);
    }

    // The lifepod.

    /// <summary>Eleven wide, doubled to 22: the lifepod's 6×6 from (6, 4) in a room with no air at all, its hatch at (8, 9).</summary>
    private const string PodRoom = """
        ###########
        #.........#
        #..OOO...S#
        #..OOO....#
        #..OOO....#
        ###########
        """;

    private static int AtPod(int x, int y) => y * 22 + x;
    private static readonly int Hatch = AtPod(6 + Content.Crew.PodHatch.X, 4 + Content.Crew.PodHatch.Y);
    /// <summary>The last cell right of the pod (its column 11) its air reaches, and the first past it.</summary>
    private static readonly int Reached = AtPod(13, 6), Beyond = AtPod(14, 6);

    /// <summary>The pod room with hero 0 brought in at <paramref name="cell"/>.</summary>
    private static Scenario PodWorld(int cell)
    {
        var w = new Scenario(PodRoom).Do(EnterAt(cell)).Run();
        Assert.Empty(w.M.Atmosphere.AiredCells);
        return w;
    }

    [Fact]
    public void MakesHeroesAtThePodsHatchOnAMapWithOneWhateverItsS()
    {
        Assert.Equal(Hatch, new Scenario(PodRoom).M.Crew.Spawn);
        var w = new Scenario(PodRoom).Do(EnterAt(Hatch, 3)).Run();
        Assert.Equal((3, Hatch, Scenario.Full, 0, 0), All(w, HeroOf(w, 3)));
    }

    [Fact]
    public void ReachesTheCellsWithinPodReachOfThePodEitherWayAndNoneWithoutOne()
    {
        var w = new Scenario(PodRoom);
        Assert.Equal(2, Content.Crew.PodReach);
        foreach (int cell in new[] { Hatch, AtPod(6, 4), Reached, AtPod(4, 2), AtPod(13, 11) }) Assert.True(w.M.Crew.ByPod(cell), $"{cell}");
        foreach (int cell in new[] { Beyond, AtPod(18, 2), AtPod(3, 2), -5, 999 }) Assert.False(w.M.Crew.ByPod(cell), $"{cell}");
        Assert.False(new Scenario(Room).M.Crew.ByPod(At(4, 1)));
    }

    [Fact]
    public void KeepsASuitFullByThePodInVacuumGivingTheRoomNothing()
    {
        var w = PodWorld(Hatch);
        Breaths(w, 20);
        Assert.Equal(Scenario.Full, HeroOf(w)?.Air);
        Assert.Empty(w.M.Atmosphere.AiredCells);
    }

    [Fact]
    public void FillsASuitByThePodInVacuumSlowlyAndDrainsItPastPodReachAsBefore()
    {
        var w = PodWorld(Beyond);
        Breaths(w, 20);
        Assert.Equal(Scenario.Full - 20, HeroOf(w)?.Air);
        w.Do(EnterAt(Reached)).Run();
        int suit = HeroOf(w)!.Air;
        Breaths(w, 5);
        Assert.Equal(suit + 5 * (Content.Crew.PodRefill - 1), HeroOf(w)?.Air);
        w.Do(EnterAt(Beyond)).Run();
        suit = HeroOf(w)!.Air;
        Breaths(w, 5);
        Assert.Equal(suit - 5, HeroOf(w)?.Air);
    }

    [Fact]
    public void HoldsAnExertedHerosSuitByThePodWithoutFillingIt()
    {
        var w = PodWorld(Beyond);
        Breaths(w, 10);
        w.Do(EnterAt(Hatch)).Do(new Exert(true) { From = 0 }).Run();
        int suit = HeroOf(w)!.Air;
        Breaths(w, 5);
        Assert.Equal(suit + 5 * (Content.Crew.PodRefill - Content.Crew.Exerted), HeroOf(w)?.Air);
    }

    [Fact]
    public void WakesAHeroThatPassedOutPastPodReachAtTheHatchWithAFullSuit()
    {
        var w = PodWorld(Beyond);
        Breaths(w, Scenario.Full);
        Assert.Equal((0, Content.Crew.PassOut), (HeroOf(w)!.Air, HeroOf(w)!.Out));
        w.Run(Content.Crew.PassOut);
        Assert.Equal((0, Hatch, Scenario.Full, 0, 0), All(w, HeroOf(w)));
        // Made at the pod, it breathes there in vacuum on a full suit.
        Breaths(w, 10);
        Assert.Equal(Scenario.Full, HeroOf(w)?.Air);
    }

    // Suit colours.

    [Fact]
    public void PutsEachHeroComingInIntoTheFirstColourNoOneWears()
    {
        var (w, _, _) = SetUp();
        w.Do(EnterAt(At(4, 1), 0)).Run();
        w.Do(EnterAt(At(4, 1), 1)).Run();
        w.Do(new PickColour(2) { From = 0 }).Run();
        w.Do(EnterAt(At(4, 1), 2)).Run();
        Assert.Equal([2, 1, 0], Colours(w));
    }

    [Fact]
    public void SharesTheLeastWornColourOnceEveryColourIsWorn()
    {
        var (w, _, _) = SetUp();
        for (int id = 0; id <= Content.Crew.Colours; id++) w.Do(EnterAt(At(4, 1), id)).Run();
        Assert.Equal([.. Enumerable.Range(0, Content.Crew.Colours), 0], Colours(w));
    }

    [Fact]
    public void LetsAHeroPutOnAFreeColourAndKeepsItThroughABreathAndAMove()
    {
        var (w, _, _) = SetUp();
        w.Do(EnterAt(At(4, 1), 0)).Run();
        w.Do(new PickColour(Content.Crew.Colours - 1) { From = 0 }).Run();
        Assert.Empty(w.Refusals);
        Breaths(w, 2);
        w.Do(EnterAt(At(6, 2), 0)).Run();
        Assert.Equal(Content.Crew.Colours - 1, w.M.Players.ColourOf(0));
    }

    [Fact]
    public void FreesTheColourOfAHeroWhosePeerLeftForTheNextToComeIn()
    {
        var (w, _, _) = SetUp();
        w.Do(EnterAt(At(4, 1), 0)).Run();
        w.Do(EnterAt(At(4, 1), 1)).Run();
        w.Do(new Leave { From = 1 }).Run();
        Assert.Null(HeroOf(w, 1));
        w.Do(EnterAt(At(4, 1), 2)).Run();
        Assert.Equal([0, 1], Colours(w));
        w.Do(new Leave { From = 1 }).Run();
        Assert.Equal(["crew.noHero"], w.Refusals.Select(r => r.Reason));
    }

    [Fact]
    public void RefusesAColourAnotherHeroWearsOneThatIsNotThereAndAHeroNotAboard()
    {
        var (w, _, _) = SetUp();
        w.Do(EnterAt(At(4, 1), 0)).Run();
        w.Do(EnterAt(At(4, 1), 1)).Run();
        w.Do(new PickColour(0) { From = 1 }).Run();
        w.Do(new PickColour(Content.Crew.Colours) { From = 1 }).Run();
        // TypeScript's 1.5 cannot be sent here (Colour is an int): one below the first stands in for it.
        w.Do(new PickColour(-1) { From = 1 }).Run();
        w.Do(new PickColour(3) { From = 7 }).Run();
        Assert.Equal(
            ["crew.colourTaken", "crew.noSuchColour", "crew.noSuchColour", "crew.noHero"],
            w.Refusals.Select(r => r.Reason));
        Assert.Equal([0, 1], Colours(w));
    }

    [Fact]
    public void KeepsEachHeroToItselfInIdOrderWhateverOrderTheyCameIn()
    {
        var (w, _, room) = SetUp(3);
        AirUp(w, room);
        w.Do(EnterAt(Outside, 3)).Do(EnterAt(At(4, 1), 1)).Run();
        Assert.Equal([1, 3], w.Crew().Heroes.Select(h => h.Id));
        Breaths(w, 4);
        Assert.Equal(Scenario.Full, HeroOf(w, 1)?.Air);
        Assert.Equal(Scenario.Full - 4, HeroOf(w, 3)?.Air);
    }

    private sealed record Step(string Type, int Hero = 0, int Cell = 0, int Ticks = 0);

    /// <summary>
    /// fast-check's steps: heroes 0–3 entering inside, outside or off the grid, loads, a breach, thrusting
    /// or stopping (heroes 0 and 1 wear jetpacks), breathing canisters, and waits of up to 1500 ticks, mostly
    /// short as fast-check draws them.
    /// </summary>
    private static readonly Gen<Step> Steps = Gen.OneOf(
        from hero in Gen.Choose(0, 3)
        from cell in Gen.Elements(At(4, 1), At(6, 3), Outside, -5, 999)
        select new Step("enter", hero, cell),
        from hero in Gen.Choose(0, 3)
        from thrusting in Gen.Elements(0, 1)
        select new Step("thrust", hero, Ticks: thrusting),
        from hero in Gen.Choose(0, 3) select new Step("refill", hero),
        Gen.Constant(new Step("load")),
        Gen.Constant(new Step("breach")),
        Gen.Frequency((2, Gen.Choose(1, 20)), (1, Gen.Choose(1, 1500)), (1, Gen.Elements(1, 1500)))
            .Select(t => new Step("wait", Ticks: t)));

    [Property(MaxTest = 40)]
    public Property KeepsEverySuitBetweenEmptyAndFullWhateverHappens()
    {
        var steps = from n in Gen.Choose(0, 25) from s in Steps.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var (w, tank, _) = SetUp();
            w.Wear(0, Content.Jetpack.Item).Wear(1, Content.Jetpack.Item);
            foreach (var s in all)
            {
                if (s.Type == "enter") w.Do(EnterAt(s.Cell, s.Hero));
                else if (s.Type == "thrust") w.Do(new Thrust(s.Ticks == 1) { From = s.Hero });
                else if (s.Type == "refill") w.Do(new Refill { From = s.Hero });
                else if (s.Type == "load") w.Do(new LoadCanister(tank));
                else if (s.Type == "breach" && w.M.Grid.StructureAt(Breach[0]) == Structure.Wall)
                    foreach (int cell in Breach) w.Do(new ClearAt(cell, "structure"));
                w.Run(s.Type == "wait" ? s.Ticks : 1);
                var heroes = w.Crew().Heroes;
                for (int i = 0; i < heroes.Count; i++)
                {
                    if (heroes[i].Air < 0 || heroes[i].Air > Scenario.Full)
                        return false.Label($"hero {heroes[i].Id}'s suit holds {heroes[i].Air}");
                    if (i > 0 && heroes[i].Id <= heroes[i - 1].Id) return false.Label("heroes are not in id order");
                }
            }
            return true.ToProperty();
        });
    }

    [Fact]
    public void BringsAHeroThatLeftBackWithWhatItCarriedWhenItsPeerComesAboardAgain()
    {
        // A player keeps their seat in a world (0032): leaving takes the hero out, not its inventory.
        var w = new Scenario(Room);
        w.Do(EnterAt(At(4, 1), 2)).Run();
        w.GiveItems(2, ("scrap", 5));
        w.Do(new Leave { From = 2 }).Run();
        Assert.Null(HeroOf(w, 2));
        w.Do(EnterAt(At(4, 1), 2)).Run();
        Assert.NotNull(HeroOf(w, 2));
        Assert.Equal(5, w.M.Items.Totals(2).Single(t => t.Item == "scrap").Count);
    }
}
