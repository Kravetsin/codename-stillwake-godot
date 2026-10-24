using Stillwake.Game.Features.Debris;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>Wreckage chopped and shot by script (<see cref="WreckDemo"/>), hosting too, so a guest sees the host chop.</summary>
[Dev("--wreck", "a hull section chopped with the fire axe (WreckDemo)")]
[Dev("--wrecks", "every kind of wreckage chopped in a row (WreckDemo)")]
[Dev("--harpoon", "a crate and a section shot and reeled in (WreckDemo)")]
public sealed class WreckScenario : DevScenario
{
    private bool All => Args.Has("--wrecks");

    private bool Harpoon => Args.Has("--harpoon");

    public override Layout? Map => WreckDemo.Map;

    public override World Seed(World world, DevSeed seed) => WreckDemo.Seed(world, seed.Modules, all: All, harpoon: Harpoon);

    public override bool Hosted => true;

    public override void Attach(DevContext c) => c.Add(new WreckDemo(c.Debris, c.Ui, all: All, harpoon: Harpoon));
}

[Dev("--grapple", "the hero pulled up to a hall's ceiling and its wall (WreckDemo)")]
public sealed class GrappleScenario : DevScenario
{
    public override Layout? Map => WreckDemo.Hall;

    public override World Seed(World world, DevSeed seed) => WreckDemo.SeedGrapple(world, seed.Modules);

    public override void Attach(DevContext c) => c.Add(new WreckDemo(c.Debris, c.Ui, grapple: true));
}
