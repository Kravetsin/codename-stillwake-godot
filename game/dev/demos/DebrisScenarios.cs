using System;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Debris;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

// Out among the debris: the hero far out of the wreck, played by script unless --manual.

[Dev("--debris", "the wreck with debris about, a crate caught with --autopilot (DebrisDemo)")]
public sealed class DebrisScenario : DevScenario
{
    public override World Seed(World world, DevSeed seed) => DebrisDemo.Seed(world, seed.Modules);

    public override bool Hosted => true;

    public override void Attach(DevContext c) => c.Add(new DebrisDemo(c.Debris, c.Ui));
}

[Dev("--fly", "the hero flown far out (FarDemo)")]
public sealed class FlyScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed)
    {
        if (Args.Arg("--bow") is { } bow) FarDemo.Bow = int.Parse(bow);
        return FarDemo.Seed(world, seed.M, seed.Modules, int.Parse(Args.Value("--far") ?? "0"));
    }

    public override HeroAutopilot? Script => Args.Has("--manual") ? null : HeroAutopilot.Fly();

    public override Func<Registry, int>? Start => FarDemo.StartOf;

    public override void Attach(DevContext c) => c.Add(new FarDemo(c.Debris, c.World));
}

[Dev("--asteroid", "two asteroids drilled and chopped out in the band (AsteroidDemo)")]
public sealed class AsteroidScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed) => AsteroidDemo.Seed(world, seed.M, seed.Modules, ArcDrill);

    public override Func<Registry, int>? Start => FarDemo.StartOf;

    public override void Attach(DevContext c)
    {
        if (!Args.Has("--manual")) c.Add(new AsteroidDemo(c.Debris, c.Ui, c.World));
    }
}

[Dev("--sortie", "the hero far out among one of each derelict and asteroid, an engine block drilled (SortieDemo)")]
public sealed class SortieScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed) => SortieDemo.Seed(world, seed.M, seed.Modules);

    public override Func<Registry, int>? Start => SortieDemo.StartOf;

    public override void Attach(DevContext c)
    {
        if (!Args.Has("--manual")) c.Add(new SortieDemo(c.Debris, c.Ui, c.World));
    }
}
