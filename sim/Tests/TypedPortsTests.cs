using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Pipes;
using Xunit;
using Way = Stillwake.Content.Way;

namespace Stillwake.Sim.Tests;

/// <summary>Typed ports (2026-10-29-typed-ports): a port takes or gives one fluid, and pipes and machines keep to it.</summary>
public class TypedPortsTests
{
    /// <summary>
    /// Ports: the top vent's (0, 0) takes, the tank's (0, 6) and the generator's (12, 6) give, the bottom vents' (0, 10) and
    /// (12, 10) take; open cells between.
    /// </summary>
    private const string Ship = """
        VV......................
        VV......................
        ........................
        ........................
        TTTT........GGGG........
        TTTT........GGGG........
        TTTT........GGGG........
        TTTT........GGGG........
        ........................
        ........................
        VV..........VV..........
        VV..........VV..........
        """;

    /// <summary>The ship with no generator and one vent: room for machines planned onto the pipes.</summary>
    private const string Bare = """
        VV......................
        VV......................
        ........................
        ........................
        TTTT....................
        TTTT....................
        TTTT....................
        TTTT....................
        """;

    private static readonly Placement Generator = new("machine", "generator");
    private static readonly Placement Vent = new("machine", "vent");

    private static Scenario Piped(string map = Ship) => new Scenario(map, cells: true).GiveItems(0, ("tube", 20));
    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();

    /// <summary>Works plan <paramref name="plan"/> in creative mode up to its last tick, which it leaves undone.</summary>
    private static Scenario WorkAllButLast(Scenario w, int plan, string kind)
    {
        for (int i = 1; i < Content.Machines.Get(kind).BuildTicks; i++) w.Do(new Work(plan)).Run();
        return w;
    }

    [Fact]
    public void LaysAPipeFromAnOutputToAnInputAndRefusesOneJoiningTwoOutputsOrTwoInputs()
    {
        var w = Piped();
        w.Do(new LayPipe(w.At(0, 6), w.At(0, 0))).Do(new LayPipe(w.At(0, 6), w.At(12, 6))).Do(new LayPipe(w.At(0, 10), w.At(12, 10))).Run();
        Assert.Equal(["pipes.bothGive", "pipes.bothTake"], Reasons(w));
        Assert.Equal([new PipeSpan(w.At(0, 0), w.At(0, 6), true)], w.M.Pipes.Spans);
    }

    [Fact]
    public void JoinsATankAGeneratorAndAVentThroughAJointTwoOutputsOnOneLine()
    {
        var w = Piped();
        int joint = w.At(6, 8);
        w.Do(new LayPipe(w.At(0, 6), joint)).Do(new LayPipe(w.At(12, 6), joint)).Do(new LayPipe(joint, w.At(0, 10))).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(3, Assert.Single(w.M.Pipes.Networks).Machines.Count);
        Assert.Equal(Content.Fluids.Air, w.M.Pipes.FluidAt(joint));
    }

    [Fact]
    public void LaysAVentToAVentOnlyThroughAJoint()
    {
        var w = Piped();
        int joint = w.At(5, 5);
        w.Do(new LayPipe(w.At(0, 0), w.At(0, 10))).Do(new LayPipe(w.At(0, 0), joint)).Do(new LayPipe(joint, w.At(0, 10))).Run();
        Assert.Equal(["pipes.bothTake"], Reasons(w));
        Assert.Equal([new PipeSpan(w.At(0, 0), joint, true), new PipeSpan(joint, w.At(0, 10), true)], w.M.Pipes.Spans);
        Assert.Equal(2, Assert.Single(w.M.Pipes.Networks).Machines.Count);
    }

    [Fact]
    public void AJointCarriesNoFluidTillAPortIsOnItsLineAndAValveShutLeavesTheLineWhole()
    {
        var w = Piped().GiveItems(0, ("gear", 1));
        int joint = w.At(6, 2), valve = w.At(4, 2);
        w.Do(new LayPipe(joint, valve)).Run();
        Assert.Null(w.M.Pipes.FluidAt(joint));
        Assert.Null(w.M.Pipes.FluidProblem(joint, w.At(0, 0)));
        w.Do(new LayPipe(valve, w.At(0, 0))).Do(new MakeValve(valve, true)).Do(new SetValve(valve, false)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(Content.Fluids.Air, w.M.Pipes.FluidAt(joint));
        Assert.Equal("pipes.bothTake", w.M.Pipes.FluidProblem(w.At(0, 0), w.At(0, 10)));
        Assert.Null(w.M.Pipes.FluidProblem(joint, w.At(0, 10)));
    }

    [Fact]
    public void KeepsADrawnSpanBetweenTwoOutputsThroughALoad()
    {
        // The tank's and the generator's ports joined straight by a pipe drawn under them: no longer laid, still kept.
        string[] rows = [.. Ship.Split('\n').Select(r => r.Trim())];
        string[] under = ["", "", "", "", "", "", "-------------"];
        var w = new Scenario(Layout.Parse(rows, under));
        Assert.Equal([new PipeSpan(w.At(0, 6), w.At(12, 6), true)], w.M.Pipes.Spans);
        w.Reload().Run();
        Assert.Equal([new PipeSpan(w.At(0, 6), w.At(12, 6), true)], w.M.Pipes.Spans);
        // Laid again after the load: already there, before the ports are asked.
        w.Do(new LayPipe(w.At(0, 6), w.At(12, 6))).Run();
        Assert.Equal(["pipes.twice"], Reasons(w));
        Assert.Equal(2, Assert.Single(w.M.Pipes.Networks).Machines.Count);
    }

    [Fact]
    public void RefusesPlanningAMachineWhosePortWouldLandOnANodeSpannedToAPortOfItsWay()
    {
        var w = Piped(Bare);
        w.Do(new LayPipe(w.At(0, 6), w.At(12, 6))).Do(new LayPipe(w.At(0, 0), w.At(8, 0))).Run();
        w.Do(new PlacePlan(w.At(12, 4), Generator)).Do(new PlacePlan(w.At(8, 0), Vent)).Run();
        Assert.Equal(["pipes.bothGive", "pipes.bothTake"], Reasons(w));
        Assert.Empty(w.M.Building.Plans);
        // Built at once, the same.
        w.Do(new SetInstant(true)).Do(new PlacePlan(w.At(12, 4), Generator)).Run();
        Assert.Equal(["pipes.bothGive", "pipes.bothTake", "pipes.bothGive"], Reasons(w));
        Assert.DoesNotContain(w.M.Grid.Machines, m => m.Kind == "generator");
    }

    [Fact]
    public void AMachinePlannedBeforeThePipeWaitsAtItsLastTickTillThePipeIsCut()
    {
        var w = Piped(Bare).Do(new Items.SetCreative(true));
        w.Do(new PlacePlan(w.At(10, 0), Vent)).Run();
        int plan = w.M.Building.Plans[0].Id;
        w.Do(new LayPipe(w.At(0, 0), w.At(10, 0))).Run();
        WorkAllButLast(w, plan, "vent").Do(new Work(plan)).Run();
        Assert.Equal(["pipes.bothTake"], Reasons(w));
        Assert.Null(w.M.Grid.MachineAt(w.At(10, 0)));
        w.Do(new CutPipe(w.At(0, 0), w.At(10, 0))).Do(new Work(plan)).Run();
        Assert.Equal("vent", w.M.Grid.MachineAt(w.At(10, 0))!.Kind);
    }

    [Fact]
    public void SeesWhatEarlierCommandsOfItsTickDid()
    {
        // A generator finished, then a pipe from the tank to its port: refused on the tick it is built.
        var w = Piped(Bare).Do(new Items.SetCreative(true));
        w.Do(new PlacePlan(w.At(12, 4), Generator)).Run();
        int plan = w.M.Building.Plans[0].Id;
        WorkAllButLast(w, plan, "generator").Do(new Work(plan)).Do(new LayPipe(w.At(0, 6), w.At(12, 6))).Run();
        Assert.Equal(["pipes.bothGive"], Reasons(w));
        Assert.Equal("generator", w.M.Grid.MachineAt(w.At(12, 6))!.Kind);

        // A pipe from the tank to a joint, then a generator finished on it: refused on the tick the pipe is laid.
        var v = Piped(Bare).Do(new Items.SetCreative(true));
        v.Do(new PlacePlan(v.At(12, 4), Generator)).Run();
        int other = v.M.Building.Plans[0].Id;
        WorkAllButLast(v, other, "generator").Do(new LayPipe(v.At(0, 6), v.At(12, 6))).Do(new Work(other)).Run();
        Assert.Equal(["pipes.bothGive"], Reasons(v));
        Assert.Null(v.M.Grid.MachineAt(v.At(12, 6)));
    }

    [Fact]
    public void AsksThePortsAfterTheLengthAndBeforeTheItems()
    {
        // Two inputs too far apart: too long first. Two outputs by a hero with no tubes: the ports first.
        var w = new Scenario(Ship, cells: true);
        w.Do(new LayPipe(w.At(0, 0), w.At(12, 10))).Do(new LayPipe(w.At(0, 6), w.At(12, 6))).Run();
        Assert.Equal(["pipes.tooLong", "pipes.bothGive"], Reasons(w));
        Assert.Empty(w.M.Pipes.Spans);
    }

    [Fact]
    public void BuildsAMachineWhosePortsFitThePipesThere()
    {
        // A vent on a joint piped from the tank, a generator on one piped from the vent: each gives to a port that takes.
        foreach (bool instant in new[] { false, true })
        {
            var w = Piped(Bare).Do(new Items.SetCreative(true)).Do(new SetInstant(instant));
            w.Do(new LayPipe(w.At(0, 6), w.At(8, 6))).Do(new LayPipe(w.At(0, 0), w.At(8, 2))).Run();
            w.Do(new PlacePlan(w.At(8, 6), Vent)).Do(new PlacePlan(w.At(8, 0), Generator)).Run();
            foreach (var plan in w.M.Building.Plans.ToList())
                for (int i = 0; i < Content.Machines.Get(plan.Place!.Kind).BuildTicks; i++) w.Do(new Work(plan.Id)).Run();
            Assert.Empty(w.Refusals);
            Assert.Equal("vent", w.M.Grid.MachineAt(w.At(8, 6))!.Kind);
            Assert.Equal("generator", w.M.Grid.MachineAt(w.At(8, 2))!.Kind);
            Assert.All(w.M.Pipes.Networks, n => Assert.Equal(2, n.Machines.Count));
        }
    }

    [Fact]
    public void LaysAPipeToAVentFinishedEarlierInItsTick()
    {
        var w = Piped().Do(new Items.SetCreative(true));
        w.Do(new PlacePlan(w.At(6, 6), Vent)).Run();
        int plan = w.M.Building.Plans[0].Id;
        WorkAllButLast(w, plan, "vent").Do(new Work(plan)).Do(new LayPipe(w.At(12, 6), w.At(6, 6))).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(2, Assert.Single(w.M.Pipes.Networks).Machines.Count);
    }

    /// <summary>What breaks the ports' rules on a surface: a span joining two ports of one way, a network of two fluids.</summary>
    private static string? Broken(Registry m)
    {
        foreach (var s in m.Pipes.Spans)
            if (m.Grid.PortAt(s.A) is { } a && m.Grid.PortAt(s.B) is { } b && a.Way == b.Way)
                return $"span {s.A}–{s.B} joins two ports that {(a.Way == Way.Out ? "give" : "take")}";
        foreach (var n in m.Pipes.Networks)
            if (n.Cells.Select(c => m.Grid.PortAt(c)?.Fluid).OfType<string>().Distinct().Count() > 1)
                return $"network {n.Id} holds two fluids";
        return null;
    }

    /// <summary>Where the property's pipes end: the ship's ports, and joints, four of them where a machine planned puts its port.</summary>
    private static readonly (int X, int Y)[] Nodes = [(0, 0), (0, 6), (12, 6), (0, 10), (12, 10), (6, 8), (5, 5), (9, 2), (16, 2), (16, 8), (20, 0), (18, 10)];

    /// <summary>Machines the property plans, each at a top-left that puts its port on one of those joints.</summary>
    private static readonly (string Kind, int X, int Y)[] Spots = [("generator", 16, 0), ("tank", 16, 6), ("vent", 20, 0), ("vent", 18, 10)];

    [Fact]
    public void TheWreckJoinsNoTwoPortsOfOneWayNorTwoFluids()
    {
        var m = new Registry();
        World.Create(1, Registry.Wreck, m.All);
        Assert.NotEmpty(m.Pipes.Spans);
        Assert.Null(Broken(m));
    }

    [Property(MaxTest = 60)]
    public Property KeepsEveryPortToItsWayAndFluidWhateverIsLaidCutBuiltOrTakenApart()
    {
        var step = from op in Gen.Choose(0, 9) from i in Gen.Choose(0, Nodes.Length - 1) from j in Gen.Choose(0, Nodes.Length - 1) select (op, i, j);
        var steps = from n in Gen.Choose(1, 40) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = Piped().Do(new Items.SetCreative(true)).Do(new SetInstant(true)).Run();
            int At((int X, int Y) c) => w.At(c.X, c.Y);
            foreach (var (op, i, j) in all)
            {
                var spot = Spots[i % Spots.Length];
                w.Do(op switch
                {
                    < 6 => new LayPipe(At(Nodes[i]), At(Nodes[j])),
                    6 when w.M.Pipes.Spans.Count > 0 => new CutPipe(w.M.Pipes.Spans[i % w.M.Pipes.Spans.Count].A, w.M.Pipes.Spans[i % w.M.Pipes.Spans.Count].B),
                    7 or 8 => new PlacePlan(w.At(spot.X, spot.Y), new Placement("machine", spot.Kind)),
                    _ => new ClearAt(At(Nodes[i]), "machine"),
                }).Run();
                if (Broken(w.M) is { } broken) return false.Label(broken);
            }
            return true.ToProperty();
        });
    }
}
