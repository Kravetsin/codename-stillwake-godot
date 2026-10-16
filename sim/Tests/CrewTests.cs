using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Xunit;
using Materials = Stillwake.Content.Materials;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/crew/crew.test.ts, ported.</summary>
public class CrewTests
{
    /// <summary>Nine wide: a sealed room of 21 cells with a vent above a tank, the spawn inside it.</summary>
    private const string Room = """
        #########
        #V..S...#
        #TT.....#
        #TT.....#
        #########
        """;

    private static int At(int x, int y) => y * 9 + x;
    /// <summary>A corner of the hull: in no room, so no air.</summary>
    private static readonly int Outside = At(0, 0);
    /// <summary>Room's leak a minute: twenty scrap hull cells and the scrap back walls behind its 21 cells.</summary>
    private static int Leak => (20 + 21) * Materials.Get("scrap").Leak;

    /// <summary>A world from <see cref="Room"/> in creative mode, its vent piped to its tank, with <paramref name="canisters"/> loaded.</summary>
    private static (Scenario W, int Tank, int Room) SetUp(int canisters = 0)
    {
        var w = new Scenario(Room).Do(new Items.SetCreative(true));
        foreach (int cell in new[] { At(1, 1), At(1, 2), At(1, 3) })
            w.Do(new Place(cell, new Placement("conduit", "pipe")));
        w.Run();
        int tank = w.M.Grid.Machines.FirstOrDefault(m => m.Kind == "tank")?.Id ?? 0;
        for (int i = 0; i < canisters; i++) w.Do(new LoadCanister(tank));
        w.Run();
        int room = w.M.Rooms.RoomAt(At(4, 2)) ?? throw new InvalidOperationException("room");
        return (w, tank, room);
    }

    private static Hero? HeroOf(Scenario w, int id = 0) => w.M.Crew.HeroOf(id);

    /// <summary>A hero as TypeScript's <c>toEqual</c> compares it: every field.</summary>
    private static (int Id, int Cell, int Suit, int Out, int Colour)? All(Hero? h) =>
        h == null ? null : (h.Id, h.Cell, h.Suit, h.Out, h.Colour);

    private static IReadOnlyList<int> Aired(Scenario w) => w.M.Atmosphere.AiredCells;
    private static int AirClock(Scenario w) => w.Air().Clock;
    private static int[] Colours(Scenario w) => w.Crew().Heroes.Select(h => h.Colour).ToArray();
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
        Assert.Equal(At(4, 1), new Scenario(Room).M.Crew.Spawn);
        Assert.Equal(0, new Scenario("#..#").M.Crew.Spawn);
    }

    [Fact]
    public void BringsAHeroInWithAFullSuitWhereItsClientSaysAndMovesItAfter()
    {
        var (w, _, _) = SetUp();
        w.Do(EnterAt(At(4, 1), 2)).Run();
        Assert.Equal((2, At(4, 1), Content.Crew.Suit, 0, 0), All(HeroOf(w, 2)));
        Breaths(w, 3);
        w.Do(EnterAt(At(6, 2), 2)).Run();
        Assert.Equal(At(6, 2), HeroOf(w, 2)?.Cell);
        Assert.Equal(Content.Crew.Suit - 3, HeroOf(w, 2)?.Suit);
    }

    [Fact]
    public void BreathesFromTheSuitAUnitABreathInARoomWithNoAirOrNoneAtAll()
    {
        var (w, _, _) = SetUp();
        w.Do(EnterAt(At(4, 1))).Run();
        Breaths(w, 5);
        Assert.Equal(Content.Crew.Suit - 5, HeroOf(w)?.Suit);
    }

    [Fact]
    public void BreathesTwiceAsMuchFromTheSuitRunningAndOnceAgainOnceItStops()
    {
        var (w, _, _) = SetUp();
        w.Do(EnterAt(At(4, 1))).Do(new Exert(true) { From = 0 }).Run();
        Breaths(w, 5);
        Assert.Equal(Content.Crew.Suit - 5 * Content.Crew.Exerted, HeroOf(w)?.Suit);
        w.Do(new Exert(false) { From = 0 }).Run();
        int suit = HeroOf(w)!.Suit;
        Breaths(w, 3);
        Assert.Equal(suit - 3, HeroOf(w)?.Suit);
    }

    [Fact]
    public void PassesOutRunningOnALastUnitAndWakesBreathingCalmly()
    {
        var (w, _, _) = SetUp();
        w.Do(EnterAt(At(4, 1))).Do(new Exert(true) { From = 0 }).Run();
        Breaths(w, (Content.Crew.Suit + 1) / Content.Crew.Exerted);
        var hero = HeroOf(w)!;
        Assert.Equal((0, Content.Crew.PassOut, false), (hero.Suit, hero.Out, hero.Exerting));
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
        Breaths(w, Content.Crew.Suit);
        var hero = HeroOf(w)!;
        Assert.Equal((0, Content.Crew.PassOut, At(7, 3)), (hero.Suit, hero.Out, hero.Cell));
        w.Run(Content.Crew.PassOut - 1);
        Assert.Equal(1, HeroOf(w)?.Out);
        w.Run();
        Assert.Equal((0, At(4, 1), Content.Crew.Suit, 0, 0), All(HeroOf(w)));
    }

    [Fact]
    public void BreathesRoomAirStandingInItRefillingTheSuitPaidByTheTank()
    {
        var (w, tank, room) = SetUp(3);
        w.Do(EnterAt(Outside)).Run();
        Breaths(w, 10);
        AirUp(w, room);
        w.Do(EnterAt(At(4, 1))).Run();
        int suit = HeroOf(w)?.Suit ?? 0;
        int oxygen = w.M.Atmosphere.OxygenIn(tank);
        int clock = AirClock(w);
        Breaths(w, 2);
        Assert.Equal(suit + 2 * Content.Crew.Refill, HeroOf(w)?.Suit);
        // The tank pays for the breaths, and its leak on a leak beat; the room stays whole.
        Assert.Equal(
            oxygen - 2 * (1 + Content.Crew.Refill) - AirTesting.Leaked(Leak, clock, AirClock(w)),
            w.M.Atmosphere.OxygenIn(tank));
        Assert.Equal(21, Aired(w).Count);
        // Full, it takes only its breath.
        Breaths(w, 10);
        Assert.Equal(Content.Crew.Suit, HeroOf(w)?.Suit);
    }

    [Fact]
    public void BreathesTheFarEdgeOfTheAirWhenNoTankPays()
    {
        var (w, _, room) = SetUp(1);
        AirUp(w, room);
        // The vent's pipe cut: nothing pays, nothing fills.
        w.Do(new ClearAt(At(1, 2), "conduit")).Run();
        w.Do(EnterAt(At(4, 1))).Run();
        int clock = AirClock(w);
        Breaths(w, 1);
        Assert.Equal(Content.Crew.Suit, HeroOf(w)?.Suit);
        // The corner farthest from the vent went first; the hull and back walls leak the room's air too.
        Assert.DoesNotContain(At(7, 3), Aired(w));
        Assert.Equal(21 - 1 - AirTesting.Leaked(Leak, clock, AirClock(w)), Aired(w).Count);
    }

    [Fact]
    public void BreathesRoomAirOnlyInACellHoldingItPastTheEdgeOfTheAirFromTheSuit()
    {
        var (w, _, room) = SetUp(1);
        AirUp(w, room);
        w.Do(new ClearAt(At(1, 2), "conduit")).Run();
        // At the far end, where the air goes first.
        w.Do(EnterAt(At(7, 3))).Run();
        for (int i = 0; i < 30 && Aired(w).Contains(At(7, 3)); i++) Breaths(w, 1);
        Assert.DoesNotContain(At(7, 3), Aired(w));
        int suit = HeroOf(w)?.Suit ?? 0;
        Breaths(w, 2);
        Assert.Equal(suit - 2, HeroOf(w)?.Suit);
        // The room still holds air nearer the vent.
        Assert.True(Aired(w).Count > 10, $"{Aired(w).Count} aired");
    }

    [Fact]
    public void FallsBackOnTheSuitOnceTheRoomStartsVenting()
    {
        var (w, _, room) = SetUp(3);
        AirUp(w, room);
        w.Do(EnterAt(At(4, 1))).Run();
        Breaths(w, 1);
        w.Do(new ClearAt(At(8, 2), "structure")).Run();
        Assert.Equal(RoomStatus.Venting, w.M.Atmosphere.StatusOf(room));
        Breaths(w, 2);
        Assert.Equal(Content.Crew.Suit - 2, HeroOf(w)?.Suit);
    }

    // The lifepod.

    /// <summary>Eleven wide: the lifepod in a room with no air at all, its hatch at (4, 4).</summary>
    private const string PodRoom = """
        ###########
        #.........#
        #..OOO...S#
        #..OOO....#
        #..OOO....#
        ###########
        """;

    private static int AtPod(int x, int y) => y * 11 + x;
    private static readonly int Hatch = AtPod(4, 4);
    /// <summary>The last cells either side of the pod its air reaches, and the first past them.</summary>
    private static readonly int Reached = AtPod(6, 3), Beyond = AtPod(7, 3);

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
        Assert.Equal((3, Hatch, Content.Crew.Suit, 0, 0), All(HeroOf(w, 3)));
    }

    [Fact]
    public void ReachesTheCellsWithinPodReachOfThePodEitherWayAndNoneWithoutOne()
    {
        var w = new Scenario(PodRoom);
        Assert.Equal(1, Content.Crew.PodReach);
        foreach (int cell in new[] { Hatch, AtPod(3, 2), Reached, AtPod(2, 1), AtPod(6, 5) }) Assert.True(w.M.Crew.ByPod(cell), $"{cell}");
        foreach (int cell in new[] { Beyond, AtPod(9, 1), AtPod(1, 1), -5, 999 }) Assert.False(w.M.Crew.ByPod(cell), $"{cell}");
        Assert.False(new Scenario(Room).M.Crew.ByPod(At(4, 1)));
    }

    [Fact]
    public void KeepsASuitFullByThePodInVacuumGivingTheRoomNothing()
    {
        var w = PodWorld(Hatch);
        Breaths(w, 20);
        Assert.Equal(Content.Crew.Suit, HeroOf(w)?.Suit);
        Assert.Empty(w.M.Atmosphere.AiredCells);
    }

    [Fact]
    public void FillsASuitByThePodInVacuumSlowlyAndDrainsItPastPodReachAsBefore()
    {
        var w = PodWorld(Beyond);
        Breaths(w, 20);
        Assert.Equal(Content.Crew.Suit - 20, HeroOf(w)?.Suit);
        w.Do(EnterAt(Reached)).Run();
        int suit = HeroOf(w)!.Suit;
        Breaths(w, 5);
        Assert.Equal(suit + 5 * (Content.Crew.PodRefill - 1), HeroOf(w)?.Suit);
        w.Do(EnterAt(Beyond)).Run();
        suit = HeroOf(w)!.Suit;
        Breaths(w, 5);
        Assert.Equal(suit - 5, HeroOf(w)?.Suit);
    }

    [Fact]
    public void HoldsAnExertedHerosSuitByThePodWithoutFillingIt()
    {
        var w = PodWorld(Beyond);
        Breaths(w, 10);
        w.Do(EnterAt(Hatch)).Do(new Exert(true) { From = 0 }).Run();
        int suit = HeroOf(w)!.Suit;
        Breaths(w, 5);
        Assert.Equal(suit + 5 * (Content.Crew.PodRefill - Content.Crew.Exerted), HeroOf(w)?.Suit);
    }

    [Fact]
    public void WakesAHeroThatPassedOutPastPodReachAtTheHatchWithAFullSuit()
    {
        var w = PodWorld(Beyond);
        Breaths(w, Content.Crew.Suit);
        Assert.Equal((0, Content.Crew.PassOut), (HeroOf(w)!.Suit, HeroOf(w)!.Out));
        w.Run(Content.Crew.PassOut);
        Assert.Equal((0, Hatch, Content.Crew.Suit, 0, 0), All(HeroOf(w)));
        // Made at the pod, it breathes there in vacuum on a full suit.
        Breaths(w, 10);
        Assert.Equal(Content.Crew.Suit, HeroOf(w)?.Suit);
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
        Assert.Equal(Content.Crew.Colours - 1, HeroOf(w)?.Colour);
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
        Assert.Equal(Content.Crew.Suit, HeroOf(w, 1)?.Suit);
        Assert.Equal(Content.Crew.Suit - 4, HeroOf(w, 3)?.Suit);
    }

    private sealed record Step(string Type, int Hero = 0, int Cell = 0, int Ticks = 0);

    /// <summary>
    /// fast-check's steps: heroes 0–3 entering inside, outside or off the grid, loads, a breach, and
    /// waits of up to 1500 ticks, mostly short as fast-check draws them.
    /// </summary>
    private static readonly Gen<Step> Steps = Gen.OneOf(
        from hero in Gen.Choose(0, 3)
        from cell in Gen.Elements(At(4, 1), At(6, 3), Outside, -5, 999)
        select new Step("enter", hero, cell),
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
            foreach (var s in all)
            {
                if (s.Type == "enter") w.Do(EnterAt(s.Cell, s.Hero));
                else if (s.Type == "load") w.Do(new LoadCanister(tank));
                else if (s.Type == "breach" && w.M.Grid.StructureAt(At(8, 2)) == Structure.Wall)
                    w.Do(new ClearAt(At(8, 2), "structure"));
                w.Run(s.Type == "wait" ? s.Ticks : 1);
                var heroes = w.Crew().Heroes;
                for (int i = 0; i < heroes.Count; i++)
                {
                    if (heroes[i].Suit < 0 || heroes[i].Suit > Content.Crew.Suit)
                        return false.Label($"hero {heroes[i].Id}'s suit holds {heroes[i].Suit}");
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
