using System;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Building;

namespace Stillwake.Game.Dev;

// Scripts that work the plan's hands: each takes the pointer as it is attached and plays every frame.

[Dev("--plan-demo", "plays the plan, cables and overlays by a script (PlanDemo)")]
public sealed class PlanScenario : DevScenario
{
    private PlanDemo? _demo;

    public override void Attach(DevContext c)
    {
        _demo = new PlanDemo(c.Hands, c.Layout);
        _demo.Start();
    }

    public override void Frame(Client client, Registry m, Action<string> log) => _demo?.Frame(client, m.Grid, log);
}

[Dev("--mark", "machine|structure|back: everything of that layer marked, its dashes along its pictures, over them", Shape = DevShape.Equals)]
public sealed class MarkScenario : DevScenario
{
    private bool _marked;

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (_marked) return;
        _marked = true;
        string layer = Args.Value("--mark")!;
        if (layer == "machine")
            foreach (var machine in m.Grid.Machines) client.Send(new Deconstruct(machine.Cell, layer));
        else
            for (int c = 0; c < m.Grid.Width * m.Grid.Height; c++)
                if ((layer == "back" ? m.Grid.BackLayer[c] : m.Grid.StructureLayer[c]) != 0) client.Send(new Deconstruct(c, layer));
    }
}

[Dev("--layer-demo", "plan|remove|fade|axe|aim|strip|pipe|valve: the plan's layers and modes by script (LayerDemo, 0107–0109)", Shape = DevShape.Next)]
public sealed class LayerScenario : DevScenario
{
    private LayerDemo? _demo;

    // With --poses too, the hero's tools are seeded once, by its scenario.
    public override World Seed(World world, DevSeed seed) => Args.Has("--poses") ? world : GroundDemo.Seed(world, seed.Modules);

    public override void Attach(DevContext c)
    {
        _demo = new LayerDemo(c.Hands, Args.Arg("--layer-demo")!);
        _demo.Start();
    }

    public override void Frame(Client client, Registry m, Action<string> log) => _demo?.Frame(client, m, log);
}

[Dev("--shimmer-demo", "with --testbed --near=furnace, aims at the furnace, plans, marks and welds over it (ShimmerDemo)")]
public sealed class ShimmerScenario : DevScenario
{
    private ShimmerDemo? _demo;

    public override void Attach(DevContext c)
    {
        _demo = new ShimmerDemo(c.Hands);
        _demo.Start();
    }

    public override void Frame(Client client, Registry m, Action<string> log) => _demo?.Frame(client, m.Grid, log);
}

[Dev("--driver-demo", "square|cell|flat|short|hover|nothing|station: the power driver held on a run of wall plans in a lit room (DriverDemo, power-driver)", Shape = DevShape.Equals)]
public sealed class DriverScenario : DevScenario
{
    private DriverDemo? _demo;

    public override Layout? Map => DriverDemo.Map;

    public override World Seed(World world, DevSeed seed) => DriverDemo.Seed(world, seed.Modules, Args.Value("--driver-demo") != "flat", Args.Value("--driver-demo") == "short" ? 2 : DriverDemo.Plates);

    public override void Attach(DevContext c)
    {
        _demo = new DriverDemo(c.Hands, Args.Value("--driver-demo")!);
        _demo.Start();
    }

    public override void Frame(Client client, Registry m, Action<string> log) => _demo?.Frame(client, m, log);
}
