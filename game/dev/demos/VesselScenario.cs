using System;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Places;

namespace Stillwake.Game.Dev;

/// <summary>
/// The new game's world with a generated vessel beside the ship (generated-vessels): <c>--vessel=class</c> finds one of that class and
/// jumps to it on the first ticks, as built; <c>--vessel=class,wrecked</c> broken by a breach (wrecks to come); <c>--vessel</c> alone a freighter. The
/// hero starts in the vessel's middle, so the camera holds it whole and its compartments up close. <c>--breakers</c> hands it what
/// breaks hulls, creative, to try the damage scale on the vessel and the ship alike (damage-scale).
/// </summary>
[Dev("--vessel", "the new game's world with a vessel beside the ship: =class as built, =class,wrecked by a breach (VesselScenario)", Shape = DevShape.Either)]
[Dev("--breakers", "with --vessel, the pickaxe, sledgehammer, fire axe, fifty blasting charges and the railgun with nails on 1-6, a battery on the belt, creative (damage-scale)")]
public sealed class VesselScenario : DevScenario
{
    private bool _creative;

    private bool Breakers => Args.Has("--breakers");

    public override Layout? Map => Registry.Wreck;

    public override World Seed(World world, DevSeed seed)
    {
        var given = (Args.Given("--vessel") ?? "freighter").Split(',');
        world.Step([new Find(given[0])]);
        // A new world's first contact.
        world.Step([new Jump(1, Wrecked: given.Length > 1 && given[1] == "wrecked")]);
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

    public override Func<Registry, int>? Start => m =>
        m.Grid.Reserved is { } box ? (box.Top + box.Height / 2) * m.Grid.Width + box.Left + box.Width / 2 : m.Crew.Spawn;

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        if (!Breakers || _creative || m.Crew.HeroOf(client.Id) == null) return;
        _creative = true;
        client.Send(new Sim.Items.SetCreative(true));
    }
}
