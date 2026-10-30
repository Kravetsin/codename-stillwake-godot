using Stillwake.Sim.Crafting;
using Stillwake.Sim.Items;
using Xunit;
using Machines = Stillwake.Content.Machines;
using Move = Stillwake.Sim.Items.Move;
using Slot = Stillwake.Sim.Items.Place;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The electric smelter and heat (0057): a fed station on power, twice the furnace's speed and hot enough for
/// quartz and gravel; what is too hot for a station is taken in and kept, never worked.
/// </summary>
public class SmelterTests
{
    /// <summary>
    /// A solar panel out under the stars over a room with a smelter and an oxygen generator; the panel is cabled
    /// to the smelter, and with <paramref name="short"/> to the generator too, which draws 4 of its 5.
    /// </summary>
    private static Scenario World(bool cabled = true, bool @short = false)
    {
        string[] rows = ["PPP         ", "            ", "############", "#EE.GG.....#", "#EE.GG.....#", "############"];
        IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>? cables = !cabled ? null
            : @short ? [((0, 0), (1, 3)), ((0, 0), (4, 3))]
            : [((0, 0), (1, 3))];
        return new Scenario(Layout.Parse(rows, null, cables), doubled: true)
            .GiveItems(0, ("quartz", 4), ("gravel", 2 * Takes), ("glass", 2), ("rock", 2));
    }

    /// <summary>A sealed room aired by a tank piped to a vent, holding a furnace, as the furnace's tests.</summary>
    private static Scenario Furnace()
    {
        var w = new Scenario(Layout.Parse(["##########", "#FF.TT...#", "#FF.TTV..#", "##########"], ["", "    o", "    ---"]), doubled: true)
            .GiveItems(0, ("ironOre", 2), ("quartz", 2), ("planks", 2));
        for (int i = 0; i < 2000 && !w.M.Atmosphere.AiredAt(w.At(1, 1)); i++) w.Run();
        return w;
    }

    private static int Smelter(Scenario w) => w.M.Grid.MachinesOf("smelter").Single().Id;
    private static int FurnaceOf(Scenario w) => w.M.Grid.MachinesOf("furnace").Single().Id;
    private static int Demand(Scenario w) => w.M.Power.NetworkOf(0)!.Demand;
    private static int Held(Scenario w, int machine, string item) => w.M.Items.Crate(machine).Sum(s => s?.Item == item ? s.Count : 0);
    private static int Carried(Scenario w, string item) => w.M.Items.Inventory(0).Sum(s => s?.Item == item ? s.Count : 0);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int Work(string item) => Content.Items.Get(item).SmeltTicks!.Value * 1000;
    private static int Yield(string item, string of) => Content.Items.Get(item).Yields!.Single(y => y.Item == of).Count;
    private static readonly int Draws = Machines.Get("smelter").Draws!.Value;
    private static readonly int Takes = Content.Items.Get("gravel").SmeltTakes;

    /// <summary>Moves <paramref name="count"/> of what peer 0 carries of <paramref name="item"/> into slot <paramref name="slot"/> of <paramref name="machine"/>, and runs the tick.</summary>
    private static Scenario Put(Scenario w, int machine, string item, int count = 1, int slot = 0)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
        return w.Do(new Move(new Slot(from), new Target(slot, machine), count)).Run();
    }

    [Fact]
    public void SmeltsQuartzIntoSiliconAtTwiceTheFurnacesSpeedDrawingWhileItWorks()
    {
        var w = World().Run(2);
        int smelter = Smelter(w);
        Assert.Equal((2000, 2), (Content.Stations.Get("smelter").Speed, Content.Stations.Get("smelter").Heat));
        Assert.Equal(0, Demand(w));
        // The tick it is fed is the first of its work, at the share the network gave it resting.
        Put(w, smelter, "quartz");
        Assert.Equal(2000, w.M.Crafting.Progress(smelter));
        Assert.True(w.M.Crafting.Works(smelter));
        w.Run();
        Assert.Equal(Draws, Demand(w));
        // The furnace's ticks of work in half as many.
        w.Run(Work("quartz") / 2000 - 3);
        Assert.Equal(0, Held(w, smelter, "silicon"));
        w.Run();
        Assert.Equal(Yield("quartz", "silicon"), Held(w, smelter, "silicon"));
        Assert.Equal(0, Held(w, smelter, "quartz"));
        // Empty, it rests: drawing nothing from the next tick.
        Assert.False(w.M.Crafting.Works(smelter));
        w.Run();
        Assert.Equal(0, Demand(w));
    }

    [Fact]
    public void SmeltsAtFiveEighthsOnANetworkShortOfWhatItDraws()
    {
        var w = World(@short: true).Run();
        int smelter = Smelter(w);
        Put(w, smelter, "quartz");
        // Resting until now, it drew nothing on the tick it began: the panel's 5 covered the generator's 4.
        Assert.Equal(2000, w.M.Crafting.Progress(smelter));
        w.Run();
        int share = w.M.Power.Satisfaction(smelter);
        Assert.Equal(5000 / 8, share);
        int gain = 2000 * share / 1000;
        Assert.Equal(2000 + gain, w.M.Crafting.Progress(smelter));
        int ticks = (Work("quartz") - 2000 + gain - 1) / gain;
        w.Run(ticks - 2);
        Assert.Equal(0, Held(w, smelter, "silicon"));
        w.Run();
        Assert.Equal(Yield("quartz", "silicon"), Held(w, smelter, "silicon"));
    }

    [Fact]
    public void SmeltsGravelIntoGlassAsManyAtATimeAsASmeltTakesItsHeatEnoughForIt()
    {
        var w = World();
        int smelter = Smelter(w);
        Put(w, smelter, "gravel", 2 * Takes);
        Assert.False(w.M.Crafting.TooCold(smelter));
        w.Run(2 * Work("gravel") / 2000 - 1);
        Assert.Equal(2 * Yield("gravel", "glass"), Held(w, smelter, "glass"));
        Assert.Equal(0, Held(w, smelter, "gravel"));
    }

    /// <summary>A smelt of gravel takes four (smelt-chances): three wait, the smelter resting; a fourth, and one glass comes of them all.</summary>
    [Fact]
    public void MakesOneGlassOfFourGravelAndRestsWhileThreeWait()
    {
        var w = World().Run(2);
        int smelter = Smelter(w);
        Assert.Equal((4, 1), (Takes, Yield("gravel", "glass")));
        Put(w, smelter, "gravel", Takes - 1).Run(3);
        Assert.True(w.M.Crafting.TooFew(smelter));
        Assert.False(w.M.Crafting.TooCold(smelter));
        Assert.Equal(0, w.M.Crafting.Progress(smelter));
        Assert.Equal(0, Demand(w));
        Put(w, smelter, "gravel");
        Assert.False(w.M.Crafting.TooFew(smelter));
        w.Run(Work("gravel") / 2000 - 1);
        Assert.Equal((0, 1), (Held(w, smelter, "gravel"), Held(w, smelter, "glass")));
    }

    [Fact]
    public void MakesNothingWithNoCableKeepingWhatItHolds()
    {
        var w = World(cabled: false);
        int smelter = Smelter(w);
        Put(w, smelter, "quartz").Run(200);
        Assert.Empty(w.Refusals);
        Assert.Equal(0, w.M.Crafting.Progress(smelter));
        Assert.False(w.M.Crafting.Works(smelter));
        Assert.Equal(1, Held(w, smelter, "quartz"));
    }

    [Fact]
    public void RestsDrawingNothingWhileItsOutputsAreFullOrItsInputHoldsWhatDoesNotSmelt()
    {
        var w = World();
        int smelter = Smelter(w);
        // Glass smelts into nothing, and rock is crushed: each sits there, the smelter resting, not too cold for it either.
        foreach (string item in (string[])["glass", "rock"])
        {
            Put(w, smelter, item).Run(2);
            Assert.Equal(0, Demand(w));
            Assert.False(w.M.Crafting.TooCold(smelter));
            Assert.Equal(0, w.M.Crafting.Progress(smelter));
            w.Do(new Move(new Slot(0, smelter), new Target(), 1)).Run();
        }

        // Its outputs full of what nothing smelts into: the quartz waits, the smelter resting.
        Put(w, smelter, "quartz");
        w.FillCrate(smelter, ("canister", 3 * Content.Items.Get("canister").Stack)).Run(3);
        Assert.Equal(0, Demand(w));
        Assert.False(w.M.Crafting.Works(smelter));
        Assert.Equal(1, Held(w, smelter, "quartz"));
        // Room again: it works on from the work it had done.
        w.Do(new Move(new Slot(1, smelter), new Target(), 999)).Run(2);
        Assert.Equal(Draws, Demand(w));
        Assert.Equal(3 * 2000, w.M.Crafting.Progress(smelter));
    }

    [Fact]
    public void RefusesEveryCraftAsNotMadeThere()
    {
        var w = World().GiveItems(0, ("iron", 10));
        int smelter = Smelter(w);
        w.Do(new Craft(smelter, "plate", 1)).Do(new Cancel(smelter, 1)).Run();
        Assert.Equal(["crafting.notHere", "crafting.notHere"], Reasons(w));
    }

    /// <summary>
    /// The user's rule: what needs more heat than a station has is taken into its input, so the player sees the
    /// right kind of machine; the station says it is too cold and works nothing, and nothing is lost. Replaced by
    /// what it can work, it works: iron ore into iron.
    /// </summary>
    [Fact]
    public void AFurnaceTakesQuartzInButIsTooColdForItKeepingItAndItsFuelThenSmeltsIronOre()
    {
        var w = Furnace();
        int furnace = FurnaceOf(w);
        int fuel = Machines.Get("furnace").FuelSlot!.Value;
        Put(w, furnace, "planks", 2, fuel);
        Put(w, furnace, "quartz", 2).Run(100);
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Crafting.TooCold(furnace));
        Assert.False(w.M.Crafting.CanSmelt(furnace));
        var at = w.M.Furnace.At(furnace);
        Assert.Equal((false, "crafting.tooCold", 0), (at.Burning, at.Problem, at.Progress));
        Assert.Equal((2, 2, 0), (Held(w, furnace, "quartz"), Held(w, furnace, "planks"), Held(w, furnace, "silicon")));

        w.Do(new Move(new Slot(0, furnace), new Target(), 2)).Run();
        Assert.Equal(2, Carried(w, "quartz"));
        Put(w, furnace, "ironOre");
        Assert.False(w.M.Crafting.TooCold(furnace));
        Assert.True(w.M.Furnace.At(furnace).Burning);
        // It smelts from the tick after it lights: the ore's work at the furnace's speed.
        w.Run(Work("ironOre") / 1000 - 1);
        Assert.Equal(0, Held(w, furnace, "iron"));
        w.Run();
        Assert.Equal(Yield("ironOre", "iron"), Held(w, furnace, "iron"));
    }

    [Fact]
    public void IsBoughtInTheTree()
    {
        var w = new Scenario(Layout.Parse(["####", "#..#", "####"]) with { Known = Content.Research.StartKnown });
        var smelter = w.M.Research.Recipes().Single(r => r.Id == "smelter");
        Assert.False(smelter.Known);
    }
}
