using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
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
using WreckRows = Stillwake.Content.Wreck;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/wreck.test.ts, ported: the world every new game starts in.</summary>
public class WreckTests
{
    /// <summary>The new game's wreck, calm: its rooms' air is the subject here, and a meteor's blast would open them (meteor-blast).</summary>
    private static readonly Layout Wreck = Registry.Wreck with { Meteors = false };
    /// <summary>Where the wreck's drawing lies in the world: its top-left cell, centred.</summary>
    private static readonly (int X, int Y) Drawn = Layout.OffsetInWorld(Registry.WreckMap.Width, Registry.WreckMap.Height);
    /// <summary>The world's cell at (<paramref name="x"/>, <paramref name="y"/>) of the wreck as content draws it: its block's top-left, doubled (0104).</summary>
    private static int At(int x, int y) => (2 * y + Drawn.Y) * Wreck.Width + 2 * x + Drawn.X;
    /// <summary>The cells of the drawing's characters from (<paramref name="left"/>, <paramref name="top"/>) to (<paramref name="right"/>, <paramref name="bottom"/>), doubled.</summary>
    private static IEnumerable<int> Blocks(int left, int right, int top, int bottom) =>
        from y in Enumerable.Range(2 * top, 2 * (bottom - top + 1))
        from x in Enumerable.Range(2 * left, 2 * (right - left + 1))
        select (y + Drawn.Y) * Wreck.Width + x + Drawn.X;
    /// <summary>Room A's cells: the generator's room, where the hero wakes; its doorways fill their blocks (0104).</summary>
    private static readonly int[] A = [.. Blocks(46, 53, 19, 22).Order()];
    private static readonly int DoorToB = At(45, 21);
    private static readonly int Inner = At(54, 21);
    private static readonly int Outer = At(57, 21);
    /// <summary>Room B's tech station, and the cells of B's hull, floor and back wall torn away: drawn as space.</summary>
    private static readonly int TechStation = At(41, 21);
    private const int BLeft = 30, BRight = 44, BTop = 18, BBottom = 23;
    private static bool Torn(int x, int y) => x < WreckRows.Rows[y].Length && WreckRows.Rows[y][x] == ' ';
    private static readonly int Panel = At(50, 17);
    /// <summary>The lifepod's top-left cell, in A against the airlock's inner doorway, and its hatch.</summary>
    private static readonly int Pod = At(51, 20), Hatch = Pod + Content.Crew.PodHatch.Y * Wreck.Width + Content.Crew.PodHatch.X;
    /// <summary>A cell of open space, far from the hull.</summary>
    private static readonly int Space = At(10, 10);

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
        // The drawing's 192×96, doubled to 384×192, centred in the zone's 1024×512: (320, 160) in, then the pod's own (102, 40).
        Assert.Equal((320 + 102, 160 + 40), (pod % m.Grid.Width - zone.Left, pod / m.Grid.Width - zone.Top));
        Assert.Equal(Pod, pod);
    }

    [Fact]
    public void HoldsAPanelCabledToALampAndAGeneratorAVentATechStationATankOfACanisterTheLifepodNoFurnaceOrCrate()
    {
        var m = new Registry();
        World.Create(1, Wreck, m.All);
        // The tank right after the tech station in reading order (liquid-oxygen), in B at (43, 21), holding a canister's liquid.
        Assert.Equal(["solarPanel", "lamp", "lifepod", "techStation", "tank", "generator", "vent"], m.Grid.Machines.Select(x => x.Kind));
        var tank = m.Grid.Machines[4];
        Assert.Equal((At(43, 21), Content.Air.Canister), (tank.Cell, m.Atmosphere.OxygenIn(tank.Id)));
        Assert.Equal(ResearchSpec.StartKnown.Order(StringComparer.Ordinal), m.Research.Recipes().Where(r => r.Known).Select(r => r.Id).Order(StringComparer.Ordinal));
        Assert.Equal(0, m.Research.Credits);
        var doors = Wreck.CellsOf('D').Select(m.Grid.StructureAt).ToList();
        Assert.Equal(Enumerable.Repeat(Structure.DoorClosed, 36), doors);
        var cells = m.Grid.Machines.Select(x => x.Cell).ToArray();
        var (panel, lamp, generator) = (cells[0], cells[1], cells[5]);
        // The generator and the lamp draw what the panel makes: in full.
        var network = Assert.Single(m.Power.Networks);
        Assert.Equal((panel, 5, 5, 1000), (network.Id, network.Supply, network.Demand, network.Satisfaction));
        Assert.Equal([panel, lamp, generator], network.Nodes);
        Assert.Equal(Pod, cells[2]);
        Assert.Equal(Hatch, m.Crew.Spawn);
        Assert.Contains(m.Crew.Spawn, A);
        Assert.All(m.Grid.Footprint("lifepod", Pod)!, cell => Assert.Contains(cell, A));
    }

    [Fact]
    public void MakesHeroesAtThePodInAWhereTheyBreatheWithAOpenToSpace()
    {
        var w = new Scenario(Wreck);
        int pod = w.M.Grid.MachineAt(Pod)!.Id;
        w.Do(new SetDoor(DoorToB, true)).Do(new Enter(w.M.Crew.Spawn) { From = 1 }).Run(10);
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
    private static int BuildFurnace(Scenario w, params (string Item, int Count)[] more) => Build(w, "furnace", At(49, 21), more);

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

        // A workbench of salvage, on the generator; wire and a plate, crafts the wreck grants, of what was smelted.
        int bench = Build(w, "workbench", At(46, 20));
        CraftAtBench(w, bench, "wire");
        CraftAtBench(w, bench, "plate");
        Assert.Empty(w.Refusals);
        Assert.Equal(1, w.M.Items.Totals(0).Single(t => t.Item == "plate").Count);
    }

    /// <summary>The first minutes (2026-10-30-trade-and-tech): a trade station of raw salvage, a chunk sold, the first block bought at the wreck's tech station, its floor planned.</summary>
    [Fact]
    public void SellsAChunkAtATradeStationOfSalvageBuysTheFirstBlockAndPlansIt()
    {
        var w = AiredWreck();
        w.GiveItems(0, Machines.Get("tradeStation").Cost.ToArray());
        int trade = Build(w, "tradeStation", At(49, 21));
        int chunk = Stillwake.Content.Market.PriceOf("scrapChunk")!.Value, price = Prices.Of(ResearchSpec.Node("furnace")!), chunks = price / chunk + 1;
        w.GiveItems(0, ("scrapChunk", chunks));
        w.Do(new Move(new Slot(SlotOf(w, "scrapChunk")), new Target(Crate: trade), chunks)).Run();
        w.Run(chunks * Stillwake.Content.Market.SellEvery);
        Assert.Equal(chunks * chunk, w.M.Research.Credits);
        int tech = w.M.Grid.MachineAt(TechStation)?.Id ?? 0;
        w.Do(new BuyBlock(tech, "furnace")).Run();
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
    public void FindsRoomAAndTheAirlockBTornOpenIsSpaceItsTechStationInIt()
    {
        var m = new Registry();
        World.Create(1, Wreck, m.All);
        Assert.Equal([At(46, 19), At(55, 20)], m.Rooms.All.Select(r => r.Id));
        Assert.All(m.Rooms.All, r => Assert.Empty(r.Openings));
        Assert.Equal(RoomsModule.Space, m.Rooms.RoomAt(TechStation));
        Assert.Equal("techStation", m.Grid.MachineAt(TechStation)?.Kind);
    }

    [Fact]
    public void AirsRoomAFromTheGeneratorWithinFifteenSecondsAndNothingElse()
    {
        var w = new Scenario(Wreck).Run(15 * 20);
        Assert.Equal(A.Length, AiredIn(w, A));
        Assert.Equal(A.Length, w.M.Atmosphere.AiredCells.Count);
    }

    [Fact]
    public void KeepsAWholeWithTwoHeroesBreathingInItOneOfThemRefillingAnEmptySuit()
    {
        var w = AiredWreck();
        w.Do(new Enter(At(49, 21)) { From = 1 });
        w.Do(new Enter(Space) { From = 2 }).Run();
        w.Run((Scenario.Full - 3) * CrewSpec.BreathEvery);
        Assert.True(w.M.Crew.HeroOf(2)?.Air <= 3);
        w.Do(new Enter(At(51, 21)) { From = 2 }).Run();
        w.Run(5 * 60 * 20);
        Assert.Equal(A.Length, AiredIn(w, A));
        Assert.Equal((Scenario.Full, Scenario.Full), (w.M.Crew.HeroOf(1)?.Air, w.M.Crew.HeroOf(2)?.Air));
    }

    [Fact]
    public void KeepsAWholeWithFourHeroesBreathingInItForAMinuteForABreathIsACellOfItsAir()
    {
        // A breath is a cell of air and the cell a quarter of what it was (0104): the generator, making four times what it
        // did, outruns four heroes now, an accepted change to retune after a playtest.
        var w = AiredWreck();
        // Out of the pod's reach, whose air would pay their breaths.
        foreach (int peer in new[] { 1, 2, 3, 4 }) w.Do(new Enter(At(45 + peer, 21)) { From = peer });
        w.Run(60 * 20);
        Assert.Equal(A.Length, AiredIn(w, A));
    }

    [Fact]
    public void LosesAsAirWithinAFewTicksOnceTheDoorToTheBreachedRoomBOpens()
    {
        var w = AiredWreck();
        w.Do(new SetDoor(DoorToB, true)).Run(7);
        Assert.Equal(0, AiredIn(w, A));
    }

    [Fact]
    public void CyclesTheAirlockWithoutALosingACellOfItsAir()
    {
        var w = AiredWreck();
        var closet = Blocks(55, 56, 20, 22).ToArray();
        // In: the inner doorway opened, the generator airs the closet too.
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

    [Fact]
    public void MakesBARoomWithScrapWallsAndBackWallsItsAnalyzerPoweredByACableLearning()
    {
        var w = new Scenario(Wreck);
        // Walled where its hull was torn, and its back wall put back.
        for (int y = BTop; y <= BBottom; y++)
            for (int x = BLeft; x <= BRight; x++)
            {
                if (!Torn(x, y)) continue;
                bool edge = x == BLeft || y == BTop || y == BBottom;
                foreach (int cell in Blocks(x, x, y, y)) w.Do(new Place(cell, edge ? Placement.Wall("scrap") : Placement.BackWall("scrap")));
            }
        w.Run();
        var room = w.M.Rooms.RoomAt(TechStation);
        Assert.NotEqual(RoomsModule.Space, room);
        Assert.Empty(w.M.Rooms.All.First(r => r.Id == room).Openings);
    }
}
