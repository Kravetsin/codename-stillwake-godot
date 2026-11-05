using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Rooms;
using Xunit;
using Air = Stillwake.Content.Air;
using Doors = Stillwake.Content.Doors;
using Materials = Stillwake.Content.Materials;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Sim.Tests;

/// <summary>
/// packages/sim/src/modules/atmosphere/air.test.ts, ported.
/// </summary>
public class AirTests
{
    /// <summary>A minute of ticks: leaks are counted over ten.</summary>
    private const int Minute = 60 * 20;

    private static readonly Placement Wall = Placement.Wall();

    /// <summary>
    /// Nine wide, doubled (0104) with its hull one cell thick (<see cref="AirTesting.OneThick"/>): a sealed room of 21 × 4 cells,
    /// a vent above a tank, hull of 20 × 2 scrap walls, all backed in scrap.
    /// </summary>
    private const string Room = """
        #########
        #V......#
        #TT.....#
        #TT.....#
        #########
        """;

    private static int At(int x, int y) => AirTesting.Cell(9, x, y);
    private const int Volume = 21 * 4;
    private const int Hull = 20 * 2;
    private static int Scrap => Materials.Get("scrap").Leak;
    private static int Iron => Materials.Get("iron").Leak;
    /// <summary>What its air touches leaks: the hull, and the back wall behind each of its cells (0020).</summary>
    private static int Leak => (Hull + Volume) * Scrap;

    /// <summary><see cref="Room"/> with <paramref name="rows"/> rows drawn inside: 7 characters a row, a hull of 14 + 2 × rows drawn.</summary>
    private static string Deep(int rows) =>
        string.Join("\n", new[] { "#########", "#V......#", "#TT.....#", "#TT.....#" }
            .Concat(Enumerable.Repeat("#.......#", rows - 3)).Append("#########"));

    private sealed record Setup(Scenario W, int Room, int[] Tanks)
    {
        public Scenario Fill(int canisters, int? tank = null)
        {
            for (int i = 0; i < canisters; i++) W.Do(new LoadCanister(tank ?? Tanks[0]));
            return W.Run();
        }
    }

    /// <summary>
    /// A world from <paramref name="map"/> in creative mode, its tank made the chain (<see cref="AirTesting.Chained"/>): a
    /// vaporizer where the tank was drawn, the tank under the hull, a panel powering the vaporizer; its vent piped to the
    /// vaporizer's output through a valve if asked.
    /// </summary>
    private static Setup SetUp(string map = Room, bool valve = false) => SetUp(AirTesting.Chained(map), valve);

    private static Setup SetUp(Layout drawn, bool valve = false)
    {
        var w = new Scenario(AirTesting.OneThick(drawn)).Do(new Items.SetCreative(true));
        // From the vent's port to the vaporizer's output, a span, or two through a valve on the vaporizer's top-left.
        int output = w.Output(At(1, 2));
        if (valve) w.Pipe(At(1, 1), At(1, 2), output).Do(new MakeValve(At(1, 2), true));
        else w.Pipe(At(1, 1), output);
        w.Run();
        var tanks = w.M.Grid.Machines.Where(m => m.Kind == "tank").Select(m => m.Id).ToArray();
        int room = w.M.Rooms.RoomAt(At(4, 2)) ?? throw new InvalidOperationException("set up");
        if (tanks.Length == 0 || room < 0) throw new InvalidOperationException("set up");
        return new Setup(w, room, tanks);
    }

    private static RoomStatus Status(Scenario w, int room) => w.M.Atmosphere.StatusOf(room);
    private static int Oxygen(Scenario w, int tank) => w.M.Atmosphere.OxygenIn(tank);
    private static List<int> AiredIn(Scenario w, int room) => w.Air().Aired.Where(c => w.M.Rooms.RoomAt(c) == room).ToList();

    /// <summary>
    /// Whether the orders kept room by room are those found afresh from the live vents, as after a load; the live vents
    /// fed ones; and the air of every room with a live vent the first cells of its order (2026-11-05-air-bubbles): a room
    /// with none keeps its air where it lay.
    /// </summary>
    private static bool FreshOrders(Scenario w)
    {
        var live = w.Air().Live ?? [];
        var fed = w.M.Grid.Machines.Where(m => Content.Machines.Is(m.Kind, Content.MachineTrait.Vents) && w.M.Atmosphere.Fed(m.Id)).Select(m => m.Cell).ToList();
        if (!live.All(fed.Contains) || !live.SequenceEqual(fed.Where(live.Contains))) return false;
        var roomOf = w.M.Rooms.RoomOfCells.ToArray();
        var fresh = Order.Rooms(roomOf, w.M.Grid.Width, w.M.Rooms.All, live, new bool[roomOf.Length]);
        var kept = w.Air().Orders;
        if (fresh.Count != kept.Count) return false;
        foreach (var (room, order) in fresh)
        {
            if (!kept.TryGetValue(room, out var other) || !other.SequenceEqual(order)) return false;
            if (!live.Any(v => roomOf[v] == room)) continue;
            int aired = order.Count(w.M.Atmosphere.AiredAt);
            if (!order.Take(aired).All(w.M.Atmosphere.AiredAt)) return false;
        }
        return true;
    }

    /// <summary>Steps along rows and columns from the vent's top-left cell, (1, 1) as <see cref="Room"/> draws it, the same as through the room.</summary>
    private static int FromVent(int cell) => Math.Abs(cell % 18 - At(1, 1) % 18) + Math.Abs(cell / 18 - At(1, 1) / 18);

    /// <summary>Runs until the room is aired, at most <paramref name="limit"/> ticks.</summary>
    private static void AirUp(Scenario w, int room, int limit = 1000)
    {
        for (int i = 0; i < limit && Status(w, room) != RoomStatus.Aired; i++) w.Run();
        Assert.Equal(RoomStatus.Aired, Status(w, room));
    }

    /// <summary>Runs until the chain is dry, its tank's liquid and its vaporizer's gas both gone, at most <paramref name="limit"/> ticks.</summary>
    private static void DryOut(Scenario w, int limit = 20000)
    {
        for (int i = 0; i < limit && w.Held() > 0; i++) w.Run();
        Assert.Equal(0, w.Held());
    }

    // Air.

    [Fact]
    public void DrawsFromTheTanksOfANetworkOneByOneInIdOrder()
    {
        var w = SetUp().W;
        // A second tank, built after the first, joins the vaporizer's input network: a pipe from its output, three cells right of its input, to the input.
        w.Do(new Place(At(5, 2), new Placement("machine", "tank"))).Run();
        w.Pipe(At(5, 3) + 3, At(1, 3)).Run();
        var tanks = w.M.Grid.MachinesOf("tank").Select(m => m.Id).ToArray();
        int first = tanks[0], second = tanks[1];
        Assert.Equal(new[] { first, second }, w.M.Pipes.Network(w.M.Pipes.NetworkAt(At(1, 3))!.Value)!.Machines.Where(tanks.Contains));
        // Loaded on one tick, so the vaporizer has both to take from from its first unit.
        w.Do(new LoadCanister(second)).Do(new LoadCanister(first));
        w.Run(4);
        // A unit a tick at full power, all of it from the first.
        Assert.Equal(Air.Canister - 4, Oxygen(w, first));
        Assert.Equal(Air.Canister, Oxygen(w, second));
        Assert.Equal(RoomStatus.Filling, Status(w, w.M.Rooms.RoomAt(At(4, 2))!.Value));
    }

    [Fact]
    public void LeaksThroughItsHullAndBackWallsByTheirMaterialsAndADoorByItsOwnOverAPeriod()
    {
        // The right wall's middle is a closed door, and the left wall is iron: of the forty hull cells, two
        // leak as a door, six as iron, the rest as scrap; each cell's scrap back wall leaks too, but not
        // the ones behind the door, which touch no air.
        var s = SetUp("""
            #########
            #V......#
            #TT.....D
            #TT.....#
            #########
            """);
        var w = s.W;
        // The left wall's one cell thick: the right column of its blocks, taken down and built again a tick after, as a player would.
        int[] left = [.. new[] { 1, 2, 3 }.SelectMany(y => new[] { At(0, y) + 1, At(0, y) + 1 + 18 })];
        foreach (int cell in left) w.Do(new ClearAt(cell, "structure"));
        w.Run();
        foreach (int cell in left) w.Do(new Place(cell, Placement.Wall("iron")));
        w.Run();
        s.Fill(2);
        AirUp(w, s.Room);
        int before = w.Held();
        int leak = (Hull - 2 * 4 + Volume) * Scrap + 2 * 3 * Iron + 2 * Air.DoorLeak;
        Assert.Equal(leak, Of(w, s.Room)!.Leak);
        // Spread over the period, paid by the chain: about half by half way, all of it by its end.
        w.Run(Air.LeakEvery / 2);
        Assert.InRange(before - w.Held(), leak / 2, (leak + 1) / 2);
        w.Run(Air.LeakEvery / 2);
        Assert.Equal(before - leak, w.Held());
        Assert.Equal(Volume, w.Air().Aired.Count);
    }

    [Fact]
    public void VentsThroughABreachTheAirGoesOverAFewTicksAndTheVentDrainsItsNetwork()
    {
        var s = SetUp(Room, valve: true);
        var w = s.W;
        s.Fill(3);
        AirUp(w, s.Room);
        // The store refills once the room is aired, for the vent to have its drain to take.
        w.Run(Air.GeneratorStore);
        Assert.True(w.Gas() >= 3 * Air.VentDrain);
        int before = w.Held();
        w.Do(new ClearAt(At(8, 2), "structure")).Run();
        Assert.Equal(RoomStatus.Venting, Status(w, s.Room));
        // The breach is a cell of the room now, with air or without.
        Assert.True(AiredIn(w, s.Room).Count <= Volume + 1 - Air.VentLoss);
        Assert.True(AiredIn(w, s.Room).Count > 0);
        Assert.Equal(before - Air.VentDrain, w.Held());
        w.Run(2);
        Assert.Equal(before - 3 * Air.VentDrain, w.Held());
        w.Run((Volume + Air.VentLoss - 1) / Air.VentLoss);
        Assert.Empty(w.Air().Aired);
    }

    [Fact]
    public void StopsVentingFromANetworkOnceAValveCutsTheVentOff()
    {
        var s = SetUp(Room, valve: true);
        var w = s.W;
        s.Fill(3);
        AirUp(w, s.Room);
        w.Do(new SetValve(At(1, 2), false)).Do(new ClearAt(At(8, 2), "structure")).Run();
        int cut = w.Held();
        w.Run(5);
        Assert.Equal(RoomStatus.Venting, Status(w, s.Room));
        // The vaporizer goes on filling its store behind the valve, and nothing leaves the chain.
        Assert.Equal(cut, w.Held());
    }

    [Fact]
    public void FillsAgainOnceTheBreachIsSealed()
    {
        var s = SetUp();
        var w = s.W;
        s.Fill(3);
        AirUp(w, s.Room);
        w.Do(new ClearAt(At(8, 2), "structure")).Run(2);
        w.Do(new Place(At(8, 2), Wall)).Run();
        Assert.Equal(RoomStatus.Filling, Status(w, s.Room));
        AirUp(w, s.Room);
    }

    [Fact]
    public void DrainsANetworkThroughAVentLeftInSpaceAsThroughAWideHole()
    {
        var s = SetUp();
        var w = s.W;
        s.Fill(3);
        AirUp(w, s.Room);
        w.Run(Air.GeneratorStore);
        Assert.True(w.Gas() >= 2 * Air.VentDrain);
        foreach (int y in new[] { 1, 2 })
            w.Do(new ClearAt(At(8, y), "structure")).Do(new ClearAt(At(8, y) + 18, "structure"));
        w.Run();
        Assert.Equal(RoomsModule.Space, w.M.Rooms.RoomAt(At(1, 1)));
        int before = w.Held();
        w.Run();
        Assert.Equal(before - Air.VentDrain, w.Held());
    }

    // Two rooms and a door.

    private const string Two = """
        ###############
        #V......#.....#
        #TT.....D.....#
        #TT.....#.....#
        ###############
        """;

    private static int At15(int x, int y) => AirTesting.Cell(15, x, y);

    /// <summary>
    /// <see cref="Two"/> made the chain (<see cref="AirTesting.Chained"/>), its left room aired from three canisters and its
    /// vaporizer's store refilled after, so the vent fills <c>Air.CellsPerFill</c> cells a beat again; its vent's pipe cut
    /// after if asked.
    /// </summary>
    private static (Scenario W, int Left, int Door, int Right) TwoRooms(bool cut = false)
    {
        var w = new Scenario(AirTesting.OneThick(AirTesting.Chained(Two))).Do(new Items.SetCreative(true));
        int vent = At15(1, 1), output = w.Output(At15(1, 2));
        w.Pipe(vent, output).Run();
        int tank = w.M.Grid.Machines.FirstOrDefault(m => m.Kind == "tank")?.Id ?? 0;
        for (int i = 0; i < 3; i++) w.Do(new LoadCanister(tank));
        w.Run();
        int left = w.M.Rooms.RoomAt(At15(4, 2))!.Value;
        AirUp(w, left);
        w.Run(Air.GeneratorStore);
        Assert.True(w.Gas() >= 3 * Air.CellsPerFill);
        if (cut) w.Do(new CutPipe(vent, output)).Run();
        return (w, left, At15(8, 2), At15(10, 2));
    }

    [Fact]
    public void TakesTheAirOutOfACellAWallIsBuiltOn()
    {
        var s = SetUp();
        var w = s.W;
        s.Fill(1);
        AirUp(w, s.Room);
        w.Do(new Place(At(5, 2), Wall)).Run();
        Assert.DoesNotContain(At(5, 2), w.Air().Aired);
    }

    [Fact]
    public void LaysARoomOutFromItsMiddleBesideAVentWithNothingBehindItAndAroundItOnceFed()
    {
        var w = new Scenario("#########\n#.....GG#\n#.....GG#\n#.......#\n#########");
        int room = w.M.Rooms.All[0].Id;
        int middle = w.Air().Orders[room][0];
        w.Do(new Place(At(1, 1), new Placement("machine", "vent"))).Run();
        int vent = w.M.Grid.MachineAt(At(1, 1))!.Id;
        // A vent with nothing behind it blows nothing: the room's air does not gather at it, piped to nowhere or not.
        Assert.Equal(middle, w.Air().Orders[room][0]);
        Assert.False(w.M.Atmosphere.Fed(vent));
        w.GiveItems(0, ("tube", 6)).Pipe(Port(w, At(1, 1)), At(3, 3)).Run();
        Assert.Equal(middle, w.Air().Orders[room][0]);
        Assert.False(w.M.Atmosphere.Fed(vent));
        // Fed once piped to a vaporizer's output, but dry, nothing behind it: not live, so still not (2026-11-05-air-bubbles).
        w.Pipe(At(3, 3), w.Output(At(6, 1))).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Atmosphere.Fed(vent));
        Assert.Equal(middle, w.Air().Orders[room][0]);
        // Gas in the vaporizer's store makes it live: the air gathers at it.
        Store(w, w.M.Grid.MachineAt(At(6, 1))!.Id, Air.GeneratorStore);
        w.Run();
        Assert.Equal(At(1, 1), w.Air().Orders[room][0]);
        Assert.True(FreshOrders(w));
    }

    /// <summary>Puts <paramref name="gas"/> into vaporizer <paramref name="vaporizer"/>'s store, as a map with no tank behind it cannot.</summary>
    private static void Store(Scenario w, int vaporizer, int gas)
    {
        w.Air().Tanks.Add(new TankAir { Tank = vaporizer, Oxygen = gas });
        w.Air().Tanks.Sort((a, b) => a.Tank.CompareTo(b.Tank));
        w.Reload();
    }

    private static int Port(Scenario w, int cell) => w.M.Grid.PortsOf(w.M.Grid.MachineAt(cell)!).First();

    [Fact]
    public void LaysARoomOutAgainWhenItsVentsOnlyChangeOrderAVentRebuiltWhereItStood()
    {
        var w = new Scenario("#########\n#V.GG..V#\n#..GG...#\n#########");
        // Both vents fed by the vaporizer between them, through its output.
        w.GiveItems(0, ("tube", 6)).Pipe(Port(w, At(1, 1)), w.Output(At(3, 1)), Port(w, At(7, 1))).Run();
        Assert.Empty(w.Refusals);
        // Live while its store holds gas.
        Store(w, w.M.Grid.MachineAt(At(3, 1))!.Id, Air.GeneratorStore);
        w.Run();
        int room = w.M.Rooms.All[0].Id;
        Assert.Equal(At(1, 1), w.Air().Orders[room][0]);
        // Taken apart and built again on one tick: the same cells, the vent now last in id order.
        w.Do(new ClearAt(At(1, 1), "machine")).Do(new Place(At(1, 1), new Placement("machine", "vent"))).Run();
        Assert.Equal(At(7, 1), w.Air().Orders[room][0]);
        Assert.True(FreshOrders(w));
    }

    // Rooms found again around a change far from space: only the rooms it touches are redone.

    /// <summary>Three rooms in a row, doors between, a wall stub in the first; a cell of space all round, so a change inside stays far from it.</summary>
    private static readonly string[] Row =
    [
        "                       ",
        " ##################### ",
        " #V......#.....#.....# ",
        " #TT.....#.....#.....# ",
        " #TT.....D.....D.....# ",
        " #....#..#.....#.....# ",
        " #.......#.....#.....# ",
        " ##################### ",
        "                       ",
    ];

    private static int At23(int x, int y) => AirTesting.Cell(23, x, y);

    /// <summary>
    /// <see cref="Row"/> with its first room aired, its tank made the chain (<see cref="AirTesting.Vaporized"/>, the chain's
    /// tank and panel in space below the row) and its vent piped to the vaporizer's output.
    /// </summary>
    private static (Scenario W, int Left) AiredRow()
    {
        // The stub right of the tank's bottom row is what Vaporized asks for.
        var w = new Scenario(AirTesting.Vaporized(Row, ["", "", "", "", "    -"]), doubled: true).Do(new Items.SetCreative(true));
        w.Pipe(At23(2, 2), w.Output(At23(2, 3))).Run();
        int tank = w.M.Grid.Machines.First(m => m.Kind == "tank").Id;
        for (int i = 0; i < 3; i++) w.Do(new LoadCanister(tank));
        w.Run();
        int left = w.M.Rooms.RoomAt(At23(4, 4))!.Value;
        AirUp(w, left);
        return (w, left);
    }

    /// <summary>Runs <paramref name="c"/> and checks the rooms were found around it, not from scratch.</summary>
    private static void AroundA(Scenario w, Command c)
    {
        int version = w.M.Rooms.Version;
        w.Do(c).Run();
        Assert.NotNull(w.M.Rooms.ChangedSince(version));
    }

    [Fact]
    public void TakesTheAirOutOfACellAWallIsBuiltOnFarFromSpaceAndKeepsTheRest()
    {
        var (w, left) = AiredRow();
        AroundA(w, new Place(At23(4, 4), Wall));
        Assert.DoesNotContain(At23(4, 4), w.Air().Aired);
        Assert.Equal(RoomStatus.Aired, Status(w, left));
        Assert.Equal(w.M.Rooms.RoomById(left)!.Volume, AiredIn(w, left).Count);
    }

    [Fact]
    public void AirsTheCellOfAWallTakenOutOfAnAiredRoomAndLeaksThroughTheBackWallBehindIt()
    {
        var (w, left) = AiredRow();
        var before = Of(w, left)!;
        int volume = w.M.Rooms.RoomById(left)!.Volume;
        AroundA(w, new ClearAt(At23(6, 5), "structure"));
        // The leak is paid before the fill: a tick or two, as the store refills, for the vent to reach it.
        w.Run(5);
        Assert.Equal(volume + 1, w.M.Rooms.RoomById(left)!.Volume);
        Assert.Contains(At23(6, 5), w.Air().Aired);
        Assert.Equal(RoomStatus.Aired, Status(w, left));
        Assert.Equal(before.Leak + Scrap, Of(w, left)!.Leak);
    }

    /// <summary>
    /// Eight rooms in a row, each with its own chain drawn whole (<see cref="AirTesting.Vaporized"/>): a vent piped to its
    /// vaporizer's output, a canister in its tank.
    /// </summary>
    private static Layout Eight()
    {
        string[] tile = ["########", "#V.....#", "#TT....#", "#TT....#", "########"];
        string[] under = ["", " ---", " o -", "   -"];
        string[] Repeat(string[] of) => [.. of.Select(r => string.Concat(Enumerable.Repeat(r.PadRight(8), 8)))];
        return AirTesting.Vaporized(Repeat(tile), Repeat(under));
    }

    /// <summary>More than a few cells a tick: the aired cells are merged into a new list, not edited in place.</summary>
    [Fact]
    public void VentsEightBreachedRoomsAtOnceTheirVentLossEachFromTheFarEdge()
    {
        var w = new Scenario(Eight(), doubled: true);
        w.Run(300);
        Assert.All(w.M.Rooms.All, r => Assert.Equal(RoomStatus.Aired, Status(w, r.Id)));
        var before = w.Air().Aired.ToList();
        // Through the top of the hull, both of its cells thick, above the first open cell of eight tiles.
        for (int tile = 0; tile < 8; tile++)
            w.Do(new ClearAt(w.At(tile * 8 + 3, 0), "structure")).Do(new ClearAt(w.At(tile * 8 + 3, 0) + w.Layout.Width, "structure"));
        w.Run();
        var after = w.Air().Aired;
        Assert.Equal(8 * Air.VentLoss, before.Count - after.Count);
        Assert.True(after.Zip(after.Skip(1)).All(p => p.First < p.Second));
        Assert.True(FreshOrders(w));
    }

    /// <summary>
    /// Doors toggled and walls built and taken apart, far from space and beside the hull, give the
    /// same air as a world loaded from a save, which finds every order, leak and survey afresh.
    /// </summary>
    [Property(MaxTest = 40)]
    public Property KeepsTheAirOfRoomsFoundAroundAChangeAsAWorldLoadedAfreshWould()
    {
        int[] cells = [At23(9, 4), At23(15, 4), At23(4, 4), At23(6, 5), At23(12, 3), At23(12, 4), At23(9, 3), At23(15, 5), At23(5, 2), At23(18, 6)];
        var step = from cell in Gen.Elements(cells) from ticks in Gen.Choose(1, 40) select (cell, ticks);
        var steps = from n in Gen.Choose(1, 12) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var (w, _) = AiredRow();
            foreach (var (cell, ticks) in all)
            {
                int structure = w.M.Grid.StructureAt(cell);
                w.Do(structure is Structure.DoorClosed or Structure.DoorOpen ? new SetDoor(cell, structure == Structure.DoorClosed)
                    : structure == Structure.Empty ? new Place(cell, Wall) : new ClearAt(cell, "structure"));
                w.Run(ticks);
                if (!FreshOrders(w)) return false.Label($"orders after {cell}");
                var m = new Registry();
                var loaded = World.Load(w.World.Save(), [.. m.All, new Builder(m)]);
                if (!m.Atmosphere.RoomsAir().SequenceEqual(w.M.Atmosphere.RoomsAir())) return false.Label($"rooms' air after {cell}");
                loaded.Step([]);
                w.World.Step([]);
                if (loaded.Hash() != w.World.Hash()) return false.Label($"the air a tick after {cell}");
            }
            return true.ToProperty();
        });
    }

    private sealed record Step(string Type, int Cell = 0, int Ticks = 0);

    /// <summary>
    /// fast-check's steps: loads, valve toggles, breaches, seals, vents built and taken away, a hero
    /// breathing, and waits of up to 1300 ticks, mostly short as fast-check draws them.
    /// </summary>
    private static readonly Gen<Step> Steps = Gen.OneOf(
        Gen.Constant(new Step("load")),
        Gen.Constant(new Step("valve")),
        Gen.Elements(At(8, 2), At(8, 1), At(0, 2) + 1, At(5, 2)).Select(c => new Step("breach", c)),
        Gen.Elements(At(8, 2), At(8, 1), At(0, 2) + 1, At(5, 1), At(5, 2), At(5, 3)).Select(c => new Step("seal", c)),
        Gen.Elements(At(6, 1), At(4, 3)).Select(c => new Step("vent", c)),
        Gen.Elements(At(3, 1), At(7, 3)).Select(c => new Step("breathe", c)),
        Gen.Frequency((2, Gen.Choose(1, 20)), (1, Gen.Choose(1, 1300)), (1, Gen.Elements(1, 1300)))
            .Select(t => new Step("wait", Ticks: t)));

    /// <summary>The checks of the invariant test, after every step; null when all hold, else what broke.</summary>
    private static string? Broken(Scenario w, int tank)
    {
        int o = Oxygen(w, tank);
        if (o < 0 || o > Air.TankCapacity) return $"tank holds {o}";
        // Only tanks holding oxygen are listed.
        if (w.Air().Tanks.Any(t => t.Oxygen <= 0)) return "an empty tank is listed";
        var cells = w.Air().Aired;
        for (int i = 0; i < cells.Count; i++)
        {
            if (w.M.Rooms.RoomAt(cells[i]) is not >= 0) return $"cell {cells[i]} is aired in no room";
            if (i > 0 && cells[i] <= cells[i - 1]) return "aired cells are not sorted and distinct";
        }
        return FreshOrders(w) ? null : "the orders kept are not those found afresh";
    }

    [Property(MaxTest = 60)]
    public Property KeepsTanksWithinTheirCapacityAndAirInRoomsWhateverHappens()
    {
        var steps = from n in Gen.Choose(0, 30) from s in Steps.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var s = SetUp(Room, valve: true);
            var w = s.W;
            int tank = s.Tanks[0];
            foreach (var step in all)
            {
                switch (step.Type)
                {
                    case "load": w.Do(new LoadCanister(tank)); break;
                    case "valve": w.Do(new SetValve(At(1, 2), !w.M.Pipes.ValveAt(At(1, 2))!.Open)); break;
                    case "breach": w.Do(new ClearAt(step.Cell, "structure")); break;
                    case "breathe": w.Do(new Enter(step.Cell)); break;
                    case "vent":
                        w.Do(w.M.Grid.MachineAt(step.Cell) != null
                            ? new ClearAt(step.Cell, "machine")
                            : new Place(step.Cell, new Placement("machine", "vent")));
                        break;
                    case "seal":
                        if (w.M.Grid.StructureAt(step.Cell) == Structure.Empty) w.Do(new Place(step.Cell, Wall));
                        break;
                }
                w.Run(step.Type == "wait" ? step.Ticks : 1);
                var broken = Broken(w, tank);
                if (broken != null) return false.Label(broken);
            }
            // Loaded between beats, a world plays on exactly as the one that never stopped.
            var m = new Registry();
            var loaded = World.Load(w.World.Save(), [.. m.All, new Builder(m)]);
            for (int i = 0; i < Air.TicksPerCell * 3 + 1; i++)
            {
                if (loaded.Hash() != w.World.Hash()) return false.Label($"hashes differ {i} ticks after the load");
                loaded.Step([]);
                w.World.Step([]);
            }
            return true.ToProperty();
        });
    }

    // Rooms air.

    private static RoomAir? Of(Scenario w, int room) => w.M.Atmosphere.RoomsAir().FirstOrDefault(r => r.Room == room);

    [Fact]
    public void TellsAnAiredRoomItsLeakTheOxygenBehindItAndHowLongThatPaysTheLeak()
    {
        var s = SetUp();
        var w = s.W;
        s.Fill(1);
        AirUp(w, s.Room);
        // The liquid behind its vaporizer, which makes more than the room leaks: the liquid leaves at the leak.
        int held = Oxygen(w, s.Tanks[0]);
        Assert.True(Leak < Air.GeneratorRate);
        Assert.Equal(
            new RoomAir(s.Room, RoomStatus.Aired, Volume, Leak, held, Air.GeneratorRate, (int)((long)held * Air.LeakEvery / Leak), null),
            Of(w, s.Room));
    }

    private static int? LeakAt(Scenario w, int cell) => Of(w, w.M.Rooms.RoomAt(cell)!.Value)?.Leak;

    [Fact]
    public void LeaksThroughEachBackWallByItsMaterialIronBehindEveryCellAThirdOfScrap()
    {
        var s = SetUp();
        var w = s.W;
        // Taken down and built again a tick after, as a player would: a room with no back wall is open to space between.
        var cells = Enumerable.Range(0, 18 * 10).Where(c => w.M.Rooms.RoomAt(c) == s.Room).ToList();
        foreach (int cell in cells) w.Do(new ClearAt(cell, "back"));
        w.Run();
        foreach (int cell in cells) w.Do(new Place(cell, Placement.BackWall("iron")));
        w.Run();
        s.Fill(1);
        AirUp(w, w.M.Rooms.RoomAt(At(4, 2))!.Value);
        Assert.Equal(Hull * Scrap + Volume * Iron, LeakAt(w, At(4, 2)));
    }

    [Fact]
    public void LeaksNothingThroughAnEmptyCellOnlyThroughWhatItsAirTouches()
    {
        var s = SetUp();
        var w = s.W;
        Assert.Equal(0, LeakAt(w, At(4, 2)));
        s.Fill(1);
        AirUp(w, s.Room);
        Assert.Equal(Leak, LeakAt(w, At(4, 2)));
    }

    [Fact]
    public void LeaksNothingThroughABackWallBehindAWallNoAirTouchesIt()
    {
        var s = SetUp();
        var w = s.W;
        w.Do(new Place(At(5, 2), Wall)).Run();
        s.Fill(1);
        AirUp(w, s.Room);
        Assert.Equal((Hull + Volume - 1) * Scrap, LeakAt(w, At(4, 2)));
    }

    [Fact]
    public void TellsAVentingRoomHowSoonItsVentDrainsTheNetwork()
    {
        var s = SetUp();
        var w = s.W;
        s.Fill(1);
        w.Do(new ClearAt(At(8, 2), "structure")).Run();
        int held = Oxygen(w, s.Tanks[0]);
        var air = Of(w, s.Room)!;
        // The vent drains more than the vaporizer makes: the liquid leaves at the vaporizer's unit a tick.
        Assert.True(Air.VentDrain * Air.LeakEvery > Air.GeneratorRate);
        Assert.Equal((RoomStatus.Venting, 0, (int?)held, (int?)(held * Air.LeakEvery / Air.GeneratorRate)),
            (air.Status, air.Aired, air.Oxygen, air.Lasts));
    }

    [Fact]
    public void CountsANetworkOnceForOxygenAndOncePerVentForTheDrain()
    {
        // Two vents in the room, one network: the left vent's pipe runs through a joint on to the right one.
        var s = SetUp("""
            #########
            #V.....V#
            #TT.....#
            #TT.....#
            #########
            """);
        var w = s.W;
        w.Pipe(At(1, 1), At(4, 1), At(7, 1));
        s.Fill(1);
        AirUp(w, s.Room);
        w.Run(Air.GeneratorStore);
        Assert.True(w.Gas() >= 2 * Air.VentDrain);
        int before = w.Held();
        w.Do(new ClearAt(At(8, 2), "structure")).Run();
        // Each vent drains the network from the vaporizer's store: two vents, twice the drain.
        Assert.Equal(before - 2 * Air.VentDrain, w.Held());
        int held = Oxygen(w, s.Tanks[0]);
        var air = Of(w, s.Room)!;
        // The network once: its tank's liquid, its vaporizer's making, and the liquid leaving at that unit a tick.
        Assert.Equal(((int?)held, Air.GeneratorRate, (int?)(held * Air.LeakEvery / Air.GeneratorRate)), (air.Oxygen, air.Made, air.Lasts));
    }

    [Fact]
    public void SaysWhyARoomStaysInVacuumNoVentAVentWithNoPipeOrPipesWithNoOxygen()
    {
        var bare = new Scenario("#####\n#...#\n#####");
        var none = Of(bare, bare.M.Rooms.All[0].Id)!;
        Assert.Equal((RoomStatus.Vacuum, "atmosphere.noVent"), (none.Status, none.Problem));

        // A vent with no pipe to the vaporizer beside it: nothing feeds the room, so it has no oxygen to show either.
        var unpiped = new Scenario(AirTesting.OneThick(AirTesting.Chained(Room)));
        var room = Of(unpiped, unpiped.M.Rooms.RoomAt(At(4, 2))!.Value)!;
        Assert.Equal(("atmosphere.notPiped", (int?)null, (int?)null), (room.Problem, room.Oxygen, room.Lasts));

        var s = SetUp();
        var dry = Of(s.W, s.Room)!;
        Assert.Equal((RoomStatus.Vacuum, "atmosphere.dry", (int?)0), (dry.Status, dry.Problem, dry.Oxygen));
    }

    // A room is a tank of air (2026-11-05-air-bubbles).

    /// <summary>
    /// One room, two chains: each vent fed by its own vaporizer, each vaporizer by its own tank under the hull; the left
    /// vent's chain first, the right's piped after. Neither tank holds liquid yet.
    /// </summary>
    private static (Setup S, int LeftVaporizer, int RightVaporizer) TwoChains()
    {
        var s = SetUp(Layout.Parse(
            [
                "#########",
                "#V.....V#",
                "#GG...GG#",
                "#GG...GG#",
                "#########",
                "",
                " TT   TT",
                " TT   TT",
                " PPP PPP",
            ],
            // Each tank's liquid from its output on its right, round it and up to its vaporizer's input.
            ["", "", "", " -    -", " -    -", " ---  ---", "   -    -", "  --   --"],
            [((1, 8), (1, 2)), ((5, 8), (6, 2))]));
        s.W.Pipe(At(7, 1), s.W.Output(At(6, 2))).Run();
        return (s, s.W.M.Grid.MachineAt(At(1, 2))!.Id, s.W.M.Grid.MachineAt(At(6, 2))!.Id);
    }

    /// <summary>
    /// One of two chains in a room runs dry, its tank taken apart and its store spent: its vent is no longer live, and the
    /// air is laid out round the other, as much of it as there was. Saved and loaded on any of those ticks, a world plays
    /// on as the one that never stopped (2026-11-05-air-bubbles).
    /// </summary>
    [Fact]
    public void LaysTheAirOutRoundTheOtherVentWhenOneChainRunsDryAndPlaysOnAlikeLoadedThen()
    {
        var (s, left, _) = TwoChains();
        var w = s.W;
        s.Fill(1, s.Tanks[0]);
        s.Fill(1, s.Tanks[1]);
        w.Run(60);
        int room = s.Room;
        Assert.Equal(At(1, 1), w.Air().Orders[room][0]);
        w.Do(new ClearAt(w.M.Grid.MachineById(s.Tanks[0])!.Cell, "machine")).Run();
        Assert.Empty(w.Refusals);
        for (int i = 0; i < 400 && w.Air().Live!.Contains(At(1, 1)); i++)
        {
            var m = new Registry();
            var loaded = World.Load(w.World.Save(), [.. m.All, new Builder(m)]);
            for (int t = 0; t < 3; t++)
            {
                loaded.Step([]);
                w.World.Step([]);
                Assert.Equal(w.World.Hash(), loaded.Hash());
            }
        }
        Assert.Equal(0, Oxygen(w, left));
        Assert.Equal([At(7, 1)], w.Air().Live);
        Assert.Equal(At(7, 1), w.Air().Orders[room][0]);
        Assert.True(FreshOrders(w));
    }

    /// <summary>
    /// Whatever is loaded into either chain and however long it runs, a tick airs only cells joined, through cells holding
    /// air in their room, to a vent whose chain held oxygen as the tick began or ended: air spreads from the vents that pay for it.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property AirsOnlyCellsJoinedThroughAirToAVentThatPays()
    {
        var step = Gen.OneOf(
            Gen.Elements(0, 1).Select(chain => (Load: chain, Ticks: 0)),
            Gen.Frequency((3, Gen.Choose(1, 40)), (1, Gen.Choose(40, 400))).Select(t => (Load: -1, Ticks: t)));
        var steps = from n in Gen.Choose(1, 10) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var (s, left, right) = TwoChains();
            var w = s.W;
            int width = w.M.Grid.Width;
            var roomOf = w.M.Rooms.RoomOfCells;
            (int Vent, int Vaporizer, int Tank)[] chains = [(At(1, 1), left, s.Tanks[0]), (At(7, 1), right, s.Tanks[1])];
            foreach (var (load, ticks) in all)
            {
                if (load >= 0) w.Do(new LoadCanister(chains[load].Tank));
                for (int t = 0; t < Math.Max(ticks, 1); t++)
                {
                    bool Holds((int Vent, int Vaporizer, int Tank) c) => Oxygen(w, c.Vaporizer) > 0 || Oxygen(w, c.Tank) > 0;
                    var held = chains.Where(Holds).Select(c => c.Vent).ToList();
                    var before = w.Air().Aired.ToHashSet();
                    w.Run();
                    var aired = w.Air().Aired.ToHashSet();
                    // From the paying vents, through the cells holding air now.
                    // A canister loaded lands before the tick's air, so a chain holding oxygen after it could pay too.
                    var paying = held.Union(chains.Where(Holds).Select(c => c.Vent));
                    var reached = new HashSet<int>(paying.Where(aired.Contains));
                    var queue = new Queue<int>(reached);
                    while (queue.Count > 0)
                    {
                        int cell = queue.Dequeue();
                        foreach (int next in new[] { cell - width, cell - 1, cell + 1, cell + width })
                            if (aired.Contains(next) && roomOf[next] == roomOf[cell] && reached.Add(next)) queue.Enqueue(next);
                    }
                    var stray = aired.Where(c => !before.Contains(c) && !reached.Contains(c)).ToList();
                    if (stray.Count > 0) return false.Label($"cell {stray[0]} aired away from every paying vent");
                }
                if (!FreshOrders(w)) return false.Label("the orders kept are not those found afresh");
            }
            return true.ToProperty();
        });
    }

    [Fact]
    public void FillsARoomTooBigForItsVaporizerTillItsAirLeaksWhatItMakesAndHoldsThere()
    {
        var s = SetUp(Deep(160));
        var w = s.W;
        int volume = w.M.Rooms.RoomById(s.Room)!.Volume;
        // The whole room leaks more than twice what one vaporizer makes.
        Assert.InRange(volume * Scrap, 2 * Air.GeneratorRate, int.MaxValue);
        s.Fill(4);
        // The last cells come slower and slower, as each leaks a share of what is made: a dozen thousand ticks to settle.
        w.Run(2 * Air.LeakEvery);
        int held = w.Air().Aired.Count;
        // Each cell leaks its scrap back wall, a few the hull too: a few hundred cells hold what a vaporizer makes.
        Assert.InRange(held, Air.GeneratorRate / (2 * Scrap), Air.GeneratorRate / Scrap);
        Assert.Equal(RoomStatus.Filling, Status(w, s.Room));
        var air = Of(w, s.Room)!;
        Assert.InRange(air.Leak, Air.GeneratorRate - 4 * Scrap, Air.GeneratorRate + 4 * Scrap);
        w.Run(Air.LeakEvery);
        Assert.InRange(w.Air().Aired.Count, held - 4, held + 4);
    }

    // Bubbles (2026-11-05-air-bubbles).

    /// <summary><see cref="Deep"/>'s room, its back walls taken down and built again of <paramref name="back"/>, four canisters in, run till its bubble stands.</summary>
    private static Setup Settled(string back, int rows = 40)
    {
        var s = SetUp(Deep(rows));
        var w = s.W;
        if (back != "scrap")
        {
            var cells = Enumerable.Range(0, w.M.Rooms.RoomOfCells.Length).Where(c => w.M.Rooms.RoomAt(c) == s.Room).ToList();
            foreach (int cell in cells) w.Do(new ClearAt(cell, "back"));
            w.Run();
            foreach (int cell in cells) w.Do(new Place(cell, Placement.BackWall(back)));
            w.Run();
        }
        s.Fill(4);
        w.Run(800);
        return s;
    }

    [Fact]
    public void HoldsAsManyCellsRoundItsVentAsItsMakingPaysTheLeakOfMoreBehindGlassThanScrap()
    {
        var scrap = Settled("scrap");
        var w = scrap.W;
        int held = w.Air().Aired.Count;
        // Each cell leaks its scrap back wall, those by the hull the hull too: a few hundred cells hold what a vaporizer makes.
        Assert.InRange(held, Air.GeneratorRate / (2 * Scrap), Air.GeneratorRate / Scrap);
        var air = Of(w, scrap.Room)!;
        Assert.Equal((RoomStatus.Filling, "atmosphere.leaky"), (air.Status, air.Problem));
        Assert.InRange(air.Leak, Air.GeneratorRate - 2 * Scrap, Air.GeneratorRate);
        // It stays there, neither growing nor shrinking.
        w.Run(400);
        Assert.Equal(held, w.Air().Aired.Count);
        // Behind glass the same vaporizer holds a whole room twice what scrap held, its scrap hull leaking the most.
        var glass = Settled("glass", rows: 30);
        Assert.Equal(glass.W.M.Rooms.RoomById(glass.Room)!.Volume, glass.W.Air().Aired.Count);
        Assert.True(glass.W.Air().Aired.Count > 2 * held);
        Assert.Equal(RoomStatus.Aired, Status(glass.W, glass.Room));
        Assert.True(FreshOrders(w) && FreshOrders(glass.W));
    }

    [Fact]
    public void SpreadsItsBubbleThroughAnOpenDoorAndLetsThePartPastItFadeWhereItLiesOnceShut()
    {
        // Two rooms deep, a vent in the left: open, its bubble reaches a little way past the door into the right.
        string map = string.Join("\n", new[] { "###############", "#V......#.....#", "#TT.....D.....#", "#TT.....#.....#" }
            .Concat(Enumerable.Repeat("#.......#.....#", 37)).Append("###############"));
        var w = new Scenario(AirTesting.OneThick(AirTesting.Chained(map))).Do(new Items.SetCreative(true));
        w.Pipe(At15(1, 1), w.Output(At15(1, 2))).Run();
        int tank = w.M.Grid.MachinesOf("tank").Single().Id, door = At15(8, 2);
        for (int i = 0; i < 4; i++) w.Do(new LoadCanister(tank));
        w.Do(new SetDoor(door, true)).Run(800);
        int left = w.M.Rooms.RoomAt(At15(4, 2))!.Value;
        bool Past(int c) => c % 30 > 17;
        int reached = w.Air().Aired.Count(Past);
        Assert.InRange(reached, 1, 5 * 10 * 2 * 37);
        // Shut, the right room has no vent: its air stays where it lay, no cell of it newly aired, and fades from the far
        // edge; the left keeps its bubble.
        w.Run(Content.Doors.SwingTicks);
        w.Do(new SetDoor(door, false));
        for (int t = 0; t < reached; t++)
        {
            var before = w.Air().Aired.Where(Past).ToHashSet();
            w.Run();
            Assert.All(w.Air().Aired.Where(Past), c => Assert.Contains(c, before));
        }
        Assert.DoesNotContain(w.Air().Aired, Past);
        Assert.Empty(w.Refusals);
        Assert.Equal("atmosphere.leaky", Of(w, left)!.Problem);
        Assert.True(FreshOrders(w));
    }

    [Fact]
    public void VentsThroughAnInnerDoorOpenedOntoABreachedRoomAWayToSpace()
    {
        var (w, left, door, _) = TwoRooms();
        w.Do(new ClearAt(At15(14, 2), "structure")).Run();
        int aired = AiredIn(w, left).Count;
        Assert.Equal(RoomStatus.Aired, Status(w, left));
        w.Do(new SetDoor(door, true)).Run();
        Assert.Equal(RoomStatus.Venting, Status(w, left));
        Assert.InRange(AiredIn(w, left).Count, 0, aired - Air.VentLoss);
    }

    [Fact]
    public void LosesItsAirFromTheFarEdgeOnceNoLiveVentHoldsItCellsPerFillABeat()
    {
        var (w, left, _, _) = TwoRooms(cut: true);
        int volume = w.M.Rooms.RoomById(left)!.Volume;
        Assert.Equal(volume - Air.CellsPerFill, AiredIn(w, left).Count);
        w.Run((volume - Air.CellsPerFill) / Air.CellsPerFill);
        Assert.Empty(w.Air().Aired);
        Assert.Equal(RoomStatus.Vacuum, Status(w, left));
    }

    [Fact]
    public void LosesItsAirOnceItsChainRunsDryAndHoldsNoneUnpoweredThoughItsTankIsFull()
    {
        var s = SetUp();
        var w = s.W;
        s.Fill(1);
        AirUp(w, s.Room);
        w.Air().Tanks.Clear();
        w.Run(Volume / Air.CellsPerFill);
        Assert.Empty(w.Air().Aired);
        Assert.Equal("atmosphere.dry", Of(w, s.Room)!.Problem);
        // Liquid again, and the panel taken away: its vent is live, but makes nothing to hold a cell with.
        s.Fill(1);
        AirUp(w, s.Room);
        w.Do(new ClearAt(w.M.Grid.MachinesOf("solarPanel")[0].Cell, "machine")).Run(Volume / Air.CellsPerFill);
        Assert.Empty(w.Air().Aired);
        Assert.True(Oxygen(w, s.Tanks[0]) > 0);
        Assert.Equal([At(1, 1)], w.Air().Live);
    }

    /// <summary>
    /// Two rooms deep, a door between, a vent in each, the right one piped to the left's vaporizer once the left room's
    /// bubble stands: the vaporizer's making split over two rooms, the left holds about half as many cells.
    /// </summary>
    [Fact]
    public void SplitsOneVaporizersMakingEvenlyOverTheRoomsItsVentsStandInAndPlaysOnAlikeLoaded()
    {
        string map = string.Join("\n", new[] { "###############", "#V......#....V#", "#TT.....D.....#", "#TT.....#.....#" }
            .Concat(Enumerable.Repeat("#.......#.....#", 37)).Append("###############"));
        var w = new Scenario(AirTesting.OneThick(AirTesting.Chained(map))).Do(new Items.SetCreative(true));
        w.Pipe(At15(1, 1), w.Output(At15(1, 2))).Run();
        int tank = w.M.Grid.MachinesOf("tank").Single().Id;
        for (int i = 0; i < 4; i++) w.Do(new LoadCanister(tank));
        w.Run(800);
        int left = w.M.Rooms.RoomAt(At15(4, 2))!.Value, right = w.M.Rooms.RoomAt(At15(10, 2))!.Value;
        int alone = AiredIn(w, left).Count;
        Assert.Empty(AiredIn(w, right));
        w.Pipe(At15(1, 1), At15(6, 1), At15(11, 1), At15(13, 1));
        for (int i = 0; i < 10; i++)
        {
            var m = new Registry();
            var loaded = World.Load(w.World.Save(), [.. m.All, new Builder(m)]);
            for (int t = 0; t < 40; t++)
            {
                loaded.Step([]);
                w.World.Step([]);
                Assert.Equal(w.World.Hash(), loaded.Hash());
            }
        }
        w.Run(400);
        Assert.InRange(AiredIn(w, left).Count, alone * 4 / 10, alone * 6 / 10);
        Assert.NotEmpty(AiredIn(w, right));
        Assert.True(FreshOrders(w));
    }

    /// <summary>
    /// Whatever doors open and shut, canisters come and pipes are cut, only cells of rooms hold air, every room's air is the
    /// first cells of its order, and air grows only with a live vent (2026-11-05-air-bubbles).
    /// </summary>
    [Property(MaxTest = 30)]
    public Property AirsOnlyCellsOfRoomsAndOnlyThePrefixOfEachRoomsOrder()
    {
        var step = Gen.OneOf(
            Gen.Elements(0, 1, 2).Select(op => (Op: op, Ticks: 0)),
            Gen.Choose(1, 60).Select(t => (Op: 3, Ticks: t)));
        var steps = from n in Gen.Choose(1, 12) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var (w, _, door, _) = TwoRooms();
            int tank = w.M.Grid.MachinesOf("tank").Single().Id, vent = At15(1, 1), output = w.Output(At15(1, 2));
            bool open = false, piped = true;
            foreach (var (op, ticks) in all)
            {
                if (op == 0) w.Do(new SetDoor(door, open = !open));
                if (op == 1) w.Do(new LoadCanister(tank));
                if (op == 2 && piped) w.Do(new CutPipe(vent, output));
                if (op == 2 && !piped) w.Pipe(vent, output);
                if (op == 2) piped = !piped;
                for (int t = 0; t < Math.Max(ticks, 1); t++)
                {
                    int before = w.Air().Aired.Count;
                    bool live = w.Air().Live?.Count > 0;
                    w.Run();
                    int stray = w.Air().Aired.FirstOrDefault(c => w.M.Rooms.RoomAt(c) is not >= 0, -1);
                    if (stray >= 0) return false.Label($"cell {stray}, in no room, aired");
                    if (!live && w.Air().Live?.Count == 0 && w.Air().Aired.Count > before) return false.Label("air grew with no live vent");
                }
                if (!FreshOrders(w)) return false.Label("the orders kept are not those found afresh");
            }
            return true.ToProperty();
        });
    }
}
