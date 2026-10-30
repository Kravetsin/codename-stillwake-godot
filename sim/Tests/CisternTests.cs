using System.Text.Json.Nodes;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Pipes;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The cistern (2026-10-29-typed-ports): water's first ports, an input on its left and an output on its right.</summary>
public class CisternTests
{
    /// <summary>
    /// A cell a character: the vent's port (0, 0); cistern A's input (6, 2), output (9, 2); cistern B's input (14, 2), output
    /// (17, 2); the tank's air output (0, 6); open cells for planned machines below.
    /// </summary>
    private const string Water = """
        VV....cccc....cccc..
        VV....cccc....cccc..
        ......cccc....cccc..
        ......cccc....cccc..
        TTTT................
        TTTT................
        TTTT................
        TTTT................
        """;

    private static readonly Placement Cistern = new("machine", "cistern");
    private static readonly Placement Vent = new("machine", "vent");

    private static Scenario Piped() => new Scenario(Water, cells: true).Do(new SetCreative(true));
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    /// <summary>Plans <paramref name="placement"/> at (<paramref name="x"/>, <paramref name="y"/>) and works it up to its last tick, which it leaves undone.</summary>
    private static int PlannedAllButLast(Scenario w, int x, int y, Placement placement)
    {
        w.Do(new PlacePlan(w.At(x, y), placement)).Run();
        int plan = w.M.Building.Plans[^1].Id;
        for (int i = 1; i < Content.Machines.Get(placement.Kind).BuildTicks; i++) w.Do(new Work(plan)).Run();
        return plan;
    }

    [Fact]
    public void PipesWaterOnlyToWaterAndAirOnlyToAir()
    {
        var w = Piped();
        w.Do(new LayPipe(w.At(9, 2), w.At(0, 0))).Do(new LayPipe(w.At(0, 6), w.At(6, 2))).Do(new LayPipe(w.At(9, 2), w.At(14, 2))).Run();
        Assert.Equal(["pipes.mixedFluid", "pipes.mixedFluid"], Reasons(w));
        Assert.Equal([new PipeSpan(w.At(9, 2), w.At(14, 2), false)], w.M.Pipes.Spans);
        Assert.Equal(Content.Fluids.Water, w.M.Pipes.FluidAt(w.At(9, 2)));
        // A joint on the water line takes water only.
        w.Do(new LayPipe(w.At(17, 2), w.At(17, 6))).Do(new LayPipe(w.At(17, 6), w.At(9, 6))).Do(new LayPipe(w.At(9, 6), w.At(0, 6))).Run();
        Assert.Equal(["pipes.mixedFluid", "pipes.mixedFluid", "pipes.mixedFluid"], Reasons(w));
        Assert.Equal(Content.Fluids.Water, w.M.Pipes.FluidAt(w.At(9, 6)));
    }

    [Fact]
    public void RefusesAMachineOnALineOfAnotherFluidWhenPlannedAndAtItsLastWork()
    {
        var w = Piped();
        // A cistern whose input would land on a joint of the tank's air line.
        w.Do(new LayPipe(w.At(0, 6), w.At(10, 6))).Do(new PlacePlan(w.At(10, 4), Cistern)).Run();
        Assert.Equal(["pipes.mixedFluid"], Reasons(w));
        Assert.Empty(w.M.Building.Plans);
    }

    [Fact]
    public void AVentPlannedBeforeAWaterPipeWaitsAtItsLastWork()
    {
        var w = Piped();
        int plan = PlannedAllButLast(w, 16, 6, Vent);
        w.Do(new LayPipe(w.At(17, 2), w.At(16, 6))).Do(new Work(plan)).Run();
        Assert.Equal(["pipes.mixedFluid"], Reasons(w));
        Assert.Null(w.M.Grid.MachineAt(w.At(16, 6)));
    }

    [Fact]
    public void FindsTheLinesAgainWithinATick()
    {
        // A pipe laid from a cistern to a joint, then a vent finished on that joint: refused, the line now water.
        var w = Piped();
        w.Do(new LayPipe(w.At(16, 6), w.At(12, 6))).Run();
        int vent = PlannedAllButLast(w, 16, 6, Vent);
        Assert.Null(w.M.Pipes.FluidAt(w.At(16, 6)));
        w.Do(new LayPipe(w.At(17, 2), w.At(16, 6))).Do(new Work(vent)).Run();
        Assert.Equal(["pipes.mixedFluid"], Reasons(w));

        // A cistern finished on a bare joint, then a pipe from the tank to the joint's line: refused, the line now water.
        var v = Piped();
        v.Do(new LayPipe(v.At(6, 6), v.At(10, 6))).Run();
        int cistern = PlannedAllButLast(v, 10, 4, Cistern);
        Assert.Null(v.M.Pipes.FluidAt(v.At(6, 6)));
        v.Do(new Work(cistern)).Do(new LayPipe(v.At(0, 6), v.At(6, 6))).Run();
        Assert.Equal(["pipes.mixedFluid"], Reasons(v));
        Assert.Equal("cistern", v.M.Grid.MachineAt(v.At(10, 6))!.Kind);

        // A water pipe cut, then a vent finished on the joint it freed: built.
        var u = Piped();
        int freed = PlannedAllButLast(u, 16, 6, Vent);
        u.Do(new LayPipe(u.At(17, 2), u.At(16, 6))).Run();
        Assert.Equal(Content.Fluids.Water, u.M.Pipes.FluidAt(u.At(16, 6)));
        u.Do(new CutPipe(u.At(17, 2), u.At(16, 6))).Do(new Work(freed)).Run();
        Assert.Empty(u.Refusals);
        Assert.Equal("vent", u.M.Grid.MachineAt(u.At(16, 6))!.Kind);
    }

    /// <summary>
    /// A sealed room: a vent over a vaporizer, two cisterns beside them; under the hull, past a row of space, a tank and a
    /// solar panel (liquid-oxygen). Drawn a character a 16 px cell.
    /// </summary>
    private const string Room = """
        #############
        #V..........#
        #GG...cc.cc.#
        #GG...cc.cc.#
        #############

         TTPPP
         TT
        """;

    [Fact]
    public void LeavesTheAirAsItWas()
    {
        Scenario Aired(bool water)
        {
            var w = new Scenario(AirTesting.OneThick(Room)).Do(new SetCreative(true));
            int Cell(int x, int y) => y * w.M.Grid.Width + x;
            // The tank's port up to the vaporizer's input, its output on to the vent's port; the panel cabled to the vaporizer.
            w.Do(new LayPipe(Cell(2, 14), Cell(2, 6))).Do(new LayPipe(Cell(5, 4), Cell(2, 2)));
            w.Do(new Power.Lay(Cell(6, 12), Cell(2, 4)));
            int tank = w.M.Grid.MachinesOf("tank")[0].Id;
            for (int i = 0; i < 3; i++) w.Do(new LoadCanister(tank));
            // The first cistern's output to the second's input.
            if (water) w.Do(new LayPipe(Cell(15, 6), Cell(18, 6)));
            return w.Run(400);
        }
        var (dry, wet) = (Aired(false), Aired(true));
        Assert.Empty(dry.Refusals);
        Assert.Empty(wet.Refusals);
        // The liquid, the gas and the water.
        Assert.Equal(3, wet.M.Pipes.Networks.Count);
        string AirOf(Scenario w) => JsonNode.Parse(w.World.Save())!["slices"]!["atmosphere"]!.ToJsonString();
        Assert.NotEmpty(dry.M.Atmosphere.AiredCells);
        Assert.Equal(AirOf(dry), AirOf(wet));
        Assert.True(dry.M.Atmosphere.OxygenIn(dry.M.Grid.MachinesOf("tank")[0].Id) > 0);
    }
}
