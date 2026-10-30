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

    /// <summary>
    /// What breaks the ports' rules on a surface: a span joining two ports of one way, a line (every valve counted open)
    /// holding two fluids.
    /// </summary>
    private static string? Broken(Registry m)
    {
        foreach (var s in m.Pipes.Spans)
            if (m.Grid.PortAt(s.A) is { } a && m.Grid.PortAt(s.B) is { } b && a.Way == b.Way)
                return $"span {s.A}–{s.B} joins two ports that {(a.Way == Way.Out ? "give" : "take")}";
        var line = new Dictionary<int, int>();
        int Root(int cell)
        {
            while (line.TryGetValue(cell, out int up) && up != cell) cell = up;
            return cell;
        }
        foreach (var s in m.Pipes.Spans)
        {
            var (a, b) = (Root(s.A), Root(s.B));
            line[a] = line.GetValueOrDefault(a, a);
            line[b] = a;
        }
        foreach (var group in m.Pipes.Spans.SelectMany(s => new[] { s.A, s.B }).Distinct().GroupBy(Root))
            if (group.Select(c => m.Grid.PortAt(c)?.Fluid).OfType<string>().Distinct().Count() > 1)
                return $"the line through {group.Key} holds two fluids";
        return null;
    }

    /// <summary>
    /// A cell a character: air's ports as on <see cref="Ship"/> but the bottom right vent; cistern C at (24, 4), its input
    /// (24, 6) and output (27, 6); cistern D at (28, 8), its input (28, 10) and output (31, 10).
    /// </summary>
    private const string Mixed = """
        VV..............................
        VV..............................
        ................................
        ................................
        TTTT........GGGG........cccc....
        TTTT........GGGG........cccc....
        TTTT........GGGG........cccc....
        TTTT........GGGG........cccc....
        ............................cccc
        ............................cccc
        VV..........VV..............cccc
        VV..........VV..............cccc
        """;

    /// <summary>Where the mixed ship's pipes end: its ports, and joints, among them where a machine planned puts a port.</summary>
    private static readonly (int X, int Y)[] Nodes =
    [
        (0, 0), (0, 6), (12, 6), (0, 10), (12, 10), (24, 6), (27, 6), (28, 10), (31, 10),
        (6, 8), (5, 5), (9, 2), (22, 6), (16, 2), (16, 8), (20, 0), (18, 10), (26, 2), (29, 2), (20, 10), (23, 10),
    ];

    /// <summary>Machines planned on the mixed ship, each at a top-left that puts its ports on joints among <see cref="Nodes"/>.</summary>
    private static readonly (string Kind, int X, int Y)[] Spots =
        [("generator", 16, 0), ("tank", 16, 6), ("vent", 20, 0), ("vent", 18, 10), ("cistern", 26, 0), ("cistern", 20, 8)];

    /// <summary>One random step on the mixed ship: a span laid or cut, a machine planned or taken apart, a valve made or set.</summary>
    private static Command StepOn(Scenario w, int op, int i, int j)
    {
        int At((int X, int Y) c) => w.At(c.X, c.Y);
        var spot = Spots[i % Spots.Length];
        var spans = w.M.Pipes.Spans;
        return op switch
        {
            < 5 => new LayPipe(At(Nodes[i]), At(Nodes[j])),
            5 when spans.Count > 0 => new CutPipe(spans[i % spans.Count].A, spans[i % spans.Count].B),
            6 or 7 => new PlacePlan(w.At(spot.X, spot.Y), new Placement("machine", spot.Kind)),
            8 => new ClearAt(At(Nodes[i]), "machine"),
            9 => new MakeValve(At(Nodes[i]), j % 3 != 0),
            _ => new SetValve(At(Nodes[i]), j % 2 == 0),
        };
    }

    private static Scenario MixedShip() =>
        new Scenario(Mixed, cells: true).GiveItems(0, ("tube", 20)).Do(new Items.SetCreative(true)).Do(new SetInstant(true)).Run();

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
        var step = from op in Gen.Choose(0, 10) from i in Gen.Choose(0, Nodes.Length - 1) from j in Gen.Choose(0, Nodes.Length - 1) select (op, i, j);
        var steps = from n in Gen.Choose(1, 40) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = MixedShip();
            foreach (var (op, i, j) in all)
            {
                w.Do(StepOn(w, op, i, j)).Run();
                if (Broken(w.M) is { } broken) return false.Label(broken);
            }
            return true.ToProperty();
        });
    }

    /// <summary>
    /// Lines found when asked are what a load finds: two mixed ships fed the same random steps, one asked of fluids and seats
    /// between ticks and now and then loaded from its save, hash alike every tick and refuse alike; and the steps reach
    /// <c>pipes.mixedFluid</c> from both a pipe and a machine.
    /// </summary>
    [Fact]
    public void AsksAndLoadsWithoutChangingWhatItRefuses()
    {
        int fromPipe = 0, fromMachine = 0;
        for (uint seed = 1; seed <= 12; seed++)
        {
            var rng = new Rng(new RngState { S = seed });
            int Below(int n) => (int)rng.Below((ulong)n);
            var (plain, asked) = (MixedShip(), MixedShip());
            for (int tick = 0; tick < 60; tick++)
            {
                for (int k = 0; k < 4; k++)
                {
                    var (node, spot) = (Nodes[Below(Nodes.Length)], Spots[Below(Spots.Length)]);
                    asked.M.Pipes.FluidAt(asked.At(node.X, node.Y));
                    asked.M.Pipes.SeatProblem(spot.Kind, asked.At(spot.X, spot.Y));
                }
                if (Below(8) == 0) asked.Reload();
                var (op, i, j) = (Below(11), Below(Nodes.Length), Below(Nodes.Length));
                var command = StepOn(plain, op, i, j);
                var (was, wasAsked) = (plain.Refusals.Count, asked.Refusals.Count);
                plain.Do(command).Run();
                asked.Do(command).Run();
                Assert.True(plain.World.Hash() == asked.World.Hash(), $"seed {seed}, tick {tick}: the asked world differs");
                Assert.Equal(plain.Refusals.Skip(was).Select(r => r.Reason), asked.Refusals.Skip(wasAsked).Select(r => r.Reason));
                foreach (var r in plain.Refusals.Skip(was).Where(r => r.Reason == "pipes.mixedFluid"))
                    if (r.Command is LayPipe) fromPipe++;
                    else fromMachine++;
            }
        }
        Assert.True(fromPipe > 0 && fromMachine > 0, $"pipes.mixedFluid from a pipe {fromPipe} times, from a machine {fromMachine}");
    }
}
