using System;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Places;

namespace Stillwake.Game.Dev;

/// <summary>
/// The new game's world with the host on a generated vessel's world (vessel-worlds): <c>--vessel=class</c> finds one of that class
/// as the world is made and jumps to it once the hero is in, as built; <c>--vessel</c> alone a freighter. The hero arrives just
/// inside its door; "to ship" on the dev panel brings it home, and the vessel's world closes. <c>--breakers</c> hands it what
/// breaks hulls, creative, to try the damage scale on the vessel and the ship alike (damage-scale).
/// </summary>
[Dev("--vessel", "the new game's world with the hero on a vessel's world: =class, a freighter by default (VesselScenario)", Shape = DevShape.Either)]
[Dev("--breakers", "with --vessel, the pickaxe, sledgehammer, fire axe, fifty blasting charges and the railgun with nails on 1-6, a battery on the belt, creative (damage-scale)")]
public sealed class VesselScenario : DevScenario
{
    private bool _jumped, _creative;

    private bool Breakers => Args.Has("--breakers");

    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed)
    {
        world.Step([new Find(Args.Given("--vessel") ?? "freighter")]);
        if (!Breakers) return world;
        (string Item, int Count)[] hotbar =
        [
            (Content.Weapons.Pickaxe.Item, 1), (Content.Weapons.Sledgehammer.Item, 1), (Content.Weapons.Axe.Item, 1),
            (Content.Shots.Thrown.Item, 50), (Content.Shots.Railgun.Item, 1), (Content.Shots.Nail.Id, 50),
        ];
        var slots = new JsonArray();
        for (int i = 0; i < Content.Inventory.Total; i++)
            slots.Add(i < hotbar.Length ? new JsonObject { ["item"] = hotbar[i].Item, ["count"] = hotbar[i].Count } : null);
        slots[Content.Inventory.Slots + 2] = new JsonObject { ["item"] = "battery", ["count"] = 1, ["charge"] = Content.Charge.Battery };
        var root = JsonNode.Parse(world.Save())!;
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    // A jump needs a hero: sent on the first frame the host's stands aboard, to the new world's first contact.
    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (m.Crew.HeroOf(client.Id) == null) return;
        if (Breakers && !_creative)
        {
            _creative = true;
            client.Send(new Sim.Items.SetCreative(true));
        }
        if (_jumped || client.World.SurfaceOf(client.Id) != 0) return;
        _jumped = true;
        client.Send(new Jump(1));
    }
}
