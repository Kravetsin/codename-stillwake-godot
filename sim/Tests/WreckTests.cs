using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Power;
using Stillwake.Sim.Rooms;
using Xunit;
using CrewSpec = Stillwake.Content.Crew;
using FurnaceSpec = Stillwake.Content.Furnace;
using HeroSpec = Stillwake.Content.Hero;
using ItemSpecs = Stillwake.Content.Items;
using Machines = Stillwake.Content.Machines;
using Move = Stillwake.Sim.Items.Move;
using ResearchSpec = Stillwake.Content.Research;
using Slot = Stillwake.Sim.Items.Place;
using WreckRows = Stillwake.Content.Wreck;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/wreck.test.ts, ported: the world every new game starts in.</summary>
public class WreckTests
{
    private static readonly Layout Wreck = Registry.Wreck;
    private static int At(int x, int y) => y * Wreck.Width + x;
    /// <summary>Room A's cells: the generator's room, where the hero wakes.</summary>
    private static readonly int[] A = Enumerable.Range(0, 4 * 8).Select(i => At(46 + i % 8, 19 + i / 8)).ToArray();
    private static readonly int DoorToB = At(45, 21);
    private static readonly int Inner = At(54, 21);
    private static readonly int Outer = At(57, 21);
    /// <summary>Room B's analyzer, and the cells of B's hull, floor and back wall torn away: drawn as space.</summary>
    private static readonly int Analyzer = At(41, 21);
    private const int BLeft = 30, BRight = 44, BTop = 18, BBottom = 23;
    private static bool Torn(int x, int y) => x < WreckRows.Rows[y].Length && WreckRows.Rows[y][x] == ' ';
    private static readonly int Panel = At(50, 17);
    /// <summary>The lifepod's top-left cell, in A against the airlock's inner doorway, and its hatch.</summary>
    private static readonly int Pod = At(51, 20), Hatch = At(52, 22);
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
    public void LeavesRoomToBuildAllAroundItTheEdgeFarFromTheHull()
    {
        Assert.Equal((192, 96), (Wreck.Width, Wreck.Height));
        var hull = Wreck.Rows.SelectMany((row, y) => row.Select((c, x) => (c, x, y)).Where(h => h.c != ' ')).ToList();
        // The margin a tether must stay within: at least this many cells of space on every side.
        int margin = new[]
        {
            hull.Min(h => h.x), hull.Min(h => h.y), Wreck.Width - 1 - hull.Max(h => h.x), Wreck.Height - 1 - hull.Max(h => h.y),
        }.Min();
        Assert.True(margin >= 16);
        // A hero on a tether from the hull, half a cell in, stays clear of the edge, all of its box.
        Assert.True(margin * 1000 - 500 > HeroSpec.Tether + HeroSpec.Height / 2);
    }

    [Fact]
    public void HoldsAPanelCabledToALampAndAGeneratorAVentAnAnalyzerTheLifepodNoFurnaceOrCrate()
    {
        var m = new Registry();
        World.Create(1, Wreck, m.All);
        Assert.Equal(["solarPanel", "lamp", "lifepod", "analyzer", "generator", "vent"], m.Grid.Machines.Select(x => x.Kind));
        Assert.Equal(ResearchSpec.StartKnown.Order(StringComparer.Ordinal), m.Research.Recipes().Where(r => r.Known).Select(r => r.Id).Order(StringComparer.Ordinal));
        Assert.Empty(m.Research.Analysed);
        var doors = Wreck.CellsOf('D').Select(m.Grid.StructureAt).ToList();
        Assert.Equal(Enumerable.Repeat(Structure.DoorClosed, 9), doors);
        var cells = m.Grid.Machines.Select(x => x.Cell).ToArray();
        var (panel, lamp, generator) = (cells[0], cells[1], cells[4]);
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
        w.Do(new ToggleDoor(DoorToB)).Do(new Enter(w.M.Crew.Spawn) { From = 1 }).Run(10);
        Assert.Equal(0, AiredIn(w, A));
        w.Run(30 * CrewSpec.BreathEvery);
        Assert.Equal(CrewSpec.Suit, w.M.Crew.HeroOf(1)?.Suit);
        // Its Fabricator works with no power and no air.
        w.GiveItems(1, ("scrap", 2));
        w.Do(new Sim.Crafting.Craft(pod, "wire", 1) { From = 1 }).Run(PodTicks("wire"));
        Assert.Equal(new Stack("wire", 1), w.M.Items.Crate(pod)[0]);
    }

    /// <summary>Ticks one unit of <paramref name="craft"/> takes at the pod.</summary>
    private static int PodTicks(string craft) => Content.Crafts.Get(craft).Ticks * 1000 / Content.Crafts.PodSpeed;

    /// <summary>Crafts one of <paramref name="craft"/> at the pod for peer 0 and takes it from the pod's outputs.</summary>
    private static void CraftAtPod(Scenario w, string craft)
    {
        int pod = w.M.Grid.MachineAt(Pod)!.Id;
        w.Do(new Sim.Crafting.Craft(pod, craft, 1)).Run(PodTicks(craft));
        w.Do(new Move(new Slot(0, pod), new Target(), 99)).Run();
    }

    /// <summary>A furnace built in A by peer 0 of the salvage it costs, given with <paramref name="more"/>; its machine id.</summary>
    private static int BuildFurnace(Scenario w, params (string Item, int Count)[] more)
    {
        w.GiveItems(0, [.. Machines.Get("furnace").Cost, .. more]);
        w.Do(new PlacePlan(At(49, 21), new Placement("machine", "furnace"))).Run();
        int plan = w.M.Building.Plans.FirstOrDefault()?.Id ?? 0;
        Machine? Built() => w.M.Grid.Machines.FirstOrDefault(m => m.Kind == "furnace");
        // Its salvage delivered on the first press, then built on the presses after.
        for (int i = 0; i < 2 * Machines.Get("furnace").BuildTicks && Built() == null; i++) w.Do(new Work(plan)).Run();
        int id = Built()?.Id ?? 0;
        Assert.True(id > 0);
        return id;
    }

    private static int SlotOf(Scenario w, string item) => w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);

    [Fact]
    public void BuildsAFurnaceInAOfCaughtSalvageBurnsAPlankInItAndSmeltsScrap()
    {
        var w = AiredWreck();
        int id = BuildFurnace(w, ("scrapChunk", 1), ("planks", 1));
        w.Do(new Move(new Slot(SlotOf(w, "planks")), new Target(FurnaceSpec.FuelSlot, id), 1)).Run();
        w.Do(new Move(new Slot(SlotOf(w, "scrapChunk")), new Target(Crate: id), 1)).Run();
        // Lit in A's air on the tick of the move, which was the first of its smelting.
        var chunk = ItemSpecs.Get("scrapChunk");
        w.Run(chunk.SmeltTicks!.Value - 1);
        Assert.Equal(new Stack("scrap", chunk.Yields!.Single(y => y.Item == "scrap").Count), w.M.Items.Crate(id)[2]);
    }

    [Fact]
    public void SmeltsScrapCraftsAWireAtThePodCablesTheAnalyzerAnalysesIronAndCraftsAPlate()
    {
        var w = AiredWreck();
        int furnace = BuildFurnace(w, ("scrapChunk", 1), ("hullPlate", 1), ("planks", 2));
        w.Do(new Move(new Slot(SlotOf(w, "planks")), new Target(FurnaceSpec.FuelSlot, furnace), 2)).Run();
        w.Do(new Move(new Slot(SlotOf(w, "scrapChunk")), new Target(Crate: furnace), 1)).Run();
        w.Run(ItemSpecs.Get("scrapChunk").SmeltTicks!.Value);
        w.Do(new Move(new Slot(SlotOf(w, "hullPlate")), new Target(Crate: furnace), 1)).Run();
        w.Run(ItemSpecs.Get("hullPlate").SmeltTicks!.Value);
        for (int slot = 2; slot < 5; slot++)
            if (w.M.Items.Crate(furnace)[slot] != null) w.Do(new Move(new Slot(slot, furnace), new Target(), 999));
        w.Run();
        Assert.True(w.M.Items.Totals(0).Single(t => t.Item == "iron").Count >= 2);

        // Wire, the one craft the wreck grants, of the scrap smelted, at the pod; the cable of the wire.
        CraftAtPod(w, "wire");
        w.Do(new Lay(Panel, Analyzer)).Run();
        Assert.Empty(w.Refusals);
        int analyzer = w.M.Grid.MachineAt(Analyzer)?.Id ?? 0;
        Assert.True(w.M.Power.Satisfaction(analyzer) > 0);

        // Iron analysed opens the plate.
        w.Do(new Move(new Slot(SlotOf(w, "iron")), new Target(Crate: analyzer), 1)).Run();
        for (int i = 0; i < 4 * ResearchSpec.Ticks && !w.M.Research.Analysed.Contains("iron"); i++) w.Run();
        Assert.True(w.M.Research.Knows("plate"));
        CraftAtPod(w, "plate");
        Assert.Empty(w.Refusals);
        Assert.Equal(1, w.M.Items.Totals(0).Single(t => t.Item == "plate").Count);
    }

    [Fact]
    public void FindsRoomAAndTheAirlockBTornOpenIsSpaceItsAnalyzerUncabled()
    {
        var m = new Registry();
        World.Create(1, Wreck, m.All);
        Assert.Equal([At(46, 19), At(55, 20)], m.Rooms.All.Select(r => r.Id));
        Assert.All(m.Rooms.All, r => Assert.Empty(r.Openings));
        Assert.Equal(RoomsModule.Space, m.Rooms.RoomAt(Analyzer));
        int id = m.Grid.MachineAt(Analyzer)?.Id ?? 0;
        Assert.Equal(0, m.Power.Satisfaction(id));
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
        w.Run((CrewSpec.Suit - 3) * CrewSpec.BreathEvery);
        Assert.True(w.M.Crew.HeroOf(2)?.Suit <= 3);
        w.Do(new Enter(At(51, 21)) { From = 2 }).Run();
        w.Run(5 * 60 * 20);
        Assert.Equal(A.Length, AiredIn(w, A));
        Assert.Equal((CrewSpec.Suit, CrewSpec.Suit), (w.M.Crew.HeroOf(1)?.Suit, w.M.Crew.HeroOf(2)?.Suit));
    }

    [Fact]
    public void LetsFourHeroesBreathingInAOutrunTheGeneratorWithinAMinute()
    {
        var w = AiredWreck();
        // Out of the pod's reach, whose air would pay their breaths.
        foreach (int peer in new[] { 1, 2, 3, 4 }) w.Do(new Enter(At(45 + peer, 21)) { From = peer });
        w.Run(60 * 20);
        Assert.True(AiredIn(w, A) < A.Length);
    }

    [Fact]
    public void LosesAsAirWithinAFewTicksOnceTheDoorToTheBreachedRoomBOpens()
    {
        var w = AiredWreck();
        w.Do(new ToggleDoor(DoorToB)).Run(7);
        Assert.Equal(0, AiredIn(w, A));
    }

    [Fact]
    public void CyclesTheAirlockWithoutALosingACellOfItsAir()
    {
        var w = AiredWreck();
        var closet = new[] { 55, 56 }.SelectMany(x => new[] { 20, 21, 22 }.Select(y => At(x, y))).ToArray();
        // In: the inner doorway opened, the generator airs the closet too.
        w.Do(new ToggleDoor(Inner)).Run();
        for (int i = 0; i < 600 && AiredIn(w, closet) < closet.Length; i++) w.Run();
        Assert.Equal(closet.Length, AiredIn(w, closet));
        Assert.Equal(A.Length, AiredIn(w, A));
        // Out: the inner closed behind, the outer opened; the closet vents, A keeps its air.
        w.Do(new ToggleDoor(Inner)).Run();
        w.Do(new ToggleDoor(Outer)).Run(20);
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
                w.Do(new Place(At(x, y), edge ? Placement.Wall("scrap") : Placement.BackWall("scrap")));
            }
        w.Run();
        var room = w.M.Rooms.RoomAt(Analyzer);
        Assert.NotEqual(RoomsModule.Space, room);
        Assert.Empty(w.M.Rooms.All.First(r => r.Id == room).Openings);
        // A cable from the panel reaches it through the roof.
        w.GiveItems(0, ("wire", 1), ("scrap", 1));
        w.Do(new Lay(Panel, Analyzer)).Run();
        Assert.Empty(w.Refusals);
        int id = w.M.Grid.MachineAt(Analyzer)?.Id ?? 0;
        int slot = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "scrap");
        w.Do(new Move(new Slot(slot), new Target(Crate: id), 1)).Run();
        Assert.True(w.M.Power.Satisfaction(id) > 0);
        w.Run(2 * ResearchSpec.Ticks);
        Assert.Equal(["scrap"], w.M.Research.Analysed);
        foreach (var made in new[] { "floor", "ladder", "pipe" }) Assert.True(w.M.Research.Knows(made), made);
    }
}
