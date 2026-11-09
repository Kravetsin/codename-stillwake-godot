using System;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Places;

namespace Stillwake.Game.Dev;

/// <summary>
/// The new game's world with the host on a generated vessel's world (vessel-worlds): <c>--vessel=class</c> finds one of that class
/// as the world is made and jumps to it once the hero is in, as built; <c>--vessel</c> alone a freighter. The hero arrives just
/// inside its door; "to ship" on the dev panel brings it home, and the vessel's world closes.
/// </summary>
[Dev("--vessel", "the new game's world with the hero on a vessel's world: =class, a freighter by default (VesselScenario)", Shape = DevShape.Either)]
public sealed class VesselScenario : DevScenario
{
    private bool _jumped;

    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed)
    {
        world.Step([new Find(Args.Given("--vessel") ?? "freighter")]);
        return world;
    }

    // A jump needs a hero: sent on the first frame the host's stands aboard, to the new world's first contact.
    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (_jumped || client.World.SurfaceOf(client.Id) != 0 || m.Crew.HeroOf(client.Id) == null) return;
        _jumped = true;
        client.Send(new Jump(1));
    }
}
