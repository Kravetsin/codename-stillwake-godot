using System;
using Stillwake.Sim;
using Stillwake.Sim.Places;

namespace Stillwake.Game.Dev;

/// <summary>
/// The new game's world with a generated vessel beside the ship (generated-vessels): <c>--vessel=class</c> finds one of that class and
/// jumps to it on the first ticks, wrecked by a breach; <c>--vessel=class,intact</c> as built; <c>--vessel</c> alone a freighter. The
/// hero starts over the vessel's middle, so the camera holds it whole.
/// </summary>
[Dev("--vessel", "the new game's world with a vessel beside the ship: =class, =class,intact as built (VesselScenario)", Shape = DevShape.Either)]
public sealed class VesselScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed)
    {
        var given = (Args.Given("--vessel") ?? "freighter").Split(',');
        world.Step([new Find(given[0])]);
        // A new world's first contact.
        world.Step([new Jump(1, Intact: given.Length > 1 && given[1] == "intact")]);
        return world;
    }

    public override Func<Registry, int>? Start => m =>
        m.Grid.Reserved is { } box ? (box.Top - 8) * m.Grid.Width + box.Left + box.Width / 2 : m.Crew.Spawn;
}
