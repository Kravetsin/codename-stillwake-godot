using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Power;
using Xunit;
using Air = Stillwake.Content.Air;
using Builds = Stillwake.Content.Builds;
using Materials = Stillwake.Content.Materials;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/atmosphere/atmosphere.test.ts, ported.</summary>
public class AtmosphereTests
{
    /// <summary>
    /// A sealed room nine wide, doubled (0104), with a vent above a tank, and three canisters for peer 0. A tank's port
    /// is the top-left of its bottom-left block, a vent's its top-left; <see cref="Setup.PipeUp"/> lays the pipe joining them.
    /// </summary>
    private const string Room = """
        #########
        #V......#
        #TT.....#
        #TT.....#
        #########
        """;

    private static int At(int x, int y) => AirTesting.Cell(9, x, y);

    /// <summary>The four cells of the character drawn at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private static int[] Block(int x, int y) => [At(x, y), At(x, y) + 1, At(x, y) + 18, At(x, y) + 19];

    /// <summary>The canisters in peer <paramref name="peer"/>'s inventory.</summary>
    private static int Canisters(Scenario w, int peer) =>
        w.M.Items.Inventory(peer).Sum(s => s?.Item == "canister" ? s.Count : 0);

    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    private sealed record Setup(Scenario W, int Tank, int Vent, int Room)
    {
        /// <summary>On a map made the chain (<see cref="Chain"/>): the vent piped to the vaporizer's output.</summary>
        public void PipeUp()
        {
            // From the vent's port to the vaporizer's output, through a joint on its top-left a valve may be made of.
            W.GiveItems(0, ("tube", 3), ("gear", 1)).Pipe(At(1, 1), At(1, 2), W.Output(At(1, 2))).Run();
            Assert.Empty(W.Refusals);
        }
    }

    private static Setup SetUp(string map = Room) => SetUp(Layout.Parse(map));

    /// <summary>
    /// <paramref name="map"/>'s tank made the chain (<see cref="AirTesting.Chained"/>): a vaporizer where it was drawn, the
    /// tank and a panel powering the vaporizer under the hull, in space.
    /// </summary>
    private static Setup Chain(string map = Room) => SetUp(AirTesting.Chained(map));

    private static Setup SetUp(Layout map)
    {
        var w = new Scenario(map, doubled: true).GiveItems(0, ("canister", 3));
        var machines = w.M.Grid.Machines;
        int tank = machines.FirstOrDefault(m => m.Kind == "tank")?.Id ?? 0;
        int vent = machines.FirstOrDefault(m => m.Kind == "vent")?.Id ?? 0;
        int room = w.M.Rooms.RoomAt(At(3, 2)) ?? throw new InvalidOperationException("no room");
        return new Setup(w, tank, vent, room);
    }

    // Back walls.

    /// <summary><see cref="Room"/> with no back wall behind the block drawn at (5, 2).</summary>
    private static readonly string OpenBehind = ReplaceFirst(Room, "#TT.....#", "#TT.. ..#");

    /// <summary>TypeScript's <c>replace</c> with a string: the first occurrence only.</summary>
    private static string ReplaceFirst(string text, string what, string with)
    {
        int at = text.IndexOf(what, StringComparison.Ordinal);
        return text[..at] + with + text[(at + what.Length)..];
    }

    private static RoomStatus? StatusAt(Scenario w)
    {
        var room = w.M.Rooms.RoomAt(At(3, 2));
        return room is >= 0 ? w.M.Atmosphere.StatusOf(room.Value) : null;
    }

    /// <summary>E held on plan <paramref name="plan"/> for <paramref name="ticks"/> ticks by peer 0.</summary>
    private static void WorkOn(Scenario w, int ticks, int plan = 1)
    {
        for (int i = 0; i < ticks; i++) w.Do(new Work(plan)).Run();
    }

    [Fact]
    public void VentsARoomOpenBehindAndFillsItOnceThatBackWallIsBuilt()
    {
        var s = Chain(OpenBehind);
        var w = s.W;
        s.PipeUp();
        Assert.Equal(RoomStatus.Venting, StatusAt(w));
        var iron = Materials.Get("iron");
        w.GiveItems(0, [.. iron.Cost.Select(c => (c.Item, 4 * c.Count))]);
        foreach (int cell in Block(5, 2)) w.Do(new PlacePlan(cell, Placement.BackWall("iron")));
        w.Run();
        foreach (int plan in w.M.Building.Plans.Select(p => p.Id).ToArray()) WorkOn(w, 1 + iron.BuildTicks, plan);
        Assert.All(Block(5, 2), cell => Assert.Equal("iron", w.M.Grid.BackAt(cell)?.Id));
        w.Do(new LoadCanister(s.Tank)).Run();
        Assert.Equal(RoomStatus.Filling, StatusAt(w));
    }

    [Fact]
    public void VentsAnAiredRoomWhoseBackWallIsTakenApartAndRefundsIt()
    {
        var s = Chain();
        var w = s.W;
        s.PipeUp();
        w.Do(new LoadCanister(s.Tank)).Run();
        // A cell a tick, the unit a tick its vaporizer makes, and a few for what the room leaks meanwhile.
        int volume = w.M.Rooms.RoomById(s.Room)!.Volume;
        for (int i = 0; i < 2 * volume && StatusAt(w) != RoomStatus.Aired; i++) w.Run();
        Assert.Equal(RoomStatus.Aired, StatusAt(w));
        w.Do(new Deconstruct(At(5, 2), "back")).Run();
        var scrap = Materials.Get("scrap");
        // Math.ceil of TypeScript, in whole ticks.
        WorkOn(w, (scrap.BuildTicks + Builds.DeconstructSpeedup - 1) / Builds.DeconstructSpeedup);
        Assert.Equal(RoomStatus.Venting, StatusAt(w));
        Assert.Equal(scrap.Cost.Select(c => new Stack(c.Item, c.Count)), w.M.Items.Inventory(0).Where(st => st?.Item == "scrapChunk"));
    }

    // Air machines.

    [Fact]
    public void StartsFillingASealedRoomOnceACanisterIsInATankPipedToAVentThere()
    {
        var s = Chain();
        var w = s.W;
        s.PipeUp();
        Assert.Equal(RoomStatus.Vacuum, w.M.Atmosphere.StatusOf(s.Room));
        w.Do(new LoadCanister(s.Tank)).Run();
        Assert.Empty(w.Refusals);
        // The vaporizer turned a unit of it into air on the same tick, and the fill beat aired a cell with it.
        int paid = w.Air().Aired.Count;
        Assert.Equal(1, paid);
        Assert.Equal(Air.Canister - 1, w.M.Atmosphere.OxygenIn(s.Tank));
        Assert.Equal(Air.Canister - paid, w.Held());
        Assert.Equal(2, Canisters(w, 0));
        Assert.Equal(RoomStatus.Filling, w.M.Atmosphere.StatusOf(s.Room));
    }

    [Fact]
    public void FillsNothingThroughAVentWithNoPipeToTheTank()
    {
        var s = SetUp();
        s.W.Do(new LoadCanister(s.Tank)).Run();
        Assert.Equal(RoomStatus.Vacuum, s.W.M.Atmosphere.StatusOf(s.Room));
    }

    [Fact]
    public void FillsNothingPastAClosedValve()
    {
        var s = Chain();
        var w = s.W;
        s.PipeUp();
        w.Do(new MakeValve(At(1, 2), true))
            .Do(new LoadCanister(s.Tank))
            .Run();
        Assert.Equal(RoomStatus.Filling, w.M.Atmosphere.StatusOf(s.Room));
        w.Do(new SetValve(At(1, 2), false)).Run();
        Assert.Equal(RoomStatus.Vacuum, w.M.Atmosphere.StatusOf(s.Room));
    }

    [Fact]
    public void FillsTheRoomOfAVentPipedToTheTankNotOneWithAVentOfItsOwnUnpiped()
    {
        var s = Chain("""
            #########
            #V..#..V#
            #TT.#...#
            #TT.#...#
            #########
            """);
        var w = s.W;
        s.PipeUp();
        w.Do(new LoadCanister(s.Tank)).Run();
        int left = w.M.Rooms.RoomAt(At(2, 1))!.Value;
        int right = w.M.Rooms.RoomAt(At(6, 1))!.Value;
        Assert.Equal(RoomStatus.Filling, w.M.Atmosphere.StatusOf(left));
        Assert.Equal(RoomStatus.Vacuum, w.M.Atmosphere.StatusOf(right));
    }

    [Fact]
    public void StartsATankDrawnOverAnOInTheMapUnderWithACanisterOfOxygen()
    {
        // Two tanks and a vent between: machine ids 1, 2 and 3 in reading order; the second is filled.
        var m = new Registry();
        World.Create(1, Layout.Parse(["TTVTT", "TT.TT"], ["", "   o"]).Doubled(), m.All);
        var tanks = m.Grid.Machines.Where(x => x.Kind == "tank").Select(x => m.Atmosphere.OxygenIn(x.Id));
        Assert.Equal(new[] { 0, Air.Canister }, tanks);
        var bare = Assert.Throws<FormatException>(() =>
            World.Create(1, Layout.Parse(["TT.", "TT."], ["  o"]).Doubled(), new Registry().All));
        Assert.Contains("'o' is under no tank", bare.Message);
        var vent = Assert.Throws<FormatException>(() =>
            World.Create(1, Layout.Parse(["TTV", "TT."], ["  o"]).Doubled(), new Registry().All));
        Assert.Contains("'o' is under no tank", vent.Message);
    }

    [Fact]
    public void TakesTheCanisterFromTheSendersInventoryNoOneElses()
    {
        var s = SetUp();
        var w = s.W;
        w.GiveItems(2, ("canister", 3)).Do(new LoadCanister(s.Tank) { From = 2 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((3, 2), (Canisters(w, 0), Canisters(w, 2)));
    }

    [Fact]
    public void RefusesACanisterForWhatIsNoTankATankThatWouldSpillOrASenderWithNone()
    {
        var s = SetUp();
        var w = s.W;
        w.Do(new LoadCanister(s.Vent)).Do(new LoadCanister(99)).Run();
        Assert.Equal(new[] { "atmosphere.noTank", "atmosphere.noTank" }, Reasons(w));

        var full = new Scenario(Room);
        int fullTank = full.M.Grid.Machines.FirstOrDefault(m => m.Kind == "tank")?.Id ?? 0;
        full.GiveItems(4, ("canister", 1));
        full.Do(new LoadCanister(fullTank)).Do(new LoadCanister(fullTank) { From = 4 }).Run();
        Assert.Equal(new[] { "items.notEnough" }, Reasons(full));
        Assert.Equal(0, Canisters(full, 4));
        full.Do(new LoadCanister(fullTank) { From = 4 }).Run();
        Assert.Equal(new[] { "items.notEnough", "items.notEnough" }, Reasons(full));
        full.Do(new Items.SetCreative(true)).Run();
        int fits = Air.TankCapacity / Air.Canister - 1;
        for (int i = 0; i < fits; i++) full.Do(new LoadCanister(fullTank)).Run();
        Assert.Equal((fits + 1) * Air.Canister, full.M.Atmosphere.OxygenIn(fullTank));
        full.Do(new LoadCanister(fullTank)).Run();
        Assert.Equal(new[] { "items.notEnough", "items.notEnough", "atmosphere.tankFull" }, Reasons(full));
        Assert.Equal("atmosphere.tankFull", full.M.Atmosphere.CanisterProblem(fullTank, 0));
    }

    [Fact]
    public void KeepsTanksInIdOrderWhateverOrderTheyAreLoadedInThroughASave()
    {
        var w = new Scenario("""
            #########
            #.......#
            #.......#
            #.......#
            #########
            """);
        var tank = new Placement("machine", "tank");
        w.Do(new Place(At(1, 1), tank)).Do(new Place(At(4, 1), tank)).Do(new Items.SetCreative(true)).Run();
        var ids = w.M.Grid.Machines.Select(m => m.Id).ToArray();
        int first = ids[0], second = ids[1];
        w.Do(new LoadCanister(second)).Do(new LoadCanister(first)).Do(new LoadCanister(second)).Run();
        Assert.Equal(new[] { (first, Air.Canister), (second, 2 * Air.Canister) },
            w.Air().Tanks.Select(t => (t.Tank, t.Oxygen)));
        ulong hash = w.World.Hash();
        Assert.Equal(hash, w.Reload().World.Hash());
    }

    [Fact]
    public void LosesTheOxygenOfATankTakenApart()
    {
        var s = SetUp();
        var w = s.W;
        w.Do(new LoadCanister(s.Tank)).Run();
        w.Do(new ClearAt(At(1, 2), "machine")).Run();
        Assert.Empty(w.Air().Tanks);
        Assert.Equal(0, w.M.Atmosphere.OxygenIn(s.Tank));
    }

    // Generators, the vaporizers (liquid-oxygen).

    /// <summary>
    /// <see cref="Room"/> made the chain (<see cref="AirTesting.Chained"/>): a vaporizer in place of the tank, the tank under
    /// the hull piped to its input, and a solar panel at (3, 6), out in space, cabled to it; drawn, for
    /// <see cref="SetUp(Layout)"/> to double.
    /// </summary>
    private static readonly Layout Generated = AirTesting.Chained(Room);

    /// <summary>Ticks between two units a generator makes.</summary>
    private const int Every = Air.LeakEvery / Air.GeneratorRate;

    private static int GeneratorOf(Scenario w) => w.M.Grid.Machines.FirstOrDefault(m => m.Kind == "generator")?.Id ?? 0;
    private static RoomAir? SurveyOf(Scenario w, int room) => w.M.Atmosphere.RoomsAir().FirstOrDefault(r => r.Room == room);

    [Fact]
    public void MakesOxygenIntoAStoreOfItsOwnAUnitEveryFewTicksUpToTheStore()
    {
        var s = SetUp(Generated);
        var w = s.W;
        int id = GeneratorOf(w);
        // Its vent unpiped, nothing draws: the store gains what the tank loses, a unit at a time.
        w.Do(new LoadCanister(s.Tank)).Run(10 * Every);
        Assert.Equal((10, Air.Canister - 10), (w.M.Atmosphere.OxygenIn(id), w.M.Atmosphere.OxygenIn(s.Tank)));
        w.Run(Air.GeneratorStore * Every);
        // Full, it takes no more liquid.
        Assert.Equal((Air.GeneratorStore, Air.Canister - Air.GeneratorStore), (w.M.Atmosphere.OxygenIn(id), w.M.Atmosphere.OxygenIn(s.Tank)));
    }

    [Fact]
    public void FillsTheRoomItsVentStandsInNeverDryWhileItsStoreSitsEmpty()
    {
        var s = SetUp(Generated);
        var w = s.W;
        s.PipeUp();
        w.Do(new LoadCanister(s.Tank));
        var statuses = new List<RoomStatus>();
        for (int i = 0; i < 15 * Every; i++)
        {
            w.Run();
            var now = w.M.Atmosphere.StatusOf(s.Room);
            if (!statuses.Contains(now)) statuses.Add(now);
        }
        Assert.Equal(new[] { RoomStatus.Filling }, statuses);
        // A cell a unit, less what the hull and the back walls leaked meanwhile.
        Assert.True(w.M.Atmosphere.AiredCells.Count >= 10);
        var survey = SurveyOf(w, s.Room)!;
        Assert.Equal((Air.GeneratorRate, (string?)null), (survey.Made, survey.Problem));
        // As long as at 16 px: four times the cells, and a unit made four times as often (0104).
        for (int i = 0; i < 4 * 100 * Every && w.M.Atmosphere.StatusOf(s.Room) != RoomStatus.Aired; i++) w.Run();
        survey = SurveyOf(w, s.Room)!;
        // Aired, it shows how long the tank's liquid pays its leak: no longer endless because a vaporizer feeds it.
        int liquid = w.M.Atmosphere.OxygenIn(s.Tank);
        Assert.True(survey.Leak < Air.GeneratorRate);
        Assert.Equal((RoomStatus.Aired, (int?)liquid, (int?)(int)((long)liquid * Air.LeakEvery / survey.Leak)),
            (survey.Status, survey.Oxygen, survey.Lasts));
    }

    [Fact]
    public void MakesNothingCutOffFromItsPowerItsRoomInVacuumForWantOfIt()
    {
        var s = SetUp(Generated);
        var w = s.W;
        s.PipeUp();
        int gen = w.M.Grid.MachineAt(At(1, 2))?.Cell ?? 0;
        w.Do(new Cut(gen, At(3, 6))).Run();
        // Liquid behind it, so want of power is all that keeps it from making.
        w.Do(new LoadCanister(s.Tank)).Run(20 * Every);
        Assert.Empty(w.M.Atmosphere.AiredCells);
        Assert.Equal((0, Air.Canister), (w.M.Atmosphere.OxygenIn(GeneratorOf(w)), w.M.Atmosphere.OxygenIn(s.Tank)));
        var survey = SurveyOf(w, s.Room)!;
        Assert.Equal((RoomStatus.Vacuum, 0, "atmosphere.unpowered"), (survey.Status, survey.Made, survey.Problem));
    }

    [Fact]
    public void TakesNoCanisterItIsNoTank()
    {
        var w = SetUp(Generated).W;
        w.Do(new LoadCanister(GeneratorOf(w))).Run();
        Assert.Equal(new[] { "atmosphere.noTank" }, Reasons(w));
    }
}

/// <summary>Breathes at once, in the commands phase: a breath taken before atmosphere's own system runs.</summary>
public sealed record BreatheNow(int Cell, int Amount) : Command;

public sealed class Breather : Module<Breather.Nothing>
{
    public sealed class Nothing;
    public override string Id => "breather";
    public Breather(IAtmosphereOps air) => Handles<BreatheNow>(_ => null, c => air.Breathe(c.Cell, c.Amount));
    protected override Nothing Init(Layout layout) => new();
}

/// <summary>What the air keeps read between calls is read again after a canister, whenever in the tick a breath comes.</summary>
public class AtmosphereReadingTests
{
    /// <summary>
    /// A breath may be paid only from a vaporizer's store (liquid-oxygen), never from the tank a canister just went into: the
    /// breath takes its cells, and the canister reaches the room through the vaporizer, a unit on the tick; a world loaded
    /// before the canister, which reads everything afresh, does the same.
    /// </summary>
    [Fact]
    public void ABreathRightAfterACanisterIsPaidFromItAsAWorldLoadedBeforeItPaysIt()
    {
        string[] rows = ["#########", "#V......#", "#TT.....#", "#TT.....#", "#########"];
        // The vent's run drawn to the vaporizer's output, and a canister in the chain's tank (AirTesting.Vaporized).
        string[] under = ["", " ---", " o -", "   -"];
        var w = new Scenario(AirTesting.Vaporized(rows, under), with: m => [new Breather(m.Atmosphere)], doubled: true);
        int tank = w.M.Grid.MachinesOf("tank").Single().Id;
        int cell = w.At(4, 1);
        w.Run(60);
        Assert.True(w.M.Atmosphere.AiredAt(cell));
        // The tank and the store emptied, as filling a room bigger than a canister leaves them.
        ((AtmosphereState)w.M.Atmosphere.Slice).Tanks.Clear();
        w.GiveItems(0, ("canister", 1)).Run(2);
        int aired = w.M.Atmosphere.AiredCells.Count;
        var room = w.M.Atmosphere.RoomsAir().Single(a => a.Room == w.M.Rooms.RoomAt(cell));
        int clock = w.Air().Clock;

        var r = new Registry();
        var loaded = World.Load(w.World.Save(), [.. r.All, new Builder(r), new Breather(r.Atmosphere)]);
        Command[] commands = [new LoadCanister(tank) { From = 0 }, new BreatheNow(cell, 5)];
        w.World.Step(commands);
        loaded.Step(commands);

        // The breath takes 5 cells from the far edge; the vaporizer makes a unit and the fill beat airs a cell with it; the
        // leak, due more often over 8 px cells (0104), takes what falls due from the far edge.
        Assert.Equal(aired - 5 + 1 - AirTesting.Leaked(room.Leak, clock, clock + 1), w.M.Atmosphere.AiredCells.Count);
        Assert.Equal(Air.Canister - 1, w.M.Atmosphere.OxygenIn(tank));
        Assert.Equal(r.Atmosphere.AiredCells.Count, w.M.Atmosphere.AiredCells.Count);
        Assert.Equal(loaded.Hash(), w.World.Hash());
    }
}
