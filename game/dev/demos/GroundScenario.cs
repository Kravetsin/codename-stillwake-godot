using System;
using System.Globalization;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Net;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>A planet's ground looked at, dug or struck by script (<see cref="GroundDemo"/>, 0077), the hero carrying its tools.</summary>
[Dev("--ground", "a planet's ground looked at: acacia, hit-soil|-stone|-alloy|-tree|-crystal, band0…3, hall, cave, shaft, sprint, spire-climb, spire-guards, drill, poses, fog, fog-cave (GroundDemo)", Shape = DevShape.Equals)]
public sealed class GroundScenario : DevScenario
{
    private GroundDemo? _demo;

    private string Mode => Args.Value("--ground")!;

    private GroundDemo Demo => _demo ??= new GroundDemo(Mode)
    {
        TakeAt = Args.Arg("--take-at") is { } take ? (int)(double.Parse(take, CultureInfo.InvariantCulture) * World.TicksPerSecond) : null,
        Offset = int.Parse(Args.Arg("--ground-offset") ?? "0"),
    };

    public override World Seed(World world, DevSeed seed) => GroundDemo.Seed(world, seed.Modules, ArcDrill);

    public override HeroAutopilot? Script =>
        !Args.Has("--autopilot") ? null
        : Mode == "spire-climb" ? HeroAutopilot.SpireClimb()
        : Mode == "sprint" ? HeroAutopilot.Sprint(int.Parse(Args.Arg("--sprint") ?? "300"))
        : Mode is "fog" or "fog-cave" ? HeroAutopilot.Fog()
        : null;

    public override void Attach(DevContext c)
    {
        if (Mode is "drill" or "poses") c.Add(new GroundHands(Demo));
        // With --arc-drill the fitted drill is the first slot and the axe the third (GroundDemo.Seed).
        else if (GroundDemo.Hits(Mode))
            c.Add(new GroundHands(Demo, slot: Args.Has("--axe") ? (ArcDrill != null ? Key.Key3 : Key.Key1) : ArcDrill != null ? Key.Key1 : Key.Key2));
        else if (GroundDemo.Caves(Mode)) c.Add(new GroundHands(Demo, dig: false));
    }

    public override void Frame(Client client, Registry m, Action<string> log) => Demo.Frame(client, log);
}
