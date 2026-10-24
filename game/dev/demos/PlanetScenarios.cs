using System;
using Stillwake.Game.Features.Crew;
using Stillwake.Net;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// A planet opened and the hero beamed down at once (0077), or the test strip (0076). After the hero's own scenarios, whose
/// scripts (--ladders, --feel, --hills) win over the strip's.
/// </summary>
[Dev("--planet", "a planet grown from its seed, the hero beamed down with the axe, the drill and the helmet lamp (0077)", Order = 5)]
[Dev("--strip", "the test strip (0076); with --autopilot played by a script (HeroAutopilot.PlanetRun)", Order = 5)]
public sealed class PlanetScenario : DevScenario
{
    private bool _beamedBack;

    private bool Strip => Args.Has("--strip");

    // The hero carries the axe and the drill and wears the helmet lamp, to try the caves; --ground seeds the same.
    public override World Seed(World world, DevSeed seed) =>
        Args.Value("--ground") == null && (Args.Has("--planet") || Args.Given("--fauna") != null) ? GroundDemo.Seed(world, seed.Modules, ArcDrill) : world;

    public override HeroAutopilot? Script => Strip && Args.Has("--autopilot") ? HeroAutopilot.PlanetRun() : null;

    public override void Started(Client client)
    {
        client.Send(new OpenSurface((Strip ? Content.Surfaces.TestStrip : Content.PlanetTypes.First).Id));
        client.Send(new Transfer(client.World.Surfaces.Count));
    }

    /// <summary><c>--beam-back</c>: back aboard two seconds in, to see the switch both ways.</summary>
    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (_beamedBack || !Args.Has("--beam-back") || client.World.Tick < 2 * World.TicksPerSecond) return;
        _beamedBack = true;
        client.Send(new Transfer(0));
    }
}

/// <summary>Creatures let out by the hero, with --planet or --strip, hunted by script unless --manual (0102).</summary>
[Dev("--fauna", "creatures of every kind let out by the hero, or =kind one kind, hunted by script (FaunaHands, 0102)", Shape = DevShape.Either)]
public sealed class FaunaScenario : DevScenario
{
    private FaunaDemo? _demo;

    public override void Attach(DevContext c)
    {
        // =fluff (a species or its look): only that kind let out.
        string? kind = Args.Given("--fauna") is { Length: > 0 } k ? k : null;
        string? ram = Args.Given("--fauna-ram") is { } r ? r.Length > 0 ? r : "hit" : null;
        bool drill = Args.Has("--fauna-drill"), course = Args.Has("--fauna-course");
        _demo = new FaunaDemo(drill: drill, course: course, kind: kind, herd: Args.Has("--fauna-herd"), ram: ram) { Kill = Args.Has("--fauna-kill") };
        if (!Args.Has("--manual")) c.Add(new FaunaHands(_demo, c.Hero, still: drill, quick: course));
    }

    public override void Frame(Client client, Registry m, Action<string> log) => _demo?.Frame(client, log);
}
