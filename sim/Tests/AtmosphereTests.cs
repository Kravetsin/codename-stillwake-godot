using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
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
    /// A sealed room nine wide, with a vent above a tank, and three canisters for peer 0. A tank's port
    /// is its bottom-left cell, a vent's its own; <see cref="Setup.PipeUp"/> lays the pipe joining them.
    /// </summary>
    private const string Room = """
        #########
        #V......#
        #TT.....#
        #TT.....#
        #########
        """;

    private static int At(int x, int y) => y * 9 + x;

    /// <summary>The canisters in peer <paramref name="peer"/>'s inventory.</summary>
    private static int Canisters(Scenario w, int peer) =>
        w.M.Items.Inventory(peer).Sum(s => s?.Item == "canister" ? s.Count : 0);

    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    private sealed record Setup(Scenario W, int Tank, int Vent, int Room)
    {
        public void PipeUp()
        {
            foreach (int cell in new[] { At(1, 1), At(1, 2), At(1, 3) })
                W.Do(new Place(cell, new Placement("conduit", "pipe")));
            W.Run();
        }
    }

    private static Setup SetUp(string map = Room) => SetUp(Layout.Parse(map));

    private static Setup SetUp(Layout map)
    {
        var w = new Scenario(map).GiveItems(0, ("canister", 3));
        var machines = w.M.Grid.Machines;
        int tank = machines.FirstOrDefault(m => m.Kind == "tank")?.Id ?? 0;
        int vent = machines.FirstOrDefault(m => m.Kind == "vent")?.Id ?? 0;
        int room = w.M.Rooms.RoomAt(At(3, 2)) ?? throw new InvalidOperationException("no room");
        return new Setup(w, tank, vent, room);
    }

    // Back walls.

    /// <summary><see cref="Room"/> with no back wall behind cell (5, 2).</summary>
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

    /// <summary>E held on plan 1 for <paramref name="ticks"/> ticks by peer 0.</summary>
    private static void WorkOn(Scenario w, int ticks)
    {
        for (int i = 0; i < ticks; i++) w.Do(new Work(1)).Run();
    }

    [Fact]
    public void VentsARoomOpenBehindAndFillsItOnceThatBackWallIsBuilt()
    {
        var s = SetUp(OpenBehind);
        var w = s.W;
        s.PipeUp();
        Assert.Equal(RoomStatus.Venting, StatusAt(w));
        var iron = Materials.Get("iron");
        w.GiveItems(0, [.. iron.Cost]);
        w.Do(new PlacePlan(At(5, 2), Placement.BackWall("iron"))).Run();
        WorkOn(w, 1 + iron.BuildTicks);
        Assert.Equal("iron", w.M.Grid.BackAt(At(5, 2))?.Id);
        w.Do(new LoadCanister(s.Tank)).Run();
        Assert.Equal(RoomStatus.Filling, StatusAt(w));
    }

    [Fact]
    public void VentsAnAiredRoomWhoseBackWallIsTakenApartAndRefundsIt()
    {
        var s = SetUp();
        var w = s.W;
        s.PipeUp();
        w.Do(new LoadCanister(s.Tank)).Run();
        w.Run(w.M.Rooms.RoomById(s.Room)!.Volume * Air.TicksPerCell + 1);
        Assert.Equal(RoomStatus.Aired, StatusAt(w));
        w.Do(new Deconstruct(At(5, 2), "back")).Run();
        var scrap = Materials.Get("scrap");
        // Math.ceil of TypeScript, in whole ticks.
        WorkOn(w, (scrap.BuildTicks + Builds.DeconstructSpeedup - 1) / Builds.DeconstructSpeedup);
        Assert.Equal(RoomStatus.Venting, StatusAt(w));
        Assert.Equal(scrap.Cost.Select(c => new Stack(c.Item, c.Count)), w.M.Items.Inventory(0).Where(st => st?.Item == "scrap"));
    }

    // Air machines.

    [Fact]
    public void StartsFillingASealedRoomOnceACanisterIsInATankPipedToAVentThere()
    {
        var s = SetUp();
        var w = s.W;
        s.PipeUp();
        Assert.Equal(RoomStatus.Vacuum, w.M.Atmosphere.StatusOf(s.Room));
        w.Do(new LoadCanister(s.Tank)).Run();
        Assert.Empty(w.Refusals);
        // Less what the room's first cells cost, if a fill beat came on the same tick.
        int paid = w.Air().Aired.Count;
        Assert.Equal(Air.Canister - paid, w.M.Atmosphere.OxygenIn(s.Tank));
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
        var s = SetUp();
        var w = s.W;
        s.PipeUp();
        w.Do(new ClearAt(At(1, 2), "conduit"))
            .Do(new Place(At(1, 2), new Placement("conduit", "valve")))
            .Do(new LoadCanister(s.Tank))
            .Run();
        Assert.Equal(RoomStatus.Filling, w.M.Atmosphere.StatusOf(s.Room));
        w.Do(new SetValve(At(1, 2), false)).Run();
        Assert.Equal(RoomStatus.Vacuum, w.M.Atmosphere.StatusOf(s.Room));
    }

    [Fact]
    public void FillsTheRoomOfAVentPipedToTheTankNotOneWithAVentOfItsOwnUnpiped()
    {
        var s = SetUp("""
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
        World.Create(1, Layout.Parse(["TTVTT", "TT.TT"], ["", "   o"]), m.All);
        var tanks = m.Grid.Machines.Where(x => x.Kind == "tank").Select(x => m.Atmosphere.OxygenIn(x.Id));
        Assert.Equal(new[] { 0, Air.Canister }, tanks);
        var bare = Assert.Throws<FormatException>(() =>
            World.Create(1, Layout.Parse(["TT.", "TT."], ["  o"]), new Registry().All));
        Assert.Contains("'o' is under no tank", bare.Message);
        var vent = Assert.Throws<FormatException>(() =>
            World.Create(1, Layout.Parse(["TTV", "TT."], ["  o"]), new Registry().All));
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

    // Generators.

    /// <summary>
    /// <see cref="Room"/> with a generator in place of the tank, its port its bottom-left cell too,
    /// cabled to a solar panel out in space below the room.
    /// </summary>
    private static readonly Layout Generated = Layout.Parse(
        [.. Layout.Parse(Room.Replace("TT", "GG")).Rows, "PPP"], null, [((0, 5), (1, 2))]);

    /// <summary>Ticks between two units a generator makes.</summary>
    private const int Every = Air.LeakEvery / Air.GeneratorRate;

    private static int GeneratorOf(Scenario w) => w.M.Grid.Machines.FirstOrDefault(m => m.Kind == "generator")?.Id ?? 0;
    private static RoomAir? SurveyOf(Scenario w, int room) => w.M.Atmosphere.RoomsAir().FirstOrDefault(r => r.Room == room);

    [Fact]
    public void MakesOxygenIntoAStoreOfItsOwnAUnitEveryFewTicksUpToTheStore()
    {
        var w = SetUp(Generated).W;
        int id = GeneratorOf(w);
        w.Run(10 * Every);
        Assert.Equal(10, w.M.Atmosphere.OxygenIn(id));
        w.Run(Air.GeneratorStore * Every);
        Assert.Equal(Air.GeneratorStore, w.M.Atmosphere.OxygenIn(id));
    }

    [Fact]
    public void FillsTheRoomItsVentStandsInNeverDryWhileItsStoreSitsEmpty()
    {
        var s = SetUp(Generated);
        var w = s.W;
        s.PipeUp();
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
        for (int i = 0; i < 100 * Every && w.M.Atmosphere.StatusOf(s.Room) != RoomStatus.Aired; i++) w.Run();
        survey = SurveyOf(w, s.Room)!;
        Assert.Equal((RoomStatus.Aired, (int?)null), (survey.Status, survey.Lasts));
    }

    [Fact]
    public void MakesNothingCutOffFromItsPowerItsRoomInVacuumForWantOfIt()
    {
        var s = SetUp(Generated);
        var w = s.W;
        s.PipeUp();
        int gen = w.M.Grid.MachineAt(At(1, 2))?.Cell ?? 0;
        w.Do(new Cut(gen, At(0, 5))).Run();
        w.Run(20 * Every);
        Assert.Empty(w.M.Atmosphere.AiredCells);
        Assert.Equal(0, w.M.Atmosphere.OxygenIn(GeneratorOf(w)));
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
    [Fact]
    public void ABreathRightAfterACanisterIsPaidFromItAsAWorldLoadedBeforeItPaysIt()
    {
        string[] rows = ["#########", "#V......#", "#TT.....#", "#TT.....#", "#########"];
        string[] under = ["", " -", " -o", " -"];
        var w = new Scenario(Layout.Parse(rows, under), with: m => [new Breather(m.Atmosphere)]);
        int tank = w.M.Grid.MachineAt(9 * 2 + 1)!.Id;
        int cell = 9 * 1 + 4;
        w.Run(60);
        Assert.True(w.M.Atmosphere.AiredAt(cell));
        // The room aired and its tank emptied, as filling a room bigger than a canister leaves it.
        ((AtmosphereState)w.M.Atmosphere.Slice).Tanks.Clear();
        w.GiveItems(0, ("canister", 1)).Run(2);
        int aired = w.M.Atmosphere.AiredCells.Count;

        var r = new Registry();
        var loaded = World.Load(w.World.Save(), [.. r.All, new Builder(r), new Breather(r.Atmosphere)]);
        Command[] commands = [new LoadCanister(tank) { From = 0 }, new BreatheNow(cell, 5)];
        w.World.Step(commands);
        loaded.Step(commands);

        Assert.Equal(aired, w.M.Atmosphere.AiredCells.Count);
        Assert.Equal(r.Atmosphere.AiredCells.Count, w.M.Atmosphere.AiredCells.Count);
        Assert.Equal(loaded.Hash(), w.World.Hash());
    }
}
