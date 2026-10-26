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
        Args.Value("--ground") == null && (Args.Has("--planet") || Args.Given("--creatures") != null) ? GroundDemo.Seed(world, seed.Modules, ArcDrill) : world;

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
[Dev("--creatures", "creatures of every kind let out by the hero, or =kind one kind, hunted by script (CreatureHands, 0102)", Shape = DevShape.Either)]
public sealed class CreaturesScenario : DevScenario
{
    private CreaturesDemo? _demo;

    public override void Attach(DevContext c)
    {
        // =fluff (a species or its look): only that kind let out.
        string? kind = Args.Given("--creatures") is { Length: > 0 } k ? k : null;
        string? ram = Args.Given("--creatures-ram") is { } r ? r.Length > 0 ? r : "hit" : null;
        bool drill = Args.Has("--creatures-drill"), course = Args.Has("--creatures-course");
        _demo = new CreaturesDemo(drill: drill, course: course, kind: kind, herd: Args.Has("--creatures-herd"), ram: ram) { Kill = Args.Has("--creatures-kill") };
        if (!Args.Has("--manual")) c.Add(new CreatureHands(_demo, c.Hero, still: drill, quick: course));
    }

    public override void Frame(Client client, Registry m, Action<string> log) => _demo?.Frame(client, log);
}

/// <summary>The nailer fired by script (shots-are-the-sims): on the strip with --strip, aboard without.</summary>
[Dev("--nailer", "the nailer and nails in hand, fired up at 45°, at a loper and level, by script (NailerDemo)")]
public sealed class NailerScenario : DevScenario
{
    private NailerDemo? _demo;

    public override World Seed(World world, DevSeed seed) => NailerDemo.Seed(world, seed.Modules);

    // With --manual the player fires: the loper let out, nothing else played.
    public override void Attach(DevContext c)
    {
        _demo = new NailerDemo(c.Hero);
        if (!Args.Has("--manual")) c.Add(_demo);
    }

    public override void Frame(Client client, Registry m, Action<string> log) => _demo?.Frame(client, m);
}
