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
