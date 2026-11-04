using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Logistics;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;
using Structure = Stillwake.Content.Structure;
using Tune = Stillwake.Content.Logistics;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Salvage drones (salvage-drones): a drone bay's salvage drone, on a canister of air from its fuel slot, flies out through
/// doorways into space, takes the pieces a net would catch and the stacks floating in the bay's zone, and brings them to crates.
/// </summary>
public class SalvageTests
{
    /// <summary>
    /// A sealed aired room, the bay on the left, two crates (Far, then Near) and the air plant, a shut door in its right wall
    /// and space beyond it, quiet: the stream spawns nothing, every piece a test's own. 52 cells wide; space from column 28,
    /// the bay's zone to column 37.
    /// </summary>
    internal static readonly string[] Room =
    [
        "##############            ",
        "#............D            ",
        "#RR.......TT.D            ",
        "#RR.KK.KK.TTVD            ",
        "##############            ",
    ];

    internal static readonly string[] Under = ["", "", "          o", "          ---"];

    internal const int Width = 52;

    /// <summary>The cell (<paramref name="x"/>, <paramref name="y"/>), a cell of its own.</summary>
    internal static int Cell(int x, int y) => y * Width + x;

    /// <summary>A world of <see cref="Room"/>, aired and quiet, with <paramref name="canisters"/> in its bay's fuel slot and a salvage drone for each of <paramref name="charges"/>.</summary>
    internal static Scenario Out(int canisters, params int[] charges) => Quiet(LogisticsTests.Aired(Room, Under, drones: 0), canisters, charges);

    /// <summary>The same room, its bay cabled to a panel under the hull: it charges what it holds.</summary>
    private static Scenario Powered(int canisters, params int[] charges) =>
        Quiet(LogisticsTests.Aired([.. Room, "                          ", "PPP                       "], Under, drones: 0, cables: [((0, 6), (1, 2))]), canisters, charges);

    private static Scenario Quiet(Scenario w, int canisters, int[] charges)
    {
        // What drifted in while the room aired cleared, and nothing more to come.
        var debris = (DebrisState)w.M.Debris.Slice;
        foreach (var beat in debris.Beats) beat.Next = int.MaxValue;
        debris.Pieces.Clear();
        for (int i = 0; i < charges.Length; i++) w.Put(i, Salvager(charges[i]), crate: Bay(w));
        if (canisters > 0) w.Put(FuelSlot, new Stack(Content.Crew.Canister, canisters), crate: Bay(w));
        return w;
    }

    /// <summary>One full salvage drone and <paramref name="canisters"/> canisters.</summary>
    internal static Scenario Out(int canisters = 3) => Out(canisters, Full);

    internal const int Full = Content.Charge.Drone;
    internal static readonly int FuelSlot = Content.Machines.Get(Tune.Bay).SlotTakes!.Value.Slot;

    internal static Stack Salvager(int charge = Full) => new(Tune.SalvageDrone, 1, charge);
    internal static int Bay(Scenario w) => w.M.Grid.MachinesOf(Tune.Bay)[0].Id;
    /// <summary>The crate on the left, and the one nearer the door.</summary>
    internal static int Far(Scenario w) => w.M.Grid.MachinesOf("crate")[0].Id;
    internal static int Near(Scenario w) => w.M.Grid.MachinesOf("crate")[1].Id;

    internal static int Canisters(Scenario w) => w.M.Items.Crate(Bay(w))[FuelSlot]?.Count ?? 0;
    internal static int Docked(Scenario w) => LogisticsTests.Count(w.M.Items.Crate(Bay(w)), Tune.SalvageDrone);
    internal static IEnumerable<Drone> Salvaging(Scenario w) => w.M.Logistics.Drones.Where(d => d.Stack.Item == Tune.SalvageDrone);
    internal static int Held(Scenario w, int crate, string item) => LogisticsTests.Count(w.M.Items.Crate(crate), item);

    /// <summary>A piece of <paramref name="item"/> still at the centre of cell (<paramref name="x"/>, <paramref name="y"/>), or drifting left at <paramref name="speed"/>.</summary>
    internal static Piece Drift(Scenario w, string item, int x, int y, int speed = 0)
    {
        var s = (DebrisState)w.M.Debris.Slice;
        var piece = new Piece { Id = s.NextId++, Zone = DebrisSpec.Ship.Id, Item = item, X = x * 1000 + 500, Y = y * 1000 + 500, Speed = speed, Start = x * 1000 + 500, End = 0 };
        s.Pieces.Add(piece);
        return piece;
    }

    /// <summary>
    /// A stack laid on cell (<paramref name="x"/>, <paramref name="y"/>) by nobody: it floats there in space, drifting slowly back
    /// past the ship, or, <paramref name="still"/>, thrown forward as fast, so it stays where it is.
    /// </summary>
    internal static FloorStack Float(Scenario w, string item, int count, int x, int y, bool still = false)
    {
        w.M.Items.LayDown(new Stack(item, count), Cell(x, y));
        var s = w.M.Items.Floor[^1];
        if (still) s.Vx = Content.Floor.SpaceDrift;
        return s;
    }

    /// <summary>Runs until <paramref name="done"/> holds, at most <paramref name="ticks"/>; whether it did.</summary>
    internal static bool RunUntil(Scenario w, Func<bool> done, int ticks = 3000)
    {
        for (int i = 0; i < ticks && !done(); i++) w.Run();
        return done();
    }

    private static bool Home(Scenario w) => !Salvaging(w).Any();

    private static string[] Reasons(Scenario w) => [.. w.Refusals.Select(r => r.Reason)];

    // Slots, power and passage.

    [Fact]
    public void ACanisterGoesOnlyInTheFuelSlotAndTheSalvageDroneOnlyInADroneSlot()
    {
        var w = Out(canisters: 0, []).GiveItems(0, (Content.Crew.Canister, 4), (Tune.SalvageDrone, 1), (Tune.Drone, 1));
        int bay = Bay(w);
        int canister = Slots(w, Content.Crew.Canister), salvager = Slots(w, Tune.SalvageDrone), drone = Slots(w, Tune.Drone);
        w.Do(new Move(new Slot(canister), new Target(0, bay), 1))
            .Do(new Move(new Slot(salvager), new Target(FuelSlot, bay), 1))
            .Do(new Move(new Slot(drone), new Target(FuelSlot, bay), 1)).Run();
        Assert.Equal(["items.notTaken", "items.notTaken", "items.notTaken"], Reasons(w));
        Assert.All(w.M.Items.Crate(bay), Assert.Null);

        w.Refusals.Clear();
        w.Do(new Move(new Slot(canister), new Target(FuelSlot, bay), 4)).Do(new Move(new Slot(salvager), new Target(0, bay), 1)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal((4, 1), (Canisters(w), Docked(w)));

        // As drones dock and deliveries go: a canister only to its slot, a drone never there.
        var v = Out(canisters: 0, []);
        Assert.Equal(0, v.M.Items.PutIn(Bay(v), new Stack(Content.Crew.Canister, 2)));
        for (int i = 0; i < 4; i++) Assert.Equal(0, v.M.Items.PutIn(Bay(v), Salvager()));
        Assert.Equal(1, v.M.Items.PutIn(Bay(v), Salvager()));
        Assert.Equal([.. Enumerable.Repeat(Salvager(), 4), new Stack(Content.Crew.Canister, 2)], v.M.Items.Crate(Bay(v)));
    }

    /// <summary>The first of peer 0's own slots holding <paramref name="item"/>.</summary>
    private static int Slots(Scenario w, string item)
    {
        var inventory = w.M.Items.Inventory(0);
        for (int i = 0; i < inventory.Count; i++)
            if (inventory[i]?.Item == item) return i;
        throw new InvalidOperationException($"peer 0 holds no {item}");
    }

    [Fact]
    public void ASalvageDroneChargesOnItsPadAndCanistersDrawNoPower()
    {
        var w = Powered(canisters: 3);
        w.Run();
        Assert.False(w.M.Power.Charges(Bay(w)));
        w.Put(0, Salvager(0), crate: Bay(w)).Run();
        Assert.True(w.M.Power.Charges(Bay(w)));
        int before = w.M.Items.Crate(Bay(w))[0]!.Charge;
        w.Run(10);
        Assert.True(w.M.Items.Crate(Bay(w))[0]!.Charge > before);
        Assert.Equal(3, Canisters(w));
        Assert.Equal(0, w.M.Items.Crate(Bay(w))[FuelSlot]!.Charge);
    }

    [Fact]
    public void AnUnchargedSalvageDroneDoesNotLaunchAndAChargedOneLaunchesFromAnUnpoweredBay()
    {
        var w = Out(3, Tune.LaunchAt - 1);
        Drift(w, "scrapChunk", 32, 6);
        for (int i = 0; i < 100; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.Equal((3, Tune.LaunchAt - 1), (Canisters(w), w.M.Items.Crate(Bay(w))[0]!.Charge));
        Assert.False(w.M.Power.Charges(Bay(w)));

        w.Put(0, Salvager(Tune.LaunchAt), crate: Bay(w));
        Assert.True(RunUntil(w, () => Salvaging(w).Any(), 20));
        Assert.Equal(2, Canisters(w));
    }

    [Fact]
    public void LowChargeTurnsItHomeAsLowAirDoes()
    {
        foreach (bool air in new[] { false, true })
        {
            var w = Out();
            Drift(w, "scrapChunk", 36, 6);
            Assert.True(RunUntil(w, () => Salvaging(w).Any(d => d.Cell % Width >= 29), 400));
            var d = Salvaging(w).Single();
            // Enough for a few cells home, short of the way and the spare.
            if (air) d.Air = 30;
            else d.Stack = d.Stack with { Charge = 30 * Tune.Draw * Content.Charge.Scale };
            w.Run(3);
            Assert.True(LogisticsModule.Low(d), air ? "air" : "charge");
            Assert.Single(w.M.Debris.Pieces);
        }
    }

    [Fact]
    public void ASalvageDroneFliesOutThroughAShutDoorTakesAScrapChunkAndPutsItInTheCrate()
    {
        var w = Out();
        Drift(w, "scrapChunk", 32, 6);
        bool through = false;
        Assert.True(RunUntil(w, () =>
        {
            Assert.Equal(Structure.DoorClosed, w.M.Grid.StructureAt(Cell(26, 4)));
            through |= Salvaging(w).Any(d => d.Cell % Width is 26 or 27);
            return through && Home(w);
        }, 600));
        Assert.Empty(w.M.Debris.Pieces);
        Assert.Equal(1, Held(w, Near(w), "scrapChunk"));
        Assert.Equal((2, 1), (Canisters(w), Docked(w)));
        Assert.Empty(w.M.Items.Floor);
        Assert.Empty(w.Refusals);
    }

    [Fact]
    public void OrdinaryDronesStillNeverLeaveTheAirAndSalvageDronesServeNoMachine()
    {
        // An ordinary drone beside the salvage drone, a piece outside: only the salvage drone flies out.
        var w = Out();
        LogisticsTests.Dock(w, Bay(w), 1);
        w.Put(1, Salvager(), crate: Bay(w));
        Drift(w, "scrapChunk", 32, 6);
        Assert.True(RunUntil(w, () => w.M.Logistics.Drones.Count > 0, 20));
        Assert.True(RunUntil(w, () =>
        {
            Assert.All(w.M.Logistics.Drones, d => Assert.Equal(Tune.SalvageDrone, d.Stack.Item));
            return Home(w);
        }, 600));
        Assert.Equal(1, Held(w, Near(w), "scrapChunk"));

        // A furnace short of chunks beside a crate of them: a salvage drone with a canister takes no job.
        var v = LogisticsTests.TwoRooms(drones: 0).FillCrate(LogisticsTests.Left, ("scrapChunk", 20)).Put(0, new Stack("scrapChunk", 1), crate: LogisticsTests.Furnace);
        v.Put(0, Salvager(), crate: LogisticsTests.Bay).Put(FuelSlot, new Stack(Content.Crew.Canister, 2), crate: LogisticsTests.Bay);
        for (int i = 0; i < 300; i++) Assert.Empty(v.Run().M.Logistics.Drones);
        Assert.Equal(1, v.M.Items.HeldFor(LogisticsTests.Furnace, "scrapChunk"));
        Assert.Equal(2, v.M.Items.Crate(LogisticsTests.Bay)[FuelSlot]!.Count);
    }

    // What it takes.

    [Fact]
    public void TakingACargoCrateRollsItsLootAtOnceAndRecordsItFound()
    {
        var w = Out();
        var piece = Drift(w, "cargoCrate", 32, 6);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(d => d.Loaded.Count > 0), 400));
        var d = Salvaging(w).Single();
        Assert.Equal(("cargoCrate", piece.Id), (d.Caught, d.Piece));
        var found = w.M.Debris.Found.Single(f => f.Kind == "cargoCrate");
        Assert.Equal(d.Loaded.Select(s => s.Item).Distinct(), found.Seen);
        Assert.Null(w.M.Debris.CaughtFrom(piece.Id));
        var loaded = d.Loaded.GroupBy(s => s.Item).ToDictionary(g => g.Key, g => g.Sum(s => s.Count));
        Assert.True(RunUntil(w, () => Home(w), 400));
        Assert.All(loaded, l => Assert.Equal(l.Value, Held(w, Near(w), l.Key) + Held(w, Far(w), l.Key)));
    }

    [Fact]
    public void AStackFloatingInSpaceIsFetchedAndOneOnTheShipsFloorIsLeft()
    {
        var w = Out();
        var outside = Float(w, "plate", 12, 34, 6, still: true);
        Float(w, "gear", 5, 20, 6);
        w.Run(Content.Floor.OwnPickupDelay);
        Assert.True(w.M.Rooms.Afloat(w.M.Items.Floor.Single(s => s.Item == "plate").Cell));
        Assert.True(RunUntil(w, () => Salvaging(w).Any(), 20));
        Assert.Equal((0, outside.Id), (Salvaging(w).Single().Piece, Salvaging(w).Single().Floor));
        Assert.True(RunUntil(w, () => Home(w), 600));
        Assert.Equal(12, Held(w, Near(w), "plate"));
        Assert.Equal(5, w.M.Items.Floor.Single().Count);
        Assert.Equal("gear", w.M.Items.Floor.Single().Item);
        Assert.Equal(2, Canisters(w));
    }

    [Fact]
    public void AHerosBodyAndAStackJustThrownAreLeft()
    {
        // A hero's body floating outside: never fetched.
        var w = Out().GiveItems(1, ("plate", 7));
        w.M.Items.LeaveBody(1, Cell(34, 6));
        Assert.NotEmpty(w.M.Items.Floor);
        Assert.All(w.M.Items.Floor, s => Assert.NotNull(s.Body));
        foreach (var s in w.M.Items.Floor) s.Vx = Content.Floor.SpaceDrift;
        for (int i = 0; i < 200; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.All(w.M.Items.Floor, s => Assert.True(w.M.Rooms.Afloat(s.Cell)));

        // A stack thrown out there, caught still: left until it has lain OwnPickupDelay, then fetched.
        var v = Out().GiveItems(0, ("plate", 7));
        v.Do(new Items.Drop(new Slot(Slots(v, "plate")), 7, Cell(34, 6), 1)).Run();
        int thrown = v.M.Items.Ticks;
        var stack = Assert.Single(v.M.Items.Floor);
        (stack.Vx, stack.Vy) = (Content.Floor.SpaceDrift, 0);
        while (v.M.Items.Ticks - thrown < Content.Floor.OwnPickupDelay - Tune.LookEvery) Assert.Empty(v.Run().M.Logistics.Drones);
        Assert.True(RunUntil(v, () => Salvaging(v).Any(), 2 * Tune.LookEvery));
        Assert.True(v.M.Items.Ticks - thrown >= Content.Floor.OwnPickupDelay);
    }

    [Fact]
    public void WreckageAsteroidsClodsAndReeledPiecesAreNeverTargets()
    {
        var w = Out();
        foreach (var (item, x) in new[] { ("hullSection", 30), ("asteroid", 32), ("soilClod", 34) }) Drift(w, item, x, 12);
        var reeled = Drift(w, "scrapChunk", 36, 6);
        reeled.Reel = new Reeling { By = 0, ToX = 36500, ToY = 6500, Left = 1_000_000 };
        Assert.NotNull(Content.Wreckage.HpOf("hullSection"));
        Assert.All(["hullSection", "asteroid", "soilClod"], item => Assert.DoesNotContain(item, DebrisSpec.NetCatches));
        for (int i = 0; i < 200; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.Equal(3, Canisters(w));
    }

    [Fact]
    public void APieceOverTheHullOrOutOfTheZoneIsNoTarget()
    {
        var w = Out();
        // Its middle in the hull's top wall, and one a cell past the zone's right edge.
        Drift(w, "scrapChunk", 16, 0);
        Drift(w, "scrapChunk", w.M.Logistics.ZoneOf(Bay(w))!.Value.Right + 1, 6);
        for (int i = 0; i < 200; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.Equal(3, Canisters(w));

        // One on the zone's edge is.
        Drift(w, "scrapChunk", w.M.Logistics.ZoneOf(Bay(w))!.Value.Right, 6);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(), 20));
    }

    [Fact]
    public void TheNearestTargetGoesFirstPiecesBeforeStacksThenTheLowestId()
    {
        // From the bay's top-left (2, 4): a stack 28 cells off, two pieces 28 off too, and one 27 off made last; launched one a look.
        var w = Out(canisters: 4, Full, Full, Full, Full);
        var stack = Float(w, "plate", 3, 30, 4, still: true);
        w.Run(Content.Floor.OwnPickupDelay);
        Assert.Equal(Cell(30, 4), w.M.Items.Floor.Single().Cell);
        var first = Drift(w, "scrapChunk", 28, 6);
        var second = Drift(w, "scrapChunk", 29, 5);
        var nearest = Drift(w, "scrapChunk", 29, 4);
        var order = new List<(int Piece, int Floor)>();
        Assert.True(RunUntil(w, () =>
        {
            foreach (var d in Salvaging(w))
                if (!order.Contains((d.Piece, d.Floor)) && d.Leg == Leg.Source) order.Add((d.Piece, d.Floor));
            return order.Count == 4;
        }, 200));
        Assert.Equal([(nearest.Id, 0), (first.Id, 0), (second.Id, 0), (0, stack.Id)], order);
    }

    [Fact]
    public void TwoSalvageDronesNeverGoForOneTarget()
    {
        var w = Out(canisters: 4, Full, Full);
        Drift(w, "scrapChunk", 32, 6);
        for (int i = 0; i < 60; i++) Assert.True(w.Run().M.Logistics.Drones.Count <= 1);
        Assert.Equal((3, 1), (Canisters(w), Docked(w)));

        // Two targets: both fly, each to its own.
        Drift(w, "scrapChunk", 34, 8);
        Assert.True(RunUntil(w, () => Salvaging(w).Count() == 2, 20));
        var pieces = Salvaging(w).Select(d => d.Piece).ToList();
        Assert.Equal(2, pieces.Distinct().Count());
        Assert.DoesNotContain(0, pieces);
        Assert.True(RunUntil(w, () => Home(w), 600));
        Assert.Equal(2, Held(w, Near(w), "scrapChunk"));
    }

    // Launch, and what stops it.

    [Fact]
    public void NoCanisterNoLaunchNothingSpent()
    {
        var w = Out(canisters: 0);
        Drift(w, "scrapChunk", 32, 6);
        for (int i = 0; i < 200; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.Equal((1, Full), (Docked(w), w.M.Items.Crate(Bay(w))[0]!.Charge));
        Assert.Single(w.M.Debris.Pieces);
    }

    [Fact]
    public void NoTargetNoLaunchNothingSpent()
    {
        var w = Out();
        for (int i = 0; i < 200; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.Equal((1, 3), (Docked(w), Canisters(w)));
    }

    [Fact]
    public void NoCrateWithAnEmptySlotNoLaunchNothingSpent()
    {
        static void Stays(Scenario w)
        {
            Drift(w, "scrapChunk", 32, 6);
            for (int i = 0; i < 200; i++) Assert.Empty(w.Run().M.Logistics.Drones);
            Assert.Equal((1, 3), (Docked(w), Canisters(w)));
        }
        // Full: every slot holding a chunk, room left on each.
        var full = Out();
        foreach (int crate in new[] { Far(full), Near(full) })
            for (int i = 0; i < full.M.Items.Crate(crate).Count; i++) full.Put(i, new Stack("scrapChunk", 1), crate: crate);
        Stays(full);

        // Switched off.
        var off = Out();
        off.Do(new SetServed(Far(off), false)).Do(new SetServed(Near(off), false)).Run();
        Stays(off);

        // Walled in, out of the flood.
        var walled = Out();
        foreach (int crate in new[] { Far(walled), Near(walled) })
        {
            int x = walled.M.Grid.MachineById(crate)!.Cell % Width;
            foreach (int cell in Enumerable.Range(4, 4).Select(y => Cell(x - 1, y)).Concat(Enumerable.Range(4, 4).Select(y => Cell(x + 4, y))).Concat(Enumerable.Range(x, 4).Select(c => Cell(c, 4)).Concat(Enumerable.Range(x, 4).Select(c => Cell(c, 5)))))
                walled.Do(new Place(cell, Placement.Wall()));
        }
        walled.Run();
        Assert.Empty(walled.Refusals);
        Stays(walled);
    }

    [Fact]
    public void ATargetNotReachableFromInsideLaunchesNothing()
    {
        // The door walled up: the room is sealed off from space.
        var w = Out();
        for (int y = 2; y < 8; y++)
            foreach (int x in new[] { 26, 27 })
                w.Do(new ClearAt(Cell(x, y), "structure")).Do(new Place(Cell(x, y), Placement.Wall()));
        w.Run();
        Assert.Empty(w.Refusals);
        Drift(w, "scrapChunk", 32, 6);
        for (int i = 0; i < 200; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.Equal(3, Canisters(w));
    }

    [Fact]
    public void ATargetOneCanisterCannotCoverIsSkipped()
    {
        // Far out, its target gone mid-trip: of two left, the nearer lies beyond it, the farther on its way back to the crate.
        var w = Out();
        var gone = Drift(w, "scrapChunk", 36, 6);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(d => d.To % Width is >= 31 and <= 33 && d.To != d.Cell), 400));
        var d = Salvaging(w).Single();
        int at = d.To, x = at % Width, y = at / Width;
        ((DebrisState)w.M.Debris.Slice).Pieces.Remove(gone);
        var beyond = Drift(w, "scrapChunk", x + 2, y);
        var back = Drift(w, "scrapChunk", x - 3, y);
        // Air for the trip by the one on the way back, short of the one beyond: to it, on to the crate, across it, home, the spare.
        int crate = w.M.Grid.MachineById(Near(w))!.Cell, bay = w.M.Grid.MachineById(Bay(w))!.Cell;
        var spec = Content.Machines.Get("crate");
        int Apart(int a, int b) => Math.Abs(a % Width - b % Width) + Math.Abs(a / Width - b / Width);
        int Trip(int target) => Apart(at, target) + Apart(target, crate) + spec.Width + spec.Height - 2 + Apart(crate, bay) + 4;
        int Ticks(int cells) => (cells * 1000 + Tune.Speed - 1) / Tune.Speed;
        int need = Ticks(Trip(Cell(x - 3, y))), over = Ticks(Trip(Cell(x + 2, y)));
        Assert.True(over - need >= 8);
        d.Air = need + 4;
        Assert.True(RunUntil(w, () => d.Cell == at, 5));
        Assert.Equal((Leg.Source, back.Id), (d.Leg, d.Piece));
        Assert.Contains(beyond, w.M.Debris.Pieces);
    }

    // Air.

    [Fact]
    public void OneCanisterALaunchGivesTripAirTicks()
    {
        var w = Out();
        Drift(w, "scrapChunk", 32, 6);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(), 20));
        var d = Salvaging(w).Single();
        Assert.Equal((Tune.TripAir, 2), (d.Air, Canisters(w)));
        w.Run();
        Assert.Equal(Tune.TripAir - 1, d.Air);
    }

    [Fact]
    public void OnOneCanisterItMakesTripAfterTripThenTurnsHomeLowWithTheWayHomeAndSpareLeft()
    {
        var w = Out(canisters: 1);
        for (int i = 0; i < 10; i++) Drift(w, "scrapChunk", 30 + (i % 4) * 2, 2 + i);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(), 20));
        var d = Salvaging(w).Single();
        bool low = false;
        int air = 0;
        Assert.True(RunUntil(w, () =>
        {
            if (!low && LogisticsModule.Low(d)) (low, air) = (true, d.Air);
            return Home(w);
        }, 2000));
        Assert.True(low);
        int caught = Held(w, Near(w), "scrapChunk") + Held(w, Far(w), "scrapChunk");
        Assert.InRange(caught, 3, 9);
        Assert.Equal(10 - caught, w.M.Debris.Pieces.Count);
        // It turned with the way home and the spare to fly on: it docked, nothing on the floor.
        Assert.True(air > 0);
        Assert.Equal((0, 1), (Canisters(w), Docked(w)));
        Assert.Empty(w.M.Items.Floor);
    }

    [Fact]
    public void LeftoverAirIsLostOnDocking()
    {
        var w = Out();
        Drift(w, "scrapChunk", 32, 6);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(), 20));
        var first = Salvaging(w).Single();
        Assert.True(RunUntil(w, () => Home(w), 600));
        Assert.True(first.Air > 0);
        Assert.Equal(Salvager(w.M.Items.Crate(Bay(w))[0]!.Charge), w.M.Items.Crate(Bay(w))[0]);

        // The next launch takes another canister and a whole canister's air.
        Drift(w, "scrapChunk", 32, 6);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(), 20));
        Assert.Equal((Tune.TripAir, 1), (Salvaging(w).Single().Air, Canisters(w)));
    }

    // Delivery.

    [Fact]
    public void WhatDoesNotFitGoesToTheNextCrateAndTheRestLiesBesideTheLastTried()
    {
        var w = Out();
        Float(w, "plate", 30, 34, 6, still: true);
        w.Run(Content.Floor.OwnPickupDelay);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(d => d.Loaded.Count > 0), 400));
        // Both crates filled but for room for ten plates in the near one and five in the far.
        int stack = Content.Items.Get("plate").Stack;
        foreach (var (crate, room) in new[] { (Near(w), 10), (Far(w), 5) })
        {
            w.Put(0, new Stack("plate", stack - room), crate: crate);
            for (int i = 1; i < w.M.Items.Crate(crate).Count; i++) w.Put(i, new Stack("rock", Content.Items.Get("rock").Stack), crate: crate);
        }
        Assert.True(RunUntil(w, () => Home(w), 600));
        Assert.Equal((stack, stack), (Held(w, Near(w), "plate"), Held(w, Far(w), "plate")));
        var rest = w.M.Items.Floor.Single();
        Assert.Equal(("plate", 15), (rest.Item, rest.Count));
        var far = w.M.Grid.MachineById(Far(w))!;
        Assert.InRange(rest.Cell % Width, far.Cell % Width, far.Cell % Width + 3);
    }

    [Fact]
    public void CratesFullTheLootLiesBesideTheLastCrateTriedAndTheDroneFliesHome()
    {
        var w = Out();
        Drift(w, "scrapChunk", 32, 6);
        Drift(w, "scrapChunk", 34, 8);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(d => d.Loaded.Count > 0), 400));
        foreach (int crate in new[] { Near(w), Far(w) })
            for (int i = 0; i < w.M.Items.Crate(crate).Count; i++) w.Put(i, new Stack("rock", Content.Items.Get("rock").Stack), crate: crate);
        Assert.True(RunUntil(w, () => Home(w), 600));
        var chunk = w.M.Items.Floor.Single();
        var near = w.M.Grid.MachineById(Near(w))!;
        Assert.Equal("scrapChunk", chunk.Item);
        Assert.InRange(chunk.Cell % Width, near.Cell % Width, near.Cell % Width + 3);
        // The other piece waits: no crate has an empty slot.
        for (int i = 0; i < 200; i++) Assert.Empty(w.Run().M.Logistics.Drones);
        Assert.Single(w.M.Debris.Pieces);
        Assert.Equal(2, Canisters(w));
    }

    [Fact]
    public void ATargetCrateTakenApartMidTripSendsTheLoadToAnother()
    {
        var w = Out();
        Drift(w, "scrapChunk", 32, 6);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(d => d.Loaded.Count > 0), 400));
        Assert.Equal(Near(w), Salvaging(w).Single().Target);
        int far = Far(w);
        w.Do(new ClearAt(w.M.Grid.MachineById(Near(w))!.Cell, "machine")).Run();
        Assert.Single(w.M.Grid.MachinesOf("crate"));
        Assert.True(RunUntil(w, () => Home(w), 600));
        Assert.Equal(1, Held(w, far, "scrapChunk"));
        Assert.Empty(w.M.Items.Floor);
    }

    [Fact]
    public void ALowDroneWithALoadLaysItAtItsBay()
    {
        var w = Out();
        Drift(w, "scrapChunk", 30, 6);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(d => d.Loaded.Count > 0), 400));
        var d = Salvaging(w).Single();
        var bay = w.M.Grid.MachineById(Bay(w))!;
        // Air just short of its way home, to the bay's near side, and the 4 spare cells it keeps: it turns home at once.
        int home = d.Cell % Width - (bay.Cell % Width + 3);
        d.Air = (home + 4) * 1000 / Tune.Speed - 1;
        Assert.True(RunUntil(w, () => LogisticsModule.Low(d), 5));
        Assert.True(RunUntil(w, () => Home(w), 300));
        Assert.Equal(1, Docked(w));
        var chunk = w.M.Items.Floor.Single();
        Assert.Equal("scrapChunk", chunk.Item);
        Assert.InRange(chunk.Cell % Width, bay.Cell % Width, bay.Cell % Width + 3);
        Assert.Equal(0, Held(w, Near(w), "scrapChunk") + Held(w, Far(w), "scrapChunk"));
    }

    // Races.

    [Fact]
    public void AHerosGrabOnTheArrivingTickWinsAndTheCanisterIsNotRefunded()
    {
        var w = Out();
        var piece = Drift(w, "scrapChunk", 32, 6);
        w.Do(new Enter(Cell(33, 6)) { From = 1 }).Run();
        Assert.True(RunUntil(w, () => Salvaging(w).Any(d => d.To == Cell(32, 6) && Math.Abs(d.X - 32500) + Math.Abs(d.Y - 6500) <= Tune.Speed), 400));
        w.Do(new Grab(piece.Id) { From = 1 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(1, w.M.Items.Inventory(1).Sum(s => s?.Item == "scrapChunk" ? s.Count : 0));
        Assert.True(RunUntil(w, () => Home(w), 600));
        Assert.Equal((2, 0), (Canisters(w), Held(w, Near(w), "scrapChunk")));
    }

    [Fact]
    public void ANetCatchingTheTargetWins()
    {
        // A post of hull in space, a net hung on it: the piece drifts into the net before the drone gets there.
        var w = Out();
        for (int y = 2; y < 8; y++)
            foreach (int x in new[] { 32, 33 }) w.Do(new Place(Cell(x, y), Placement.Wall()));
        w.Do(new Place(Cell(34, 2), new Placement("machine", "net"))).Run();
        Assert.Empty(w.Refusals);
        var piece = Drift(w, "scrapChunk", 37, 4, speed: 30);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(), 20));
        Assert.Equal(piece.Id, Salvaging(w).Single().Piece);
        Assert.True(RunUntil(w, () => Home(w), 600));
        Assert.Single(w.M.Debris.Held);
        Assert.Equal((2, 0), (Canisters(w), Held(w, Near(w), "scrapChunk")));
    }

    [Fact]
    public void AReelOnTheTargetLetsTheDroneGo()
    {
        var w = Out();
        var piece = Drift(w, "scrapChunk", 36, 6);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(d => d.Cell % Width >= 28), 400));
        piece.Reel = new Reeling { By = 1, ToX = 36500, ToY = 6500, Left = 1_000_000 };
        Assert.True(RunUntil(w, () => Home(w), 600));
        Assert.Single(w.M.Debris.Pieces);
        Assert.Equal(0, Held(w, Near(w), "scrapChunk"));
    }

    // Walls, drops, planets, saves.

    /// <summary>A salvage drone out in space with the chunk it took, the door then walled up behind it.</summary>
    private static Scenario CutOff()
    {
        var w = Out();
        Drift(w, "scrapChunk", 36, 6);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(d => d.Loaded.Count > 0), 400));
        for (int y = 2; y < 8; y++)
            foreach (int x in new[] { 26, 27 })
                w.Do(new ClearAt(Cell(x, y), "structure")).Do(new Place(Cell(x, y), Placement.Wall()));
        return w;
    }

    [Fact]
    public void AWallCutAcrossTheWayHomeDropsItWithItsLoadAsFloatingStacks()
    {
        var w = CutOff();
        Assert.True(RunUntil(w, () => Home(w), 10));
        Assert.Equal(0, Docked(w));
        Assert.Equal([Tune.SalvageDrone, "scrapChunk"], w.M.Items.Floor.Select(s => s.Item).Order(StringComparer.Ordinal));
        Assert.All(w.M.Items.Floor, s => Assert.True(w.M.Rooms.Afloat(s.Cell)));
        Assert.All(w.M.Items.Floor, s => Assert.True(s.Cell % Width >= 28));
    }

    [Fact]
    public void ADroppedSalvageDroneIsFetchedByAnother()
    {
        var w = CutOff();
        Assert.True(RunUntil(w, () => Home(w), 10));
        // The wall opened again, a second salvage drone docked: it fetches the first, and the chunk, to the crates. The two
        // held where they fell, so they do not drift back in through the opening first.
        foreach (var s in w.M.Items.Floor) s.Vx = Content.Floor.SpaceDrift;
        for (int y = 2; y < 8; y++)
            foreach (int x in new[] { 26, 27 }) w.Do(new ClearAt(Cell(x, y), "structure"));
        w.Run();
        Assert.Empty(w.Refusals);
        w.Put(1, Salvager(), crate: Bay(w));
        Assert.True(RunUntil(w, () => w.M.Items.Floor.Count == 0 && Home(w), 2000));
        Assert.Equal(1, Held(w, Near(w), Tune.SalvageDrone) + Held(w, Far(w), Tune.SalvageDrone));
        Assert.Equal(1, Held(w, Near(w), "scrapChunk") + Held(w, Far(w), "scrapChunk"));
    }

    [Fact]
    public void OnAPlanetASalvageDroneNeverLaunches()
    {
        // The test strip opened beside the ship, a bay and a crate set on its ground, stacks lying under its open sky.
        var w = new Scenario(LogisticsTests.Rooms.Aggregate((a, b) => a + "\n" + b));
        w.Do(new Enter(w.At(2, 1))).Do(new OpenSurface(Content.Surfaces.TestStrip.Id)).Run();
        var m = w.On(1);
        int width = m.Grid.Width, ground = 30 * 2 * width;
        m.Grid.Place(ground - 4 * width + 20, new Placement("machine", Tune.Bay));
        m.Grid.Place(ground - 2 * width + 40, new Placement("machine", "crate"));
        w.Run();
        int bay = m.Grid.MachinesOf(Tune.Bay).Single().Id;
        var items = (ItemsState)m.Items.Slice;
        Stacks.SetCrate(items, bay, [Salvager(), null, null, null, new Stack(Content.Crew.Canister, 3)]);
        for (int x = 30; x < 50; x += 4) m.Items.LayDown(new Stack("plate", 5), ground - 20 * width + x);
        for (int i = 0; i < 400; i++)
        {
            w.Run();
            Assert.Empty(m.Logistics.Drones);
        }
        Assert.Empty(m.Debris.Pieces);
        Assert.Equal(3, m.Items.Crate(bay)[FuelSlot]!.Count);
    }

    [Fact]
    public void ASalvageDroneMidTripWithItsLoadSavesLoadsAndHashesTheSame()
    {
        var w = Out();
        Drift(w, "cargoCrate", 34, 6);
        Drift(w, "scrapChunk", 36, 10);
        Assert.True(RunUntil(w, () => Salvaging(w).Any(d => d.Loaded.Count > 0 && (d.X % 1000 != 500 || d.Y % 1000 != 500)), 400));
        var m = new Registry();
        var copy = World.Load(w.World.Save(), [.. m.All, new Builder(m)]);
        Assert.Equal(w.World.Hash(), copy.Hash());
        var saved = m.Logistics.Drones.Single();
        var live = Salvaging(w).Single();
        Assert.Equal((live.Air, live.Piece, live.Caught, live.Target), (saved.Air, saved.Piece, saved.Caught, saved.Target));
        Assert.Equal(live.Loaded, saved.Loaded);
        for (int i = 0; i < 600; i++)
        {
            w.World.Step([]);
            copy.Step([]);
        }
        Assert.Equal(w.World.Hash(), copy.Hash());
        Assert.Empty(m.Logistics.Drones);
    }

    [Fact]
    public void AskingReachedAndSuppliedOnOnePeerBetweenTicksKeepsTheHashes()
    {
        var a = Out();
        var b = Out();
        foreach (var w in new[] { a, b })
        {
            Drift(w, "scrapChunk", 32, 6);
            Drift(w, "scrapChunk", 36, 10);
            w.FillCrate(Far(w), ("plate", 5));
        }
        for (int i = 0; i < 400; i++)
        {
            foreach (var machine in a.M.Grid.Machines)
            {
                a.M.Logistics.Reached(machine.Id);
                a.M.Logistics.Supplied(machine.Id, "plate");
            }
            a.Run();
            b.Run();
            Assert.Equal(a.World.Hash(), b.World.Hash());
        }
        Assert.Equal(2, Held(a, Near(a), "scrapChunk"));
    }
}
