using System;
using Stillwake.Net;
using Stillwake.Sim;

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

[Dev("--layer-demo", "plan|remove|fade|axe|aim|strip|pipe|valve: the plan's layers and hand tools by script (LayerDemo, 0107–0109)", Shape = DevShape.Next)]
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
