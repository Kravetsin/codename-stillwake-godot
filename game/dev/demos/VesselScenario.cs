using System;
using Stillwake.Sim;
using Stillwake.Sim.Places;

namespace Stillwake.Game.Dev;

/// <summary>
/// The new game's world with a generated vessel beside the ship (generated-vessels): <c>--vessel=class</c> finds one of that class and
/// jumps to it on the first ticks, as built; <c>--vessel=class,wrecked</c> broken by a breach (wrecks to come); <c>--vessel</c> alone a freighter. The
/// hero starts in the vessel's middle, so the camera holds it whole and its compartments up close.
/// </summary>
[Dev("--vessel", "the new game's world with a vessel beside the ship: =class as built, =class,wrecked by a breach (VesselScenario)", Shape = DevShape.Either)]
public sealed class VesselScenario : DevScenario
{
    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed)
    {
        var given = (Args.Given("--vessel") ?? "freighter").Split(',');
        world.Step([new Find(given[0])]);
        // A new world's first contact.
        world.Step([new Jump(1, Wrecked: given.Length > 1 && given[1] == "wrecked")]);
        return world;
    }

    public override Func<Registry, int>? Start => m =>
        m.Grid.Reserved is { } box ? (box.Top + box.Height / 2) * m.Grid.Width + box.Left + box.Width / 2 : m.Crew.Spawn;
}
