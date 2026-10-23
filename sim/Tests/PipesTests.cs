using Stillwake.Sim.Grid;
using Stillwake.Sim.Pipes;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Pipes as spans (0109): laid and cut with the wrench, joints and valves, the networks they join, and maps' pipes as spans.</summary>
public class PipesTests
{
    /// <summary>Three vents, two cells square, their ports their top-left cells: (0, 0), (12, 4) and (0, 8); open cells between.</summary>
    private const string Vents = """
        VV..............
        VV..............
        ................
        ................
        ............VV..
        ............VV..
        ................
        ................
        VV..............
        VV..............
        """;

    private static readonly Placement Vent = new("machine", "vent");

    private static Scenario World() => new Scenario(Vents, cells: true).GiveItems(0, ("tube", 20), ("gear", 4));
    private static int Tubes(Scenario w) => w.M.Items.Totals(0).Where(t => t.Item == "tube").Sum(t => t.Count);
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int VentAt(Scenario w, int cell) => w.M.Grid.MachineAt(cell)!.Id;

    [Fact]
    public void MeasuresASpanByItsRootRoundedUpAndPricesItATubeAStartedFourCells()
    {
        Assert.Equal(5, PipesModule.LengthOf(16, 0, 4 * 16 + 3));
        Assert.Equal(2, PipesModule.LengthOf(16, 0, 17));
        Assert.Equal(12, PipesModule.LengthOf(16, 0, 12));
        Assert.Equal(13, PipesModule.LengthOf(16, 0, 12 * 16 + 5));
        Assert.Equal([("tube", 1)], PipesModule.CostOf(1));
        Assert.Equal([("tube", 1)], PipesModule.CostOf(4));
        Assert.Equal([("tube", 2)], PipesModule.CostOf(5));
        Assert.Equal([("tube", 3)], PipesModule.CostOf(Content.Piping.MaxSpan));
    }

    [Fact]
    public void LaysAStraightSpanAndADiagonalOneThroughAJointJoiningTwoVentsPaidByLength()
    {
        var w = World();
        int a = w.At(0, 0), joint = w.At(6, 0), b = w.At(12, 4);
        w.Do(new LayPipe(a, joint)).Run();
        Assert.Equal(20 - 2, Tubes(w));
        Assert.Empty(w.M.Pipes.NetworksOf(VentAt(w, b)));
        // Six across and four down: the root of 52 rounded up, 8 cells, two tubes.
        w.Do(new LayPipe(b, joint)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(20 - 4, Tubes(w));
        Assert.Equal([new PipeSpan(a, joint, true), new PipeSpan(joint, b, true)], w.M.Pipes.Spans);
        var network = Assert.Single(w.M.Pipes.Networks);
        Assert.Equal(a, network.Id);
        Assert.Equal(new[] { a, joint, b }, network.Cells);
        Assert.Equal(new[] { VentAt(w, a), VentAt(w, b) }, network.Machines);
        Assert.Equal(a, w.M.Pipes.NetworkAt(joint));
        Assert.Null(w.M.Pipes.NetworkAt(w.At(3, 0)));
    }

    [Fact]
    public void BranchesAtAJointWithManySpansAndAClosedValveShutsItsBranchOff()
    {
        var w = World();
        int a = w.At(0, 0), joint = w.At(6, 4), valve = w.At(3, 7), b = w.At(12, 4), c = w.At(0, 8);
        w.Do(new LayPipe(a, joint)).Do(new LayPipe(joint, b)).Do(new LayPipe(joint, valve)).Do(new LayPipe(valve, c)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(new[] { VentAt(w, a), VentAt(w, b), VentAt(w, c) }, Assert.Single(w.M.Pipes.Networks).Machines);
        w.Do(new MakeValve(valve, true)).Run();
        Assert.Equal(new Valve(valve, true, true), w.M.Pipes.ValveAt(valve));
        Assert.Single(w.M.Pipes.Networks);
        // Shut, the branch past it is a network of its own, the valve in none.
        w.Do(new SetValve(valve, false)).Run();
        Assert.Equal(new[] { VentAt(w, a), VentAt(w, b) }, w.M.Pipes.Network(a)!.Machines);
        Assert.Equal(new[] { VentAt(w, c) }, w.M.Pipes.Network(c)!.Machines);
        Assert.Null(w.M.Pipes.NetworkAt(valve));
        int version = w.M.Pipes.Version;
        w.Do(new SetValve(valve, false)).Run();
        Assert.Equal(version, w.M.Pipes.Version);
        w.Do(new SetValve(valve, true)).Run();
        Assert.Single(w.M.Pipes.Networks);
        w.Reload();
        Assert.Equal(new Valve(valve, true, true), w.M.Pipes.ValveAt(valve));
        Assert.Equal(4, w.M.Pipes.Spans.Count);
    }

    [Fact]
    public void CuttingRefundsItsTubesAndAValveLeftOnNoSpanGoesWithItRefundedToo()
    {
        var w = World();
        int a = w.At(0, 0), joint = w.At(6, 0);
        w.Do(new LayPipe(a, joint)).Do(new MakeValve(joint, true)).Run();
        Assert.Equal((18 - 1, 3), (Tubes(w), w.M.Items.Totals(0).Single(t => t.Item == "gear").Count));
        w.Do(new MakeValve(joint, false)).Run();
        Assert.Null(w.M.Pipes.ValveAt(joint));
        Assert.Equal(18, Tubes(w));
        w.Do(new MakeValve(joint, true)).Do(new CutPipe(joint, a)).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Pipes.Spans);
        Assert.Empty(w.M.Pipes.Valves);
        Assert.Empty(w.M.Pipes.Networks);
        Assert.Equal(20, Tubes(w));
        Assert.Equal(4, w.M.Items.Totals(0).Single(t => t.Item == "gear").Count);
    }

    [Fact]
    public void RefusesEveryBadSpanAndCut()
    {
        var w = World();
        int a = w.At(0, 0);
        w.Do(new LayPipe(-1, a)).Do(new LayPipe(a, a)).Do(new LayPipe(a, w.At(13, 0)))
            .Do(new LayPipe(a, w.At(5, 0))).Do(new LayPipe(w.At(5, 0), a)).Do(new CutPipe(a, w.At(6, 0))).Run();
        Assert.Equal(["pipes.offGrid", "pipes.sameCell", "pipes.tooLong", "pipes.twice", "pipes.noPipe"], Reasons(w));
        // Nothing to pay with.
        var poor = new Scenario(Vents, cells: true);
        poor.Do(new LayPipe(a, w.At(5, 0))).Run();
        Assert.Equal("items.notEnough", Assert.Single(poor.Refusals).Reason);
        // Outside the build zone.
        var zoned = new Scenario(Sim.Layout.Parse(Vents) with { Zone = new Box(0, 0, 7, 9) }).GiveItems(0, ("tube", 5));
        zoned.Do(new LayPipe(a, w.At(9, 0))).Run();
        Assert.Equal("pipes.outsideZone", Assert.Single(zoned.Refusals).Reason);
        Assert.Equal("pipes.outsideZone", zoned.M.Pipes.LayProblem(0, a, w.At(9, 0)));
    }

    [Fact]
    public void RefusesAValveOffAJointTwiceOrUnpaidAndATurnOfNone()
    {
        var w = World();
        int a = w.At(0, 0), joint = w.At(6, 0);
        w.Do(new LayPipe(a, joint)).Run();
        w.Do(new MakeValve(w.At(3, 0), true)).Do(new MakeValve(a, true)).Do(new MakeValve(joint, false)).Do(new SetValve(joint, true))
            .Do(new MakeValve(joint, true)).Do(new MakeValve(joint, true)).Run();
        Assert.Equal(["pipes.noJoint", "pipes.noJoint", "pipes.noValve", "pipes.noValve", "pipes.already"], Reasons(w));
        var poor = new Scenario(Vents, cells: true).GiveItems(0, ("tube", 2));
        poor.Do(new LayPipe(a, joint)).Do(new MakeValve(joint, true)).Run();
        Assert.Equal("items.notEnough", Assert.Single(poor.Refusals).Reason);
    }

    [Fact]
    public void APortWhoseMachineIsTakenAwayStaysAJointAndAMachinePutBackJoinsAgain()
    {
        var w = World();
        int a = w.At(0, 0), b = w.At(12, 4);
        w.Do(new LayPipe(a, w.At(6, 0))).Do(new LayPipe(w.At(6, 0), b)).Run();
        int id = VentAt(w, b);
        w.Do(new ClearAt(b, "machine")).Run();
        Assert.Equal(new[] { VentAt(w, a) }, w.M.Pipes.Network(a)!.Machines);
        Assert.Equal(3, w.M.Pipes.Network(a)!.Cells.Count);
        w.Do(new Place(b, Vent)).Run();
        Assert.Equal(new[] { VentAt(w, a), id + 2 }, w.M.Pipes.Network(a)!.Machines);
    }

    [Fact]
    public void AMapsPipesBecomeSpansEndsCornersBranchesAndValvesJointsALongRunCutEveryTwelveCells()
    {
        // A run of 30 to a corner, 3 down to a branch; along the bottom a closed valve.
        var w = new Scenario("""
            ------------------------------.
            .............................-.
            .............................-.
            .....-----x--------------------
            """, cells: true);
        (int, int) Span(int x0, int y0, int x1, int y1) => (w.At(x0, y0), w.At(x1, y1));
        Assert.Equal(
            new[] { Span(0, 0, 12, 0), Span(12, 0, 24, 0), Span(24, 0, 29, 0), Span(29, 0, 29, 3), Span(5, 3, 10, 3), Span(10, 3, 22, 3), Span(22, 3, 29, 3), Span(29, 3, 30, 3) }
                .OrderBy(s => s.Item1).ThenBy(s => s.Item2),
            w.M.Pipes.Spans.Select(s => (s.A, s.B)));
        Assert.All(w.M.Pipes.Spans, s => Assert.True(s.Paid));
        Assert.Equal(new Valve(w.At(10, 3), false, true), Assert.Single(w.M.Pipes.Valves));
        // Shut, the valve cuts the bottom run's left end off.
        Assert.Equal(new[] { w.At(0, 0), w.At(5, 3) }, w.M.Pipes.Networks.Select(n => n.Id));
        Assert.Equal(new[] { w.At(5, 3) }, w.M.Pipes.Network(w.At(5, 3))!.Cells);
    }
}
