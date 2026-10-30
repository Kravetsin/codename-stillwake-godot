using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Furnace;
using Stillwake.Sim.Items;
using Xunit;
using FurnaceContent = Stillwake.Content.Furnace;
using ItemSpecs = Stillwake.Content.Items;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/furnace/furnace.test.ts, ported.</summary>
public class FurnaceTests
{
    /// <summary>
    /// Ten wide: a sealed room holding a furnace, and a tank of oxygen piped to a vent that airs the
    /// room. The furnace has no pipe and no cable: it burns planks and breathes the room's air.
    /// </summary>
    private static readonly string[] Rows = ["##########", "#FF.TT...#", "#FF.TTV..#", "##########"];
    private static readonly string[] Under = ["", "    o", "    ---"];

    /// <summary>The cell of the map's character at (<paramref name="x"/>, <paramref name="y"/>): its block's top-left, doubled (0104).</summary>
    private static int At(int x, int y) => 2 * y * 20 + 2 * x;
    /// <summary>A hole through the hull's right wall, two cells thick: the top row of its block at (9, 1).</summary>
    private static readonly int[] Hole = [At(9, 1), At(9, 1) + 1];
    private static Command[] Breach() => [.. Hole.Select(c => new ClearAt(c, "structure"))];
    private static Command[] Seal() => [.. Hole.Select(c => new Place(c, Grid.Placement.Wall()))];
    private static readonly int FurnaceAt = At(1, 1);
    private static readonly int Fuel = Content.Machines.Get("furnace").FuelSlot!.Value;
    private static readonly Content.ItemSpec Chunk = ItemSpecs.Get("scrapChunk");
    private static readonly Content.ItemSpec Plank = ItemSpecs.Get("planks");
    private static readonly int ChunkTicks = Chunk.SmeltTicks!.Value;
    private static readonly int ChunkScrap = Chunk.Yields!.Single(y => y.Item == "scrap").Count;

    /// <summary>The world, its room aired, a peer 0 carrying <paramref name="carry"/>, and the furnace's machine id.</summary>
    private static (Scenario W, int Id) SetUp(params (string Item, int Count)[] carry)
    {
        var w = new Scenario(AirTesting.Vaporized(Rows, Under), doubled: true)
            .GiveItems(0, carry.Length > 0 ? carry : [("scrapChunk", 2), ("planks", 2)]);
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(FurnaceAt); i++) w.Run();
        Assert.True(w.M.Atmosphere.AiredAt(FurnaceAt));
        return (w, w.M.Grid.MachineAt(FurnaceAt)?.Id ?? 0);
    }

    /// <summary>The oxygen the chain holds: the tank's liquid and the vaporizer's gas (liquid-oxygen).</summary>
    private static int TankOxygen(Scenario w) => w.M.Grid.Machines.Sum(m => w.M.Atmosphere.OxygenIn(m.Id));

    /// <summary>Moves <paramref name="count"/> of the first stack peer 0 holds of <paramref name="item"/> into slot <paramref name="slot"/> of furnace <paramref name="id"/>.</summary>
    private static Scenario Put(Scenario w, int id, string item, int slot, int count = 1)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
        return w.Do(new Move(new Items.Place(from), new Target(slot, id), count)).Run();
    }

    private static int FuelOf(Scenario w, int id) => w.M.Items.Crate(id)[Fuel]?.Count ?? 0;
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static Scenario Do(Scenario w, IEnumerable<Command> commands) { foreach (var c in commands) w.Do(c); return w; }

    [Fact]
    public void LightsOnlyWithSalvageToSmeltFuelAndAirUsingOneFuelUp()
    {
        var (w, id) = SetUp();
        Put(w, id, "planks", Fuel, 2);
        w.Run(5);
        // Fuel alone: nothing to smelt, no fire.
        Assert.Equal(new Smelting(false), w.M.Furnace.At(id));
        Assert.Equal(2, FuelOf(w, id));
        Put(w, id, "scrapChunk", 0);
        var at = w.M.Furnace.At(id);
        Assert.True(at.Burning);
        Assert.Null(at.Problem);
        Assert.Equal(1, FuelOf(w, id));
    }

    [Fact]
    public void SaysItHasNoFuelAndStands()
    {
        var (w, id) = SetUp();
        Put(w, id, "scrapChunk", 0);
        w.Run(20);
        var at = w.M.Furnace.At(id);
        Assert.False(at.Burning);
        Assert.Equal(0, at.Progress);
        Assert.Equal("furnace.noFuel", at.Problem);
    }

    [Fact]
    public void SmeltsAnItemInItsSmeltingTicksOfFireAndTheNextOnTheSameFire()
    {
        var (w, id) = SetUp();
        Put(w, id, "planks", Fuel);
        Put(w, id, "scrapChunk", 0, 2);
        // The tick of the move lit it; its fire burning at the start of the next, it smelts from then (0057): an
        // item takes its ticks of fire, one tick later than when the furnace smelted on the tick it lit.
        Assert.Equal(new Smelting(true, "scrapChunk", 0, ChunkTicks * 1000), w.M.Furnace.At(id));
        w.Run(ChunkTicks - 1);
        Assert.Null(w.M.Items.Crate(id)[2]);
        var at = w.M.Furnace.At(id);
        Assert.Equal((ChunkTicks - 1) * 1000, at.Progress);
        Assert.Equal(ChunkTicks * 1000, at.Work);
        w.Run();
        Assert.Equal(new Stack("scrapChunk", 1), w.M.Items.Crate(id)[0]);
        Assert.Equal(new Stack("scrap", ChunkScrap), w.M.Items.Crate(id)[2]);
        w.Run(ChunkTicks);
        Assert.Null(w.M.Items.Crate(id)[0]);
        Assert.Equal(new Stack("scrap", 2 * ChunkScrap), w.M.Items.Crate(id)[2]);
        // One plank smelted both and burns on.
        Assert.Equal(0, FuelOf(w, id));
        Assert.Equal(new Smelting(true), w.M.Furnace.At(id));
    }

    /// <summary>A grazer's fat (Creatures) both ways: in the input it smelts into biofuel as biomass does, in the fuel slot it burns twice a plank.</summary>
    [Fact]
    public void FatSmeltsIntoBiofuelOnAFireOfFatAndBurnsTwiceAPlank()
    {
        var fat = ItemSpecs.Get("fat");
        Assert.Equal(2 * Plank.BurnTicks, fat.BurnTicks);
        var (w, id) = SetUp(("fat", 3));
        Put(w, id, "fat", Fuel);
        Put(w, id, "fat", 0, 2);
        Assert.Equal(new Smelting(true, "fat", 0, fat.SmeltTicks!.Value * 1000), w.M.Furnace.At(id));
        w.Run(2 * fat.SmeltTicks!.Value);
        Assert.Null(w.M.Items.Crate(id)[0]);
        Assert.Equal(new Stack("biofuel", 2), w.M.Items.Crate(id)[2]);
        Assert.Empty(w.Refusals);
        // One fat smelted both and burns on, out only once its own burning is done.
        Assert.Equal(0, FuelOf(w, id));
        w.Run(fat.BurnTicks!.Value - 2 * fat.SmeltTicks!.Value - 1);
        Assert.Equal(new Smelting(false), w.M.Furnace.At(id));
    }

    [Fact]
    public void BurnsItsFuelDownWithNothingLeftToSmeltAndLightsNoMore()
    {
        var (w, id) = SetUp();
        Put(w, id, "planks", Fuel, 2);
        Put(w, id, "scrapChunk", 0);
        w.Run(Plank.BurnTicks!.Value - 1);
        Assert.Equal(new Smelting(false), w.M.Furnace.At(id));
        w.Run(100);
        Assert.Equal(1, FuelOf(w, id));
    }

    [Fact]
    public void BreathesItsRoomsAirWhileItBurnsABreathEveryBeat()
    {
        // Beside a twin with no fire: the room leaks alike in both, and the tank pays for it alike.
        var fired = SetUp();
        var cold = SetUp();
        foreach (var (w, id) in new[] { fired, cold }) Put(w, id, "planks", Fuel).Run(200);
        Assert.Equal(TankOxygen(cold.W), TankOxygen(fired.W));
        Put(fired.W, fired.Id, "scrapChunk", 0);
        cold.W.Run();
        // Lit on the tick of the move, it breathed then and on every beat after.
        fired.W.Run(4 * FurnaceContent.BreathEvery - 1);
        cold.W.Run(4 * FurnaceContent.BreathEvery - 1);
        Assert.Equal(4 * FurnaceContent.Breath, TankOxygen(cold.W) - TankOxygen(fired.W));
    }

    [Fact]
    public void DoesNotLightWithoutAirAndGoesOutWhenTheAirGoesItsFuelLost()
    {
        var (w, id) = SetUp();
        Put(w, id, "planks", Fuel, 2);
        Put(w, id, "scrapChunk", 0);
        Assert.True(w.M.Furnace.At(id).Burning);
        // A hole in the hull: the room is space, and the fire's next breath finds none.
        Do(w, Breach()).Run();
        w.Run(FurnaceContent.BreathEvery);
        var at = w.M.Furnace.At(id);
        Assert.False(at.Burning);
        Assert.Equal("furnace.noAir", at.Problem);
        w.Run(100);
        Assert.Equal(1, FuelOf(w, id));
        Assert.Null(w.M.Items.Crate(id)[2]);
    }

    [Fact]
    public void SmeltsTheTickItsFireGoesOutForWantOfAirAsItBurnedAtTheTicksStart()
    {
        // Crafting reads the fire as the tick begins (0057): a fire that breathes no air smelts that tick still.
        var (w, id) = SetUp();
        Put(w, id, "planks", Fuel);
        Put(w, id, "scrapChunk", 0, 2);
        Do(w.Run(10), Breach());
        bool wentOut = false;
        for (int i = 0; i < 3 * FurnaceContent.BreathEvery; i++)
        {
            var before = w.M.Furnace.At(id);
            w.Run();
            var after = w.M.Furnace.At(id);
            Assert.Equal(before.Burning ? 1000 : 0, after.Progress - before.Progress);
            wentOut |= before.Burning && !after.Burning;
        }
        Assert.True(wentOut);
        Assert.Equal(new Stack("scrapChunk", 2), w.M.Items.Crate(id)[0]);
    }

    [Fact]
    public void WaitsUnlitWithNoRoomForWhatItMakes()
    {
        var (w, id) = SetUp();
        var full = new Stack("iron", ItemSpecs.Get("iron").Stack);
        // Nothing puts items into a furnace's outputs but smelting: straight into the world instead.
        Stacks.SetCrate((ItemsState)w.M.Items.Slice, id, [new Stack("scrapChunk", 1), null, full, full, full]);
        Put(w, id, "planks", Fuel);
        w.Run(20);
        var at = w.M.Furnace.At(id);
        Assert.False(at.Burning);
        Assert.Equal("furnace.full", at.Problem);
        Assert.Equal(1, FuelOf(w, id));
    }

    [Fact]
    public void StartsOverOnAnItemTakenOutAndPutBack()
    {
        var (w, id) = SetUp();
        Put(w, id, "planks", Fuel);
        Put(w, id, "scrapChunk", 0);
        w.Run(30);
        w.Do(new Move(new Items.Place(0, id), new Target(), 1)).Run();
        Assert.Equal(new Smelting(true), w.M.Furnace.At(id));
        Put(w, id, "scrapChunk", 0);
        Assert.Equal(1000, w.M.Furnace.At(id).Progress);
    }

    [Fact]
    public void TakesNothingPutIntoItsOutputsAndGivesWhatTheyHold()
    {
        var (w, id) = SetUp();
        Put(w, id, "scrapChunk", 2);
        Assert.Equal(["items.takeOnly"], Reasons(w));
        Put(w, id, "planks", Fuel);
        Put(w, id, "scrapChunk", 0);
        w.Run(ChunkTicks);
        w.Do(new Move(new Items.Place(2, id), new Target(), 999)).Run();
        Assert.Equal(["items.takeOnly"], Reasons(w));
        var got = w.M.Items.Inventory(0).FirstOrDefault(s => s?.Item == "scrap");
        Assert.Equal(new Stack("scrap", ChunkScrap), got);
    }

    // Beyond furnace.test.ts: the README's invariant, under any moves a hero makes.

    /// <summary>How much of <paramref name="item"/> there is in peer 0's inventory, the furnace and on the floor.</summary>
    private static int Everywhere(Scenario w, int id, string item) =>
        w.M.Items.Inventory(0).Concat(w.M.Items.Crate(id)).Sum(s => s?.Item == item ? s.Count : 0)
        + w.M.Items.Floor.Sum(s => s.Item == item ? s.Count : 0);

    /// <summary>A step of a hero at a furnace: its commands, then ticks run.</summary>
    private sealed record Step(Command[] Commands, int Ticks);

    /// <summary>
    /// Items change only by moves, one fuel used up as a fire is lit, and smelting: chunks turn into
    /// their scrap and nothing else, and planks only go, never two on one tick.
    /// </summary>
    [Property(MaxTest = 40)]
    public Property ChangesItemsOnlyByMovesLightingAndSmelting()
    {
        // The furnace gets its id once the world is made: 1, the only machine before the tank.
        const int id = 1;
        var command = Gen.OneOf(
            from slot in Gen.Choose(0, 3)
            from dst in Gen.Elements<int?>(0, Fuel, 2, null)
            from count in Gen.Choose(1, 3)
            select new Command[] { new Move(new Items.Place(slot), new Target(dst, id), count) },
            from slot in Gen.Choose(0, 4)
            from count in Gen.Elements(1, 999)
            select new Command[] { new Move(new Items.Place(slot, id), new Target(), count) },
            Gen.Elements(Breach(), Seal()));
        var step = from c in command from ticks in Gen.Choose(0, 150) select new Step(c, ticks);
        var steps = from n in Gen.Choose(0, 20) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var (w, furnace) = SetUp(("scrapChunk", 10), ("planks", 5));
            Assert.Equal(id, furnace);
            int worth = Everywhere(w, id, "scrapChunk") * ChunkScrap + Everywhere(w, id, "scrap");
            int planks = Everywhere(w, id, "planks");
            foreach (var s in all)
            {
                Do(w, s.Commands);
                for (int t = 0; t <= s.Ticks; t++)
                {
                    w.Run();
                    int now = Everywhere(w, id, "planks");
                    Assert.InRange(planks - now, 0, 1);
                    planks = now;
                    Assert.Equal(worth, Everywhere(w, id, "scrapChunk") * ChunkScrap + Everywhere(w, id, "scrap"));
                }
            }
        });
    }
}
