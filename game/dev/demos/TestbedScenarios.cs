using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>The testbed (<see cref="Testbed"/>), and the base of the scenarios played on it. After <c>--store</c>, which plays its own world.</summary>
[Dev("--testbed", "a ready ship with every machine at work (Testbed)", Order = 1)]
public class TestbedScenario : DevScenario
{
    public override Layout? Map => Testbed.Map;

    public override World? Create(IReadOnlyList<Module> modules) => Testbed.Create(modules);
}

[Dev("--jetpack", "the testbed's hero with a jetpack put on, flown out of the airlock (JetpackDemo)", Order = 1)]
public sealed class JetpackScenario : TestbedScenario
{
    public override World Seed(World world, DevSeed seed) => JetpackDemo.Seed(world, seed.Modules, Args.Has("--caped"));

    public override HeroAutopilot? Script => HeroAutopilot.Jetpack();

    public override void Attach(DevContext c)
    {
        if (c.Items != null) c.Add(new JetpackDemo(c.Items));
    }
}

[Dev("--charge", "the testbed's charger filling a battery (ChargeDemo)", Order = 1)]
public sealed class ChargeScenario : TestbedScenario
{
    public override World Seed(World world, DevSeed seed) => ChargeDemo.Seed(world, seed.Modules);

    public override Func<Registry, int>? Start => ChargeDemo.StartOf;

    public override void Attach(DevContext c)
    {
        if (c.Items != null) c.Add(new ChargeDemo(c.Items, c.Ui, c.M));
    }
}

[Dev("--dust", "the testbed's hero sprinting and jumping: its boots' dust and landings (HeroAutopilot.Dust, 0075)", Order = 1)]
public sealed class DustScenario : TestbedScenario
{
    public override HeroAutopilot? Script => HeroAutopilot.Dust();
}

/// <summary>
/// The testbed with every machine that makes power broken, as a flare leaves them: each shows its wrench, and every machine
/// drawing power its red bolt, for a look at the signs over machines (the ink-signs ADR).
/// </summary>
[Dev("--signs", "the testbed with everything that makes power broken: wrenches over them, red bolts over what draws", Order = 1)]
public sealed class SignsScenario : TestbedScenario
{
    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        var broken = seed.M.Grid.Machines.Where(m => Content.Machines.Get(m.Kind).Makes != null).Select(m => m.Id).Order();
        root["slices"]!["power"]!["broken"] = new JsonArray([.. broken.Select(id => (JsonNode)id)]);
        return World.Load(root.ToJsonString(), seed.Modules());
    }
}
