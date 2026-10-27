using System;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

// The hero's maps and scripts. Where two give a script, the lower Order wins: --poses, --ledges, --ladders and --feel before
// --gym and --lone-hatch, and those before --hills (--feel --hills traces the hills), as the old chain had it.

[Dev("--poses", "the hall walked by a script, axe and drill carried, for the figure's motion (PosesDemo, 0090)")]
public sealed class PosesScenario : DevScenario
{
    private HeroAutopilot? _script;

    public override Layout? Map => HeroAutopilot.Hall;

    public override World Seed(World world, DevSeed seed) => GroundDemo.Seed(world, seed.Modules);

    public override HeroAutopilot? Script => _script ??= HeroAutopilot.PosesRun(Args.Has("--host") ? 900 : 40);

    public override void Attach(DevContext c) => c.Add(new PosesDemo(Script!));
}

[Dev("--ledges", "a ledge a jump steps up onto and one it falls short of (0092)")]
public sealed class LedgesScenario : DevScenario
{
    public override Layout? Map => HeroAutopilot.Ledges;

    public override HeroAutopilot? Script => Args.Has("--autopilot") ? HeroAutopilot.LedgesRun() : null;
}

[Dev("--ladders", "a tall ladder up through a hatch beside a laddered pit, or the strip's shaft with --strip (0092)")]
public sealed class LaddersScenario : DevScenario
{
    public override Layout? Map => Args.Has("--strip") ? null : HeroAutopilot.Ladders;

    public override HeroAutopilot? Script =>
        !Args.Has("--autopilot") ? null : Args.Has("--strip") ? HeroAutopilot.StripLaddersRun() : HeroAutopilot.LaddersRun();
}

[Dev("--cape", "the hero in a heavy cape hung on a rope from its shoulders, run about the hall; --manual by hand (CapeDemo)")]
public sealed class CapeScenario : DevScenario
{
    public override Layout? Map => HeroAutopilot.Hall;

    public override HeroAutopilot? Script => Args.Has("--manual") ? null : HeroAutopilot.CapeRun();

    public override void Attach(DevContext c) => c.Add(new CapeDemo());
}

[Dev("--feel", "the hero's jumps and runs traced step by step (HeroAutopilot.Feel, 0089)")]
public sealed class FeelScenario : DevScenario
{
    public override HeroAutopilot? Script => HeroAutopilot.Feel(Args.Has("--hills"), Args.Has("--strip"));
}

[Dev("--gym", "a room with a ladder for the autopilot", Order = 1)]
public sealed class GymScenario : DevScenario
{
    public override Layout? Map => HeroAutopilot.Gym;

    public override HeroAutopilot? Script => Args.Has("--autopilot") ? HeroAutopilot.GymRun() : null;
}

[Dev("--lone-hatch", "a hatch between two rooms with no ladder (Autopilot.LoneHatch); =open with it open", Shape = DevShape.Either, Order = 1)]
public sealed class LoneHatchScenario : DevScenario
{
    private bool Open => Args.Given("--lone-hatch") == "open";

    public override Layout? Map => Autopilot.LoneHatch(Open);

    public override HeroAutopilot? Script => Open && Args.Has("--autopilot") ? HeroAutopilot.LoneHatchRun() : null;
}

[Dev("--hills", "ground of one-cell steps to walk (HillsDemo)", Order = 2)]
public sealed class HillsScenario : DevScenario
{
    public override Layout? Map => HillsDemo.Map;

    public override World Seed(World world, DevSeed seed) => HillsDemo.Seed(world, seed.Modules);

    public override HeroAutopilot? Script => Args.Has("--autopilot") ? HeroAutopilot.HillsRun(HillsDemo.HighPlateau) : null;
}

[Dev("--floor", "stacks falling through a machine onto a dark floor and a step (FloorDemo)")]
public sealed class FloorScenario : DevScenario
{
    public override Layout? Map => FloorDemo.Map;

    public override World Seed(World world, DevSeed seed) => FloorDemo.Seed(world, seed.Modules);
}

[Dev("--segments", "a door and a hatch of segments")]
public sealed class SegmentsScenario : DevScenario
{
    public override Layout? Map => Autopilot.Segments;
}

[Dev("--die", "a quick death to see the body it leaves (DieDemo, 0061)")]
public sealed class DieScenario : DevScenario
{
    public override World Seed(World world, DevSeed seed) => DieDemo.Seed(world, seed.Modules);

    public override Func<Registry, int>? Start => DieDemo.StartOf;
}

[Dev("--suit", "printed bare, the free helmet and body made at the pod, worn and taken off, then out into space bare (SuitDemo)")]
public sealed class SuitScenario : DevScenario
{
    public override World Seed(World world, DevSeed seed) => SuitDemo.Seed(world, seed.Modules);

    public override void Attach(DevContext c)
    {
        if (c.Items != null) c.Add(new SuitDemo(c.Items, c.World));
    }
}

[Dev("--breathe", "a canister breathed and the pod's air breathed again (BreatheDemo)")]
[Dev("--breathe-air", "the pod's air breathed only (BreatheDemo)")]
public sealed class BreatheScenario : DevScenario
{
    private bool Canister => Args.Has("--breathe");

    public override World Seed(World world, DevSeed seed) => BreatheDemo.Seed(world, seed.Modules, suit: Canister ? 8 : 6);

    public override Func<Registry, int>? Start => BreatheDemo.StartOf;

    public override void Attach(DevContext c)
    {
        if (c.Items != null) c.Add(new BreatheDemo(c.Items, c.M, canister: Canister));
    }
}
