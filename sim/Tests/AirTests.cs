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

    /// <summary>Whether the orders kept room by room are those found afresh for the whole ship, as after a load.</summary>
    private static bool FreshOrders(Scenario w)
    {
        // A vent counts only while fed.
        var vents = w.M.Grid.Machines.Where(m => m.Kind == "vent" && w.M.Atmosphere.Fed(m.Id))
            .Select(m => m.Cell).ToList();
        var fresh = Order.Rooms(w.M.Rooms.RoomOfCells, w.M.Grid.Width, w.M.Rooms.All, vents, new bool[w.M.Rooms.RoomOfCells.Length]);
        var kept = w.Air().Orders;
        if (fresh.Count != kept.Count) return false;
        foreach (var (room, order) in fresh)
            if (!kept.TryGetValue(room, out var other) || !other.SequenceEqual(order)) return false;
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
    public void FillsARoomOutwardFromItsVentCellsPerFillABeatAUnitEach()
    {
        var s = SetUp(valve: true);
        var (w, room, _) = s;
        // The vaporizer fills its store behind the closed valve first, so the fill is not held to the unit a tick it makes.
        w.Do(new SetValve(At(1, 2), false));
        s.Fill(1);
        w.Run(Air.GeneratorStore);
        Assert.Equal(Air.GeneratorStore, w.Gas());
        w.Do(new SetValve(At(1, 2), true)).Run();
        int clock = w.Air().Clock;
        int first = w.Air().Aired.Count;
        Assert.Equal(RoomStatus.Filling, Status(w, room));
        w.Run(10 * Air.TicksPerCell);
        // Air.CellsPerFill cells a beat, four of the 8 px cells to the 16 px one a beat aired (0105).
        int filled = first + Air.CellsPerFill * (w.Air().Clock / Air.TicksPerCell - clock / Air.TicksPerCell);
        var cells = w.Air().Aired;
        Assert.Equal(filled, cells.Count);
        Assert.Contains(At(1, 1), cells);
        // Every aired cell is as near the vent as any cell still empty, or nearer.
        var empty = w.Air().Orders[room].Where(c => !cells.Contains(c)).ToList();
        Assert.True(cells.Max(FromVent) <= empty.Min(FromVent));
        // Each cell costs a unit of the chain, and the room uses its leak from the tick after its first cells.
        Assert.Equal(Air.Canister - filled - AirTesting.Leaked(Leak, clock, w.Air().Clock), w.Held());
    }

    [Fact]
    public void IsAiredOnceEveryCellIsPaidForThenUsesOnlyItsLeak()
    {
        var s = SetUp(valve: true);
        var w = s.W;
        // The store filled first behind the closed valve, so it has what the fill and the leak draw while the room fills.
        w.Do(new SetValve(At(1, 2), false));
        s.Fill(1);
        w.Run(Air.GeneratorStore);
        w.Do(new SetValve(At(1, 2), true)).Run();
        int clock = w.Air().Clock;
        AirUp(w, s.Room);
        Assert.Equal(Volume, w.Air().Aired.Count);
        Assert.Equal(w.Air().Aired, w.M.Atmosphere.AiredCells);
        int now = w.Air().Clock;
        // Paid by the chain, the tank's liquid through the vaporizer's store: a cell a unit, and the leak.
        Assert.Equal(Air.Canister - Volume - AirTesting.Leaked(Leak, clock, now), w.Held());
        w.Run(Air.TicksPerCell * 3);
        Assert.Equal(Air.Canister - Volume - AirTesting.Leaked(Leak, clock, w.Air().Clock), w.Held());
    }

    [Fact]
    public void DrawsFromTheTanksOfANetworkOneByOneInIdOrder()
    {
        var w = SetUp().W;
        // A second tank, built after the first, joins the vaporizer's input network: a pipe from its port to the input.
        w.Do(new Place(At(5, 2), new Placement("machine", "tank"))).Run();
        w.Pipe(At(5, 3), At(1, 3)).Run();
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
    public void FillsFromTheFirstVentOfTheRoomWhoseNetworkCanPayNotOneRunDry()
    {
        // Two chains, each vent fed by its own vaporizer, each vaporizer by its own tank under the hull.
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
            ["", "", "", " -    -", " -    -", " -    -", " -    -", " -    -"],
            [((1, 8), (1, 2)), ((5, 8), (6, 2))]));
        var w = s.W;
        int dry = s.Tanks[0], wet = s.Tanks[1];
        w.Pipe(At(7, 1), w.Output(At(6, 2))).Run();
        s.Fill(1, wet);
        w.Run(4 * Air.TicksPerCell);
        int filled = w.Air().Aired.Count;
        Assert.True(filled > 0);
        Assert.True(w.Held() <= Air.Canister - filled);
        Assert.Equal(0, Oxygen(w, dry));
        Assert.Equal(0, Oxygen(w, w.M.Grid.MachineAt(At(1, 2))!.Id));
        Assert.Equal(RoomStatus.Filling, Status(w, s.Room));
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
        // The left wall's one cell thick: the right column of its blocks.
        foreach (int y in new[] { 1, 2, 3 })
            foreach (int cell in new[] { At(0, y) + 1, At(0, y) + 1 + 18 })
                w.Do(new ClearAt(cell, "structure")).Do(new Place(cell, Placement.Wall("iron")));
        s.Fill(2);
        AirUp(w, s.Room);
        int before = w.Held();
        int leak = (Hull - 2 * 4 + Volume) * Scrap + 2 * 3 * Iron + 2 * Air.DoorLeak;
        // Spread over the period, paid by the chain: about half by half way, all of it by its end.
        w.Run(Air.LeakEvery / 2);
        Assert.InRange(before - w.Held(), leak / 2, (leak + 1) / 2);
        w.Run(Air.LeakEvery / 2);
        Assert.Equal(before - leak, w.Held());
        Assert.Equal(Volume, w.Air().Aired.Count);
    }

    [Fact]
    public void LeaksItsOwnAirFromTheFarEdgeWhenNoTankPays()
    {
        // Six rows deep: its leak, about half a unit a tick, within the unit a tick one vaporizer makes, so it airs.
        const int Rows = 6;
        Assert.True((14 + 14 + 4 * Rows + 14 * 2 * Rows) * Scrap < Air.GeneratorRate);
        var s = SetUp(Deep(Rows));
        var w = s.W;
        s.Fill(1);
        AirUp(w, s.Room, 2000);
        DryOut(w, 40000);
        int held = w.Air().Aired.Count;
        w.Run(Minute / 20);
        int left = w.Air().Aired.Count;
        Assert.True(left < held);
        Assert.True(left > 0);
        // The far end of the room went first: what is left lies nearest the vent.
        Assert.Contains(At(1, 1), w.Air().Aired);
        Assert.DoesNotContain(At(7, Rows), w.Air().Aired);
        Assert.Equal(RoomStatus.Vacuum, Status(w, s.Room));
    }

    [Fact]
    public void StallsWhenTheNetworkRunsDryAndGoesOnWhenACanisterComes()
    {
        // A room bigger than a canister leaks more than a vaporizer makes, so it never airs: instead a breach drains the
        // canister, liquid leaving at the vaporizer's unit a tick, until less is left than the room holds; then it is sealed.
        var s = SetUp();
        var w = s.W;
        s.Fill(1);
        w.Do(new ClearAt(At(8, 2), "structure")).Run();
        for (int i = 0; i < Air.Canister && Oxygen(w, s.Tanks[0]) > Volume / 2; i++) w.Run();
        Assert.Equal(Volume / 2, Oxygen(w, s.Tanks[0]));
        w.Do(new Place(At(8, 2), Wall)).Run();
        DryOut(w, 1000);
        Assert.True(w.Air().Aired.Count < Volume);
        Assert.Equal(RoomStatus.Vacuum, Status(w, s.Room));
        w.Run(10 * Air.TicksPerCell);
        int left = w.Air().Aired.Count;
        s.Fill(1);
        Assert.Equal(RoomStatus.Filling, Status(w, s.Room));
        w.Run(10 * Air.TicksPerCell);
        Assert.True(w.Air().Aired.Count > left);
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
    public void KeepsItsAirWhereItIsWhenADoorJoinsItToAnEmptyRoom()
    {
        var (w, left, door, right) = TwoRooms(cut: true);
        var held = w.Air().Aired.ToList();
        int clock = w.Air().Clock;
        w.Do(new SetDoor(door, true)).Run();
        int joined = w.M.Rooms.RoomAt(At15(4, 2))!.Value;
        Assert.Equal(left, joined);
        Assert.Equal(joined, w.M.Rooms.RoomAt(right));
        // Unpiped, nothing fills and nothing moves: only the leak, due more often over 8 px cells (0104), takes some aired.
        int leaked = AirTesting.Leaked(Of(w, joined)!.Leak, clock, w.Air().Clock);
        Assert.Equal(held.Count - leaked, w.Air().Aired.Count);
        Assert.All(w.Air().Aired, c => Assert.Contains(c, held));
        Assert.Equal(RoomStatus.Vacuum, Status(w, joined));
    }

    [Fact]
    public void FillsTheJoinedRoomThroughTheDoorwayTheCellsNearestTheVentFirst()
    {
        var (w, _, door, _) = TwoRooms();
        w.Do(new SetDoor(door, true)).Run(3);
        Assert.Contains(door, w.Air().Aired);
        Assert.Contains(At15(9, 2), w.Air().Aired);
        Assert.DoesNotContain(At15(10, 3), w.Air().Aired);
    }

    [Fact]
    public void LeavesALittleAirWhereItIsWhenTheDoorShutsOnItNotWhereTheRoomWouldPutIt()
    {
        var (w, _, door, right) = TwoRooms();
        w.Do(new SetDoor(door, true)).Run(3);
        // Past the doorway, which fills its block (0104).
        var inRight = w.Air().Aired.Where(c => c % 30 > At15(8, 0) + 1).ToList();
        Assert.NotEmpty(inRight);
        w.Do(new SetDoor(door, false)).Run();
        int shut = w.M.Rooms.RoomAt(right)!.Value;
        Assert.Equal(inRight, AiredIn(w, shut));
        // Its order starts at its middle, yet the air stays where it came in.
        Assert.Equal(At15(11, 2), w.Air().Orders[shut][0]);
        w.Run(10);
        Assert.Subset(inRight.ToHashSet(), AiredIn(w, shut).ToHashSet());
    }

    [Fact]
    public void FillsTheRestOfTheJoinedRoomShutThePartWithNoVentLeaksItsAirAway()
    {
        var (w, left, door, right) = TwoRooms();
        w.Do(new SetDoor(door, true)).Run();
        Assert.Equal(RoomStatus.Filling, Status(w, left));
        AirUp(w, left);
        w.Do(new SetDoor(door, false)).Run();
        int shut = w.M.Rooms.RoomAt(right)!.Value;
        int volume = w.M.Rooms.RoomById(shut)!.Volume;
        Assert.Equal(volume, AiredIn(w, shut).Count);
        Assert.Equal(RoomStatus.Aired, Status(w, shut));
        AirUp(w, left);
        // No tank pays the right room's leak: it takes the room's own air, a cell at a time.
        w.Run(Minute / 8);
        Assert.True(AiredIn(w, shut).Count < volume);
        Assert.True(AiredIn(w, shut).Count > 0);
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
        // Fed once piped to a vaporizer's output, unpowered and dry as it is: what feeds it decides, not what it holds.
        w.Pipe(At(3, 3), w.Output(At(6, 1))).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Atmosphere.Fed(vent));
        Assert.Equal(At(1, 1), w.Air().Orders[room][0]);
        Assert.True(FreshOrders(w));
    }

    private static int Port(Scenario w, int cell) => w.M.Grid.PortsOf(w.M.Grid.MachineAt(cell)!).First();

    [Fact]
    public void LaysARoomOutAgainWhenItsVentsOnlyChangeOrderAVentRebuiltWhereItStood()
    {
        var w = new Scenario("#########\n#V.GG..V#\n#..GG...#\n#########");
        // Both vents fed by the vaporizer between them, through its output.
        w.GiveItems(0, ("tube", 6)).Pipe(Port(w, At(1, 1)), w.Output(At(3, 1)), Port(w, At(7, 1))).Run();
        Assert.Empty(w.Refusals);
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
        Assert.Subset(before.ToHashSet(), after.ToHashSet());
        Assert.True(after.Zip(after.Skip(1)).All(p => p.First < p.Second));
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
        for (int cell = 0; cell < 18 * 10; cell++)
        {
            if (w.M.Rooms.RoomAt(cell) != s.Room) continue;
            w.Do(new ClearAt(cell, "back")).Do(new Place(cell, Placement.BackWall("iron")));
        }
        w.Run();
        Assert.Equal(Hull * Scrap + Volume * Iron, LeakAt(w, At(4, 2)));
    }

    [Fact]
    public void LeaksNothingThroughABackWallBehindAWallNoAirTouchesIt()
    {
        var w = SetUp().W;
        w.Do(new Place(At(5, 2), Wall)).Run();
        Assert.Equal((Hull + Volume - 1) * Scrap, LeakAt(w, At(4, 2)));
    }

    [Fact]
    public void LeaksThroughTheBackWallBehindAnOpenDoorACellOfTheRoomNotAClosedOne()
    {
        var w = new Scenario(AirTesting.OneThick(Two));
        // Shut, each room its hull and its own cells; the door's back walls touch no air.
        Assert.Equal(new int?[] { (2 * 17 + 4 * 21) * Scrap, (2 * 13 + 4 * 15) * Scrap }, new[] { LeakAt(w, At15(4, 2)), LeakAt(w, At15(10, 2)) });
        w.Do(new SetDoor(At15(8, 2), true)).Run();
        // Open, one room: both hulls, both rooms' cells and the doorway's four (0104).
        Assert.Equal((2 * (17 + 13) + 4 * (21 + 15) + 4) * Scrap, LeakAt(w, At15(4, 2)));
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
    public void TellsAFillingRoomHowFarItIsAndNothingOfHowLongItLasts()
    {
        var s = SetUp();
        s.Fill(1);
        var air = Of(s.W, s.Room)!;
        Assert.Equal((RoomStatus.Filling, (int?)null, (string?)null), (air.Status, air.Lasts, air.Problem));
        Assert.True(air.Aired < Volume);
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
}
