using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Pipes;
using Xunit;
using Air = Stillwake.Content.Air;

namespace Stillwake.Sim.Tests;

/// <summary>Tanks in a line (tanks-in-a-line): a tank piped from its output to another's input pours its liquid on.</summary>
public class TankLineTests
{
    /// <summary>
    /// A cell a character: tanks A, B and C, inputs (0, 2), (6, 2), (12, 2) and outputs (3, 2), (9, 2), (15, 2); a vaporizer
    /// at (18, 0), its input (18, 2), cabled to a panel out in space under it.
    /// </summary>
    private static readonly string[] Line =
    [
        "TTTT..TTTT..TTTT..GGGG",
        "TTTT..TTTT..TTTT..GGGG",
        "TTTT..TTTT..TTTT..GGGG",
        "TTTT..TTTT..TTTT..GGGG",
        "                      ",
        "                PPPPPP",
        "                PPPPPP",
    ];

    private static Scenario World() =>
        new Scenario(Layout.Parse(Line, null, [((16, 5), (18, 0))]), doubled: false).GiveItems(0, ("canister", Air.TankCapacity / Air.Canister + 1), ("tube", 30));

    private static Scenario Piped()
    {
        var w = World();
        return w.Do(new LayPipe(w.At(3, 2), w.At(6, 2))).Do(new LayPipe(w.At(9, 2), w.At(12, 2))).Run();
    }

    private static int[] Tanks(Scenario w) => [.. w.M.Grid.MachinesOf("tank").Select(m => m.Id)];
    private static int[] Held(Scenario w) => [.. Tanks(w).Select(w.M.Atmosphere.OxygenIn)];

    private static int Canisters(Scenario w, int peer) => w.M.Items.Totals(peer).Where(t => t.Item == "canister").Sum(t => t.Count);

    /// <summary>A canister drawn out of a tank takes a canister's liquid and gives the sender one (industrial-oxygen).</summary>
    [Fact]
    public void DrawsACanisterOutTakingItsLiquid()
    {
        var w = World();
        int tank = Tanks(w)[0], carried = Canisters(w, 0);
        w.Do(new LoadCanister(tank)).Do(new LoadCanister(tank)).Run();
        w.Do(new DrawCanister(tank)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Air.Canister, w.M.Atmosphere.OxygenIn(tank));
        Assert.Equal(carried - 1, Canisters(w, 0));
        // Loaded back, the tank holds what it did; drawn to nothing, it leaves the list.
        w.Do(new LoadCanister(tank)).Run();
        Assert.Equal(2 * Air.Canister, w.M.Atmosphere.OxygenIn(tank));
        w.Do(new DrawCanister(tank)).Do(new DrawCanister(tank)).Run();
        Assert.Equal(0, w.M.Atmosphere.OxygenIn(tank));
        Assert.Equal(carried, Canisters(w, 0));
    }

    /// <summary>
    /// A stack of canisters saved before they stacked to 5 (industrial-oxygen) loads as it was, moves whole into an empty slot
    /// and is loaded from as any: no save step.
    /// </summary>
    [Fact]
    public void ACanisterStackFromBeforeLoadsMovesWholeAndIsUsedFrom()
    {
        var w = World();
        int tank = Tanks(w)[0], old = 4 * Content.Items.Get("canister").Stack;
        w.Put(30, new Items.Stack("canister", old)).Run();
        w.Reload().Run();
        Assert.Equal(new Items.Stack("canister", old), w.M.Items.Inventory(0)[30]);
        w.Do(new Items.Move(new Items.Place(30), new Items.Target(31), old)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(new Items.Stack("canister", old), w.M.Items.Inventory(0)[31]);
        int carried = Canisters(w, 0);
        w.Do(new LoadCanister(tank)).Run();
        Assert.Equal(carried - 1, Canisters(w, 0));
    }

    /// <summary>Not from what is no tank, nor from a tank holding less than a canister, nor into a full inventory; nothing changed.</summary>
    [Fact]
    public void RefusesADrawFromNoTankFromLessThanACanisterOrIntoAFullBag()
    {
        var w = World();
        int tank = Tanks(w)[0];
        w.Do(new DrawCanister(999)).Do(new DrawCanister(tank)).Run();
        w.Do(new LoadCanister(tank)).Run();
        w.GiveItems(1, ("planks", Content.Inventory.Slots * Content.Items.Get("planks").Stack));
        w.Do(new DrawCanister(tank) { From = 1 }).Run();
        Assert.Equal(["atmosphere.noTank", "atmosphere.tankLow", "items.full"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(Air.Canister, w.M.Atmosphere.OxygenIn(tank));
    }

    /// <summary>A tank's hopper pours a canister put into it when it fits, as one loaded by hand, and waits while full (industrial-oxygen).</summary>
    [Fact]
    public void ItsHopperPoursACanisterWhenItFitsAndWaitsWhileFull()
    {
        var w = World();
        int tank = Tanks(w)[0], slot = Content.Machines.Get("tank").SlotTakes!.Value.Slot;
        Hop(w, tank, slot).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Air.Canister, w.M.Atmosphere.OxygenIn(tank));
        Assert.Null(w.M.Items.Crate(tank)[slot]);
        for (int i = 1; i < Air.TankCapacity / Air.Canister; i++) w.Do(new LoadCanister(tank));
        Hop(w.Run(), tank, slot).Run(5);
        Assert.Equal(Air.TankCapacity, w.M.Atmosphere.OxygenIn(tank));
        Assert.Equal(1, w.M.Items.Crate(tank)[slot]?.Count);
    }

    /// <summary>Peer 0 moves a canister into <paramref name="slot"/> of <paramref name="tank"/>.</summary>
    private static Scenario Hop(Scenario w, int tank, int slot)
    {
        int from = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "canister");
        return w.Do(new Items.Move(new Items.Place(from), new Items.Target(slot, tank), 1));
    }

    [Fact]
    public void PoursALineIntoItsLastTankTankFlowATick()
    {
        var w = Piped();
        Assert.Empty(w.Refusals);
        w.Do(new LoadCanister(Tanks(w)[0])).Run();
        // A gives B its flow and B gives it on to C in the same tick.
        Assert.Equal([Air.Canister - Air.TankFlow, 0, Air.TankFlow], Held(w));
        w.Run(Air.Canister / Air.TankFlow);
        Assert.Equal([0, 0, Air.Canister], Held(w));
    }

    [Fact]
    public void FillsTheLastTankThenTheOneBeforeIt()
    {
        var w = Piped();
        int[] tanks = Tanks(w);
        int full = Air.TankCapacity / Air.Canister;
        for (int i = 0; i < full; i++) w.Do(new LoadCanister(tanks[0]));
        w.Do(new LoadCanister(tanks[1])).Run((full + 1) * Air.Canister / Air.TankFlow);
        Assert.Equal([0, (full + 1) * Air.Canister - Air.TankCapacity, Air.TankCapacity], Held(w));
    }

    [Fact]
    public void PoursNothingBackIntoAnOutputNorThroughAnUnpipedLine()
    {
        // B's output to C's input only: A, unpiped, keeps its liquid; C, the last, keeps what it has.
        var w = World();
        w.Do(new LayPipe(w.At(9, 2), w.At(12, 2))).Run();
        int[] tanks = Tanks(w);
        w.Do(new LoadCanister(tanks[0])).Do(new LoadCanister(tanks[2])).Run(100);
        Assert.Equal([Air.Canister, 0, Air.Canister], Held(w));
    }

    [Fact]
    public void AVaporizerDrawsOnlyTheTanksWhoseOutputIsOnItsInput()
    {
        // C's output to the vaporizer's input and on through two joints to A's input: the vaporizer has C's liquid
        // behind it, never A's, which only takes; powered, it makes a unit a tick.
        var w = World();
        int joint = w.At(17, 4), bend = w.At(8, 4);
        w.Do(new LayPipe(w.At(15, 2), w.At(18, 2))).Do(new LayPipe(w.At(18, 2), joint)).Do(new LayPipe(joint, bend))
            .Do(new LayPipe(bend, w.At(0, 2))).Run();
        Assert.Empty(w.Refusals);
        int vaporizer = w.M.Grid.MachinesOf("generator")[0].Id;
        w.Do(new LoadCanister(Tanks(w)[0])).Run(10);
        Assert.Equal(0, w.M.Atmosphere.MakesAt(vaporizer));
        // C behind it, and pouring on into A, whose input is on its output.
        w.Do(new LoadCanister(Tanks(w)[2])).Run(10);
        Assert.Equal(1000, w.M.Atmosphere.MakesAt(vaporizer));
        Assert.Equal(Air.Canister + 10 * Air.TankFlow, w.M.Atmosphere.OxygenIn(Tanks(w)[0]));
    }
}
