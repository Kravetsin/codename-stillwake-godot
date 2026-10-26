using System;
using System.Linq;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Debris;
using Stillwake.Net;
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

[Dev("--sentry", "the sortie's derelicts, a sentry let out on the engine block beside the hero: watching, alerted, pouncing (0102)")]
public sealed class SentryScenario : DevScenario
{
    private bool _let;
    private string _was = "";

    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed) => SortieDemo.Seed(world, seed.M, seed.Modules);

    public override Func<Registry, int>? Start => SortieDemo.StartOf;

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (!_let && client.World.Tick >= 5 && m.Debris.Pieces.FirstOrDefault(p => p.Item == "engineBlock") is { } block)
        {
            _let = true;
            client.Send(new Sim.Creatures.Release(Content.Creatures.Sentry.Id, Sim.Debris.DebrisModule.CellOf(block.X, block.Y, m.Grid.Width)));
        }
        foreach (var c in m.Creatures.Creatures)
        {
            var spec = Content.Creatures.Get(c.Species);
            string now = c.Act is { } act ? $"act {act.Phase}" : $"{spec.Brain[c.Drive].GetType().Name} {c.Step}";
            if (now == _was) continue;
            log($"sentry: tick {client.World.Tick}, {now} at ({c.X}, {c.Y}), velocity ({c.VX}, {c.VY})");
            _was = now;
        }
    }
}
