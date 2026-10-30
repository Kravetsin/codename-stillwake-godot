using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using Machines = Stillwake.Content.Machines;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The crusher (economy rework): a fed station on power that crushes rock and asteroid ore into gravel, iron ore and, by
/// chance (smelt-chances), quartz, at full speed while it draws; anything not of its process sits in it unworked.
/// </summary>
public class CrusherTests
{
    /// <summary>A solar panel out under the stars cabled to a crusher in a room below, its 5 over the 4 the crusher draws.</summary>
    private static Scenario World(bool cabled = true, uint seed = 1)
    {
        string[] rows = ["PPP       ", "          ", "##########", "#kk......#", "#kk......#", "##########"];
        IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>? cables = cabled ? [((0, 0), (1, 3))] : null;
        return new Scenario(Layout.Parse(rows, null, cables), seed, doubled: true)
            .GiveItems(0, ("asteroidOre", 40), ("rock", 4), ("scrapChunk", 2), ("quartz", 2));
    }

    private static int Crusher(Scenario w) => w.M.Grid.MachinesOf("crusher").Single().Id;
    private static int Demand(Scenario w) => w.M.Power.NetworkOf(0)!.Demand;
    private static int Held(Scenario w, int machine, string item) => w.M.Items.Crate(machine).Sum(s => s?.Item == item ? s.Count : 0);
    private static int Work(string item) => Content.Items.Get(item).SmeltTicks!.Value * 1000;
    private static int Yield(string item, string of) => Content.Items.Get(item).Yields!.Single(y => y.Item == of).Count;
    private static readonly int Draws = Machines.Get("crusher").Draws!.Value;
    private static readonly int Speed = Content.Stations.Get("crusher").Speed;

    /// <summary>Moves <paramref name="count"/> of what peer 0 carries of <paramref name="item"/> into slot <paramref name="slot"/> of <paramref name="machine"/>, and runs the tick.</summary>
    private static Scenario Put(Scenario w, int machine, string item, int count = 1, int slot = 0)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
        return w.Do(new Move(new Slot(from), new Target(slot, machine), count)).Run();
    }

    /// <summary>Sets what machine <paramref name="id"/> holds, slot by slot, as no command would: a test's starting stock.</summary>
    private static Scenario Stock(Scenario w, int id, params Stack?[] slots)
    {
        Stacks.SetCrate((ItemsState)w.M.Items.Slice, id, slots);
        return w;
    }

    [Fact]
    public void CrushesAsteroidOreIntoIronOreAndTwoGravelAtFullSpeedDrawingWhileItWorks()
    {
        var w = World().Run(2);
        int crusher = Crusher(w);
        Assert.Equal(("crush", 1000), (Content.Stations.Get("crusher").Process, Speed));
        Assert.Equal((1, 2), (Yield("asteroidOre", "ironOre"), Yield("asteroidOre", "gravel")));
        Assert.Equal(0, Demand(w));
        // The tick it is fed is the first of its work, at the share the network gave it resting.
        Put(w, crusher, "asteroidOre");
        Assert.Equal(Speed, w.M.Crafting.Progress(crusher));
        Assert.True(w.M.Crafting.Works(crusher));
        w.Run();
        Assert.Equal(Draws, Demand(w));
        w.Run(Work("asteroidOre") / Speed - 3);
        Assert.Equal(0, Held(w, crusher, "gravel"));
        w.Run();
        Assert.Equal((0, 1, 2), (Held(w, crusher, "asteroidOre"), Held(w, crusher, "ironOre"), Held(w, crusher, "gravel")));
        Assert.InRange(Held(w, crusher, "quartz"), 0, 1);
        // Empty, it rests: drawing nothing from the next tick.
        Assert.False(w.M.Crafting.Works(crusher));
        w.Run();
        Assert.Equal(0, Demand(w));
    }

    /// <summary>
    /// Its quartz is drawn from the world's Rng as each crush completes, one in two: on world seed 1 twenty ores give ten,
    /// pinned, and a second world on that seed the same; each its iron ore and two gravel.
    /// </summary>
    [Fact]
    public void GivesAQuartzAboutOneAsteroidOreInTwoAsTheWorldsRngDrawsIt()
    {
        Scenario Crushed(uint seed)
        {
            var w = World(seed: seed).Run(2);
            Put(w, Crusher(w), "asteroidOre", 20);
            return w.Run(20 * Work("asteroidOre") / Speed);
        }
        Assert.Equal(2, Content.Items.Get("asteroidOre").Byproducts!.Single(b => b.Item == "quartz").OneIn);
        var (a, b) = (Crushed(1), Crushed(1));
        int crusher = Crusher(a);
        Assert.Equal((0, 20, 40), (Held(a, crusher, "asteroidOre"), Held(a, crusher, "ironOre"), Held(a, crusher, "gravel")));
        Assert.Equal(10, Held(a, crusher, "quartz"));
        Assert.Equal(a.World.Hash(), b.World.Hash());
    }

    [Fact]
    public void CrushesRockIntoTwoGravelAQuartzRarely()
    {
        var w = World().Run(2);
        int crusher = Crusher(w);
        Assert.Equal(12, Content.Items.Get("rock").Byproducts!.Single(b => b.Item == "quartz").OneIn);
        Put(w, crusher, "rock");
        w.Run(Work("rock") / Speed - 2);
        Assert.Equal(0, Held(w, crusher, "gravel"));
        w.Run();
        Assert.Equal((0, Yield("rock", "gravel")), (Held(w, crusher, "rock"), Held(w, crusher, "gravel")));
        Assert.Equal(2, Yield("rock", "gravel"));
        Assert.InRange(Held(w, crusher, "quartz"), 0, 1);
    }

    /// <summary>
    /// What is not of its process is taken in and sits there unworked, the crusher resting: a scrap chunk, which the furnace
    /// smelts, and quartz, which the smelter does; neither too cold for it, only not crushed.
    /// </summary>
    [Fact]
    public void LeavesWhatIsNotCrushedUnworkedRestingAScrapChunkAmongIt()
    {
        var w = World().Run(2);
        int crusher = Crusher(w);
        foreach (string item in (string[])["scrapChunk", "quartz"])
        {
            Put(w, crusher, item).Run(200);
            Assert.Empty(w.Refusals);
            Assert.Equal(0, w.M.Crafting.Progress(crusher));
            Assert.False(w.M.Crafting.Works(crusher));
            Assert.False(w.M.Crafting.TooCold(crusher));
            Assert.Null(w.M.Crafting.WorkOf(crusher));
            Assert.Equal(0, Demand(w));
            Assert.Equal(1, Held(w, crusher, item));
            w.Do(new Move(new Slot(0, crusher), new Target(), 1)).Run();
        }
    }

    [Fact]
    public void CrushesNothingWithNoCableKeepingWhatItHolds()
    {
        var w = World(cabled: false);
        int crusher = Crusher(w);
        Put(w, crusher, "asteroidOre").Run(200);
        Assert.Equal(0, w.M.Crafting.Progress(crusher));
        Assert.False(w.M.Crafting.Works(crusher));
        Assert.Equal(1, Held(w, crusher, "asteroidOre"));
    }

    /// <summary>
    /// It works only with room for the most a crush makes, its quartz counted: outputs that take the ore's iron ore and
    /// gravel but have no slot for a quartz hold it, resting; a slot freed, it works on.
    /// </summary>
    [Fact]
    public void RestsWhileItsOutputsLackRoomForTheMostACrushMakes()
    {
        var w = World().Run(2);
        int crusher = Crusher(w);
        int ironOre = Content.Items.Get("ironOre").Stack, gravel = Content.Items.Get("gravel").Stack;
        Stock(w, crusher, new Stack("asteroidOre", 1), new Stack("canister", 5), new Stack("ironOre", ironOre - 1), new Stack("gravel", gravel - 2)).Run(3);
        Assert.False(w.M.Crafting.CanSmelt(crusher));
        Assert.False(w.M.Crafting.TooFew(crusher));
        Assert.False(w.M.Crafting.Works(crusher));
        Assert.Equal(0, w.M.Crafting.Progress(crusher));
        Assert.Equal(0, Demand(w));
        w.Do(new Move(new Slot(1, crusher), new Target(), 5)).Run();
        Assert.True(w.M.Crafting.Works(crusher));
        Assert.Equal(Speed, w.M.Crafting.Progress(crusher));
        w.Run();
        Assert.Equal(Draws, Demand(w));
    }

    [Fact]
    public void IsBoughtInTheTree()
    {
        var w = new Scenario(Layout.Parse(["####", "#..#", "####"]) with { Known = Content.Research.StartKnown });
        var crusher = w.M.Research.Recipes().Single(r => r.Id == "crusher");
        Assert.False(crusher.Known);
    }
}
