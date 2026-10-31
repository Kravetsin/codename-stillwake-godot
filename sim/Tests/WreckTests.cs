using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Stillwake.Sim.Power;
using Stillwake.Sim.Research;
using Stillwake.Sim.Rooms;
using Xunit;
using CrewSpec = Stillwake.Content.Crew;
using HeroSpec = Stillwake.Content.Hero;
using ItemSpecs = Stillwake.Content.Items;
using Machines = Stillwake.Content.Machines;
using Move = Stillwake.Sim.Items.Move;
using ResearchSpec = Stillwake.Content.Research;
using Slot = Stillwake.Sim.Items.Place;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/wreck.test.ts, ported: the world every new game starts in.</summary>
public class WreckTests
{
    /// <summary>The new game's wreck, calm: its rooms' air is the subject here, and a meteor's blast would open them (meteor-blast).</summary>
    private static readonly Layout Wreck = Registry.Wreck with { Meteors = false };
    /// <summary>Where the wreck's drawing lies in the world: its top-left cell, centred.</summary>
    private static readonly (int X, int Y) Drawn = Layout.OffsetInWorld(Registry.WreckMap.Width, Registry.WreckMap.Height);
    /// <summary>The world's cell at (<paramref name="x"/>, <paramref name="y"/>) of the sketch the ship is drawn from, its pod's room's left wall at 41.</summary>
    private static int At(int x, int y) => (y + 25 + Drawn.Y) * Wreck.Width + x + 50 + Drawn.X;
    private static IEnumerable<int> Cells(int left, int right, int top, int bottom) =>
        from y in Enumerable.Range(top, bottom - top + 1)
        from x in Enumerable.Range(left, right - left + 1)
        select At(x, y);
    /// <summary>The iron room's cells, where the hero wakes.</summary>
    private static readonly int[] A = [.. Cells(42, 57, 13, 20).Order()];
    /// <summary>The right lock's cells, between its doors; the hold's, open to space.</summary>
    private static readonly int[] Lock = [.. Cells(60, 63, 13, 20)];
    private static readonly int[] Hold = [.. Cells(24, 33, 14, 30)];
    private static readonly int LeftOuter = At(34, 15), LeftInner = At(40, 15), Inner = At(58, 15), Outer = At(64, 15);
    /// <summary>The lifepod's top-left cell, in A against the right lock's inner door, and its hatch.</summary>
    private static readonly int Pod = At(52, 15), Hatch = Pod + Content.Crew.PodHatch.Y * Wreck.Width + Content.Crew.PodHatch.X;
    /// <summary>A cell of open space, off the hull.</summary>
    private static readonly int Space = At(0, 0);

    private static int AiredIn(Scenario w, IEnumerable<int> cells) => cells.Count(c => w.M.Atmosphere.AiredAt(c));

    /// <summary>The wreck with room A aired, as it is a few seconds after the start.</summary>
    private static Scenario AiredWreck()
    {
        var w = new Scenario(Wreck);
        for (int i = 0; i < 600 && AiredIn(w, A) < A.Length; i++) w.Run();
        Assert.Equal(A.Length, AiredIn(w, A));
        return w;
    }

    [Fact]
    public void BuildsTheSameWorldForTheSameSeed() =>
        Assert.Equal(World.Create(42, Wreck, new Registry().All).Hash(), World.Create(42, Wreck, new Registry().All).Hash());

    [Fact]
    public void LiesInTheMiddleOfTheWorldWithinItsBuildZoneRoomToBuildAllAroundIt()
    {
        var m = new Registry();
        World.Create(1, Wreck, m.All);
        Assert.Equal((Content.World.Width, Content.World.Height), (m.Grid.Width, m.Grid.Height));
        Assert.Equal(new Box(512, 256, 1535, 767), m.Grid.Zone);
        var hull = Wreck.Rows.SelectMany((row, y) => row.Select((c, x) => (c, x, y)).Where(h => h.c != ' ')).ToList();
        var built = m.Grid.Built;
        Assert.All(hull, h => Assert.True(built.Contains(h.x, h.y)));
        // The whole wreck inside the zone, a margin of the zone's space round it on every side.
        int margin = new[] { built.Left - m.Grid.Zone.Left, built.Top - m.Grid.Zone.Top, m.Grid.Zone.Right - built.Right, m.Grid.Zone.Bottom - built.Bottom }.Min();
        Assert.True(margin >= 160, $"{margin} cells from the hull to the zone's edge");
        // A hero on a tether from the hull, half a cell in, stays clear of the grid's edge, all of its box.
        Assert.True((margin + 256) * 1000 - 500 > HeroSpec.Tether + HeroSpec.Height / 2);
    }

    [Fact]
    public void PutsThePodAtTheZonesCornerPlusTheCentringPlusItsPlaceInTheDrawing()
    {
        var m = new Registry();
        World.Create(1, Wreck, m.All);
        var zone = m.Grid.Zone;
        int pod = m.Grid.MachinesOf("lifepod").Single().Cell;
        // The drawing's box of 384×192, centred in the zone's 1024×512: (320, 160) in, then the pod's own (102, 40).
        Assert.Equal((320 + 102, 160 + 40), (pod % m.Grid.Width - zone.Left, pod / m.Grid.Width - zone.Top));
        Assert.Equal(Pod, pod);
    }

    [Fact]
    public void HoldsTheLifepodOnItsPipeAndThePilesInTheHoldAndNothingElse()
    {
        var m = new Registry();
        World.Create(1, Wreck, m.All);
        Assert.Equal(["emergencyLamp", "emergencyLamp", "lifepod", "pileBits", "pileCargo", "pileHull", "pileScrap"], m.Grid.Machines.Select(x => x.Kind).Order(StringComparer.Ordinal));
        Assert.Equal([Pod], m.Grid.Machines.Where(x => Machines.Get(x.Kind).Fixed).Select(x => x.Cell));
        Assert.Equal(ResearchSpec.StartKnown.Order(StringComparer.Ordinal), m.Research.Recipes().Where(r => r.Known).Select(r => r.Id).Order(StringComparer.Ordinal));
        Assert.Equal(0, m.Research.Credits);
        // Four doors, each two cells by six.
        Assert.Equal(Enumerable.Repeat(Structure.DoorClosed, 4 * 12), Wreck.CellsOf('D').Select(m.Grid.StructureAt));
        Assert.Empty(m.Power.Networks);
        Assert.Single(m.Pipes.Spans);
        Assert.Equal(Hatch, m.Crew.Spawn);
        Assert.Contains(m.Crew.Spawn, A);
        Assert.All(m.Grid.Footprint("lifepod", Pod)!, cell => Assert.Contains(cell, A));
    }

    [Fact]
    public void WallsItsRoomAndLocksInIronAndTheRestInScrap()
    {
        var m = new Registry();
        World.Create(1, Wreck, m.All);
        // The roof and floor of both locks and the room, and the walls that part them.
        var round = Cells(36, 63, 12, 12).Concat(Cells(36, 63, 21, 21)).Concat(Cells(35, 35, 12, 14)).Concat(Cells(40, 41, 13, 14)).Concat(Cells(58, 59, 13, 14)).Concat(Cells(65, 65, 13, 14));
        Assert.All(round, cell => Assert.Equal("iron", m.Grid.WallAt(cell)?.Id));
        var walls = Enumerable.Range(0, Wreck.Width * Wreck.Height).Where(c => m.Grid.WallAt(c) != null).ToList();
        var iron = walls.Where(c => m.Grid.WallAt(c)!.Id == "iron").ToHashSet();
        Assert.Equal(76, iron.Count);
        // Everything else scrap, its dressed plates included, and behind every wall a scrap back wall: an iron wall pays one plate.
        Assert.All(walls.Where(c => !iron.Contains(c)), c => Assert.Contains(m.Grid.WallAt(c)!.Id, (string[])["scrap", "wreckScrap"]));
        Assert.All(walls, c => Assert.Equal("scrap", m.Grid.BackAt(c)?.Id));
    }

    [Fact]
    public void IsOnePieceOfWallsAndDoors()
    {
        var rows = Registry.WreckMap.Rows;
        bool Solid(int x, int y) => y >= 0 && y < rows.Count && x >= 0 && x < rows[y].Length && rows[y][x] is '#' or 'D';
        var all = (from y in Enumerable.Range(0, rows.Count) from x in Enumerable.Range(0, rows[y].Length) where Solid(x, y) select (x, y)).ToHashSet();
        var seen = new HashSet<(int x, int y)> { all.First() };
        var open = new Stack<(int x, int y)>(seen);
        while (open.TryPop(out var c))
            foreach (var n in new[] { (c.x + 1, c.y), (c.x - 1, c.y), (c.x, c.y + 1), (c.x, c.y - 1) })
                if (all.Contains(n) && seen.Add(n)) open.Push(n);
        Assert.Equal(all.Count, seen.Count);
    }

    [Fact]
    public void LeavesTheHoldAndTheLandingAfloatAndSpaceOutsideTheRightLock()
    {
        var m = new Registry();
        World.Create(1, Wreck, m.All);
        // A one-cell slit in the hull would make the hold a room.
        Assert.All(Hold.Where(c => m.Rooms.RoomAt(c) != null), c => Assert.True(m.Rooms.Afloat(c)));
        Assert.All(Cells(66, 71, 15, 20), c => Assert.True(m.Rooms.Afloat(c)));
        Assert.Equal(RoomsModule.Space, m.Rooms.RoomAt(At(66, 18)));
    }

    [Fact]
    public void MakesHeroesAtThePodInAWhereTheyBreatheWithBothDoorsOfALockOpen()
    {
        var w = new Scenario(Wreck);
        int pod = w.M.Grid.MachineAt(Pod)!.Id;
        w.Do(new SetDoor(Inner, true)).Do(new SetDoor(Outer, true)).Do(new Enter(w.M.Crew.Spawn) { From = 1 }).Run(10);
        Assert.Equal(0, AiredIn(w, A));
        w.Run(30 * CrewSpec.BreathEvery);
        Assert.Equal(Scenario.Full, w.M.Crew.HeroOf(1)?.Air);
        // Its Fabricator works with no power and no air.
        string screwdriver = Content.Screwdrivers.Basic.Item;
        w.Do(new Sim.Crafting.Craft(pod, screwdriver, 1) { From = 1 }).Run(TicksAt("lifepod", screwdriver));
        Assert.Equal(new Stack(screwdriver, 1), w.M.Items.Crate(pod)[0]);
    }

    /// <summary>Ticks one unit of <paramref name="craft"/> takes at <paramref name="station"/>.</summary>
    private static int TicksAt(string station, string craft) => Content.Crafts.Get(craft).Ticks * 1000 / Content.Stations.Get(station).Speed;

    /// <summary>Crafts one of <paramref name="craft"/> at workbench <paramref name="bench"/> for peer 0 and takes it from its outputs.</summary>
    private static void CraftAtBench(Scenario w, int bench, string craft)
    {
        w.Do(new Sim.Crafting.Craft(bench, craft, 1)).Run(TicksAt("workbench", craft));
        w.Do(new Move(new Slot(0, bench), new Target(), 99)).Run();
    }

    /// <summary>A <paramref name="kind"/> built at <paramref name="cell"/> by peer 0 of the salvage it costs, given with <paramref name="more"/>; its machine id.</summary>
    private static int Build(Scenario w, string kind, int cell, params (string Item, int Count)[] more)
    {
        w.GiveItems(0, [.. Machines.Get(kind).Cost, .. more]);
        w.M.Knowledge.Grant(kind);
        w.Do(new PlacePlan(cell, new Placement("machine", kind))).Run();
        int plan = w.M.Building.Plans.FirstOrDefault()?.Id ?? 0;
        Machine? Built() => w.M.Grid.Machines.FirstOrDefault(m => m.Kind == kind);
        // Its salvage delivered on the first press, then built on the presses after.
        for (int i = 0; i < 2 * Machines.Get(kind).BuildTicks && Built() == null; i++) w.Do(new Work(plan)).Run();
        int id = Built()?.Id ?? 0;
        Assert.True(id > 0);
        return id;
    }

    /// <summary>A furnace built in A by peer 0 of the salvage it costs, given with <paramref name="more"/>; its machine id.</summary>
    private static int BuildFurnace(Scenario w, params (string Item, int Count)[] more) => Build(w, "furnace", At(44, 17), more);

    private static int SlotOf(Scenario w, string item) => w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);

    [Fact]
    public void BuildsAFurnaceInAOfCaughtSalvageBurnsAPlankInItAndSmeltsScrap()
    {
        var w = AiredWreck();
        int id = BuildFurnace(w, ("scrapChunk", 1), ("planks", 1));
        w.Do(new Move(new Slot(SlotOf(w, "planks")), new Target(Machines.Get("furnace").FuelSlot, id), 1)).Run();
        w.Do(new Move(new Slot(SlotOf(w, "scrapChunk")), new Target(Crate: id), 1)).Run();
        // Lit in A's air on the tick of the move, it smelts from the next (0057).
        var chunk = ItemSpecs.Get("scrapChunk");
        w.Run(chunk.SmeltTicks!.Value - 1);
        Assert.Null(w.M.Items.Crate(id)[2]);
        w.Run();
        Assert.Equal(new Stack("scrap", chunk.Yields!.Single(y => y.Item == "scrap").Count), w.M.Items.Crate(id)[2]);
    }

    [Fact]
    public void SmeltsScrapCraftsAWireAndAPlateAtTheWorkbench()
    {
        var w = AiredWreck();
        w.M.Knowledge.Grant("wire");
        w.M.Knowledge.Grant("plate");
        int furnace = BuildFurnace(w, ("scrapChunk", 1), ("hullPlate", 1), ("planks", 2));
        w.Do(new Move(new Slot(SlotOf(w, "planks")), new Target(Machines.Get("furnace").FuelSlot, furnace), 2)).Run();
        w.Do(new Move(new Slot(SlotOf(w, "scrapChunk")), new Target(Crate: furnace), 1)).Run();
        w.Run(ItemSpecs.Get("scrapChunk").SmeltTicks!.Value);
        w.Do(new Move(new Slot(SlotOf(w, "hullPlate")), new Target(Crate: furnace), 1)).Run();
        w.Run(ItemSpecs.Get("hullPlate").SmeltTicks!.Value);
        for (int slot = 2; slot < 5; slot++)
            if (w.M.Items.Crate(furnace)[slot] != null) w.Do(new Move(new Slot(slot, furnace), new Target(), 999));
        w.Run();
        Assert.True(w.M.Items.Totals(0).Single(t => t.Item == "iron").Count >= 2);

        // A workbench of salvage; wire and a plate, crafts the wreck grants, of what was smelted.
        int bench = Build(w, "workbench", At(48, 19));
        CraftAtBench(w, bench, "wire");
        CraftAtBench(w, bench, "plate");
        Assert.Empty(w.Refusals);
        Assert.Equal(1, w.M.Items.Totals(0).Single(t => t.Item == "plate").Count);
    }

    /// <summary>The first minutes (2026-10-30-tablet-tree): a chunk sold at a trade station built in the room, the first block bought, its machine planned.</summary>
    [Fact]
    public void SellsAChunkAtATradeStationBuiltOfSalvageBuysTheFirstBlockAndPlansIt()
    {
        var w = AiredWreck();
        int trade = Build(w, "tradeStation", At(44, 17));
        int chunk = Stillwake.Content.Market.PriceOf("scrapChunk")!.Value, price = Prices.Of(ResearchSpec.Node("furnace")!), chunks = price / chunk + 1;
        w.GiveItems(0, ("scrapChunk", chunks));
        w.Do(new Move(new Slot(SlotOf(w, "scrapChunk")), new Target(Crate: trade), chunks)).Run();
        w.Do(new Stillwake.Sim.Trade.SendCapsule(trade)).Run(Stillwake.Content.Market.CapsuleFlight);
        Assert.Equal(chunks * chunk, w.M.Research.Credits);
        w.Do(new BuyBlock("furnace")).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(chunks * chunk - price, w.M.Research.Credits);
        Assert.True(w.M.Research.Knows("furnace"));
        var furnace = new Placement("machine", "furnace");
        int cell = Enumerable.Range(0, w.M.Grid.Width * w.M.Grid.Height).First(c => w.M.Building.Problem(c, furnace) == null);
        w.Do(new PlacePlan(cell, furnace)).Run();
        Assert.Empty(w.Refusals);
        Assert.Single(w.M.Building.Plans);
    }

    [Fact]
    public void FindsTheLeftLockTheRoomAndTheRightLockTheHoldIsSpace()
    {
        var m = new Registry();
        World.Create(1, Wreck, m.All);
        Assert.Equal([At(36, 13), At(42, 13), At(60, 13)], m.Rooms.All.Select(r => r.Id));
        Assert.All(m.Rooms.All, r => Assert.Empty(r.Openings));
        Assert.Equal(RoomsModule.Space, m.Rooms.RoomAt(At(30, 18)));
    }

    [Fact]
    public void AirsRoomAFromThePodWithinFifteenSecondsAndNothingElse()
    {
        var w = new Scenario(Wreck).Run(15 * 20);
        Assert.Equal(A.Length, AiredIn(w, A));
        Assert.Equal(A.Length, w.M.Atmosphere.AiredCells.Count);
    }

    [Fact]
    public void KeepsAWholeWithTwoHeroesBreathingInItOneOfThemRefillingAnEmptySuit()
    {
        var w = AiredWreck();
        w.Do(new Enter(At(46, 19)) { From = 1 });
        w.Do(new Enter(Space) { From = 2 }).Run();
        w.Run((Scenario.Full - 3) * CrewSpec.BreathEvery);
        Assert.True(w.M.Crew.HeroOf(2)?.Air <= 3);
        w.Do(new Enter(At(50, 19)) { From = 2 }).Run();
        w.Run(5 * 60 * 20);
        Assert.Equal(A.Length, AiredIn(w, A));
        Assert.Equal((Scenario.Full, Scenario.Full), (w.M.Crew.HeroOf(1)?.Air, w.M.Crew.HeroOf(2)?.Air));
    }

    [Fact]
    public void KeepsAWholeWithFourHeroesBreathingInItForAMinuteForABreathIsACellOfItsAir()
    {
        // A breath is a cell of air and the cell a quarter of what it was (0104): the pod, at a generator's rate, outruns
        // four heroes now, an accepted change to retune after a playtest.
        var w = AiredWreck();
        // Out of the pod's reach, whose air would pay their breaths.
        foreach (int peer in new[] { 1, 2, 3, 4 }) w.Do(new Enter(At(43 + peer, 19)) { From = peer });
        w.Run(60 * 20);
        Assert.Equal(A.Length, AiredIn(w, A));
    }

    [Fact]
    public void LosesAsAirWithinAFewTicksOnceBothDoorsOfALockOpen()
    {
        var w = AiredWreck();
        w.Do(new SetDoor(LeftInner, true)).Do(new SetDoor(LeftOuter, true)).Run(7);
        Assert.Equal(0, AiredIn(w, A));
    }

    [Fact]
    public void CyclesTheAirlockWithoutALosingACellOfItsAir()
    {
        var w = AiredWreck();
        var closet = Lock;
        // In: the inner doorway opened, the pod airs the closet too.
        w.Do(new SetDoor(Inner, true)).Run();
        for (int i = 0; i < 600 && AiredIn(w, closet) < closet.Length; i++) w.Run();
        Assert.Equal(closet.Length, AiredIn(w, closet));
        Assert.Equal(A.Length, AiredIn(w, A));
        // Out: the inner closed behind, the outer opened; the closet vents, A keeps its air.
        w.Do(new SetDoor(Inner, false)).Run();
        w.Do(new SetDoor(Outer, true)).Run(20);
        Assert.Equal(0, AiredIn(w, closet));
        Assert.Equal(A.Length, AiredIn(w, A));
        // With the outer doorway open and the inner closed, A stays whole.
        w.Run(60 * 20);
        Assert.Equal(A.Length, AiredIn(w, A));
    }

    /// <summary>The reward for sealing it (the-scoop): gravity, air, floor, for a wall across the throat.</summary>
    [Fact]
    public void MakesTheHoldARoomOnceItsThroatIsWalled()
    {
        var w = new Scenario(Wreck);
        // The pile standing in the throat comes out first: it holds its cells' build slot.
        w.Do(new Place(At(24, 30), Placement.Wall("scrap"))).Run();
        Assert.Equal("grid.machineInTheWay", Assert.Single(w.Refusals).Reason);
        PilesTests.Break(w, "pileScrap");
        foreach (int cell in Cells(24, 24, 14, 30)) w.Do(new Place(cell, Placement.Wall("scrap")));
        w.Run();
        var room = w.M.Rooms.RoomAt(At(30, 18));
        Assert.NotEqual(RoomsModule.Space, room);
        Assert.Empty(w.M.Rooms.All.First(r => r.Id == room).Openings);
        Assert.False(w.M.Rooms.Afloat(At(30, 18)));
        // Its floor is the deck a hero now stands on, and the pod airs it once both doors of the left lock stand open.
        Assert.Equal(Structure.Floor, w.M.Grid.StructureAt(At(30, 21)));
        w.Do(new SetDoor(LeftInner, true)).Do(new SetDoor(LeftOuter, true)).Run(1200);
        Assert.True(w.M.Atmosphere.AiredAt(At(30, 18)));
    }

    /// <summary>The pod feeds a first vent elsewhere (the-scoop): a branch laid from its pipe's free end, its own pipe still uncut.</summary>
    [Fact]
    public void FeedsAVentInTheLeftLockFromABranchLaidOffThePodsPipe()
    {
        var w = new Scenario(Wreck);
        Build(w, "vent", At(37, 19));
        int free = At(53, 20), bend = At(53, 19), mid = At(45, 19), vent = At(37, 19);
        w.GiveItems(0, ("tube", 40));
        w.Do(new LayPipe(free, bend)).Do(new LayPipe(bend, mid)).Do(new LayPipe(mid, vent)).Run();
        Assert.Empty(w.Refusals);
        w.Run(1200);
        Assert.All(Cells(36, 39, 13, 20), c => Assert.True(w.M.Atmosphere.AiredAt(c)));
        w.Do(new CutPipe(At(52, 20), free)).Run();
        Assert.Equal("pipes.fixed", Assert.Single(w.Refusals).Reason);
    }

    /// <summary>What closes the dead ends: a lit fire and a breathing hero together take less than the pod makes.</summary>
    [Fact]
    public void HoldsItsAirWithAHeroBreathingAndAFurnaceLit()
    {
        var w = AiredWreck();
        int id = BuildFurnace(w, ("planks", 5), ("scrapChunk", 5));
        w.Do(new Move(new Slot(SlotOf(w, "planks")), new Target(Machines.Get("furnace").FuelSlot, id), 5)).Run();
        w.Do(new Move(new Slot(SlotOf(w, "scrapChunk")), new Target(Crate: id), 5)).Run();
        // Out of the pod's reach, whose air would pay its breaths.
        w.Do(new Enter(At(44, 19)) { From = 1 });
        w.Run(12 * 20);
        Assert.True(w.M.Furnace.At(id).Burning);
        Assert.Equal(A.Length, AiredIn(w, A));
        Assert.Equal(Scenario.Full, w.M.Crew.HeroOf(1)?.Air);
    }

    [Fact]
    public void OpensTheLampWithTheBurnerForNothingBeforeItMakesPower() =>
        Assert.Equal(ResearchSpec.Node("burnerGenerator")!.Ring, ResearchSpec.Node("lamp")!.Ring);

    /// <summary>Each lights the hold, on the wall its bracket reaches: a lamp in mid-air reads as a mistake.</summary>
    [Fact]
    public void HangsAnEmergencyLampInEachPartOfTheHoldOnItsRightWall()
    {
        var w = new Scenario(Wreck);
        var lamps = w.M.Grid.MachinesOf("emergencyLamp");
        Assert.Equal(2, lamps.Count);
        var inHold = Cells(24, 33, 11, 30).ToHashSet();
        foreach (var lamp in lamps)
        {
            Assert.Contains(lamp.Cell, inHold);
            Assert.NotNull(w.M.Grid.WallAt(lamp.Cell + Machines.Get(lamp.Kind).Width));
        }
    }

    [Fact]
    public void WillNotLightAFurnaceInTheHold()
    {
        var w = new Scenario(Wreck);
        PilesTests.Break(w, "pileCargo");
        PilesTests.Break(w, "pileBits");
        int id = Build(w, "furnace", At(29, 17), ("planks", 1), ("scrapChunk", 1));
        w.Do(new Move(new Slot(SlotOf(w, "planks")), new Target(Machines.Get("furnace").FuelSlot, id), 1)).Run();
        w.Do(new Move(new Slot(SlotOf(w, "scrapChunk")), new Target(Crate: id), 1)).Run();
        w.Run(Content.Furnace.BreathEvery);
        Assert.Equal("furnace.noAir", w.M.Furnace.At(id).Problem);
    }
}
