using System;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Spec = Stillwake.Content;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Dev;

/// <summary>
/// The grapple's climb out of a planet's deep (grapple-climbs): the first planet opened and the hero beamed to the bottom of its
/// deepest chasm, open to the sky hundreds of rows up, with the harpoon in hand, the drill with a battery to dig deeper, the axe and
/// the helmet lamp, in creative mode so the climb is never cut short by the air.
/// </summary>
[Dev("--climb", "the first planet, the hero at the bottom of its deepest chasm with the harpoon in hand, the drill, the axe and the lamp, creative (grapple-climbs)", Order = 5)]
public sealed class ClimbScenario : DevScenario
{
    private bool _beamed, _creative;
    private int _planet;

    public override World Seed(World world, DevSeed seed)
    {
        var root = JsonNode.Parse(world.Save())!;
        var items = root["slices"]!["items"]!;
        var slots = new JsonArray();
        int lamp = Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf(Spec.Helmet.Mount), belt = Spec.Inventory.Slots + 2;
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == 0 ? Stack(Spec.Harpoon.Item)
                : i == 1 ? Stack(Spec.Weapons.Drill.Item)
                : i == 2 ? Stack(Spec.Weapons.Axe.Item)
                : i == lamp ? Stack(Spec.HelmetLamp.Item)
                : i == belt ? new JsonObject { ["item"] = "battery", ["count"] = 1, ["charge"] = Spec.Charge.Battery }
                : null);
        items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), seed.Modules());
    }

    private static JsonObject Stack(string item) => new() { ["item"] = item, ["count"] = 1 };

    public override void Started(Client client) => client.Send(new OpenSurface(Spec.PlanetTypes.First.Id));

    public override void Frame(Client client, Registry m, Action<string> log)
    {
        var world = client.World;
        // Creative is the surface's own: switched on once the hero is there.
        if (_beamed && !_creative && world.SurfaceOf(client.Id) == _planet)
        {
            _creative = true;
            client.Send(new Sim.Items.SetCreative(true));
        }
        if (_beamed || world.Surfaces.Count < 2) return;
        _beamed = true;
        int planet = _planet = world.Surfaces.Count - 1;
        var grid = Registry.On(world, planet).Grid;
        int w = grid.Width;
        // Each column's first solid row from the sky; the deepest pair of columns, out of the fog, is the chasm's floor.
        var top = new int[w];
        for (int x = 0; x < w; x++) top[x] = Enumerable.Range(0, grid.Height).FirstOrDefault(y => grid.StructureAt(y * w + x) != Structure.Empty);
        int margin = ((Spec.PlanetType)Registry.On(world, planet).Rules).Ground.Fog + 8;
        int best = Enumerable.Range(margin, w - 2 * margin).MaxBy(x => Math.Min(top[x], top[x + 1]));
        int floor = Math.Min(top[best], top[best + 1]);
        client.Send(new Transfer(planet, (floor - 1) * w + best));
        log($"climb: beamed to the chasm's floor at ({best}, {floor}), {floor - top[Math.Max(0, best - 40)]} rows under the ground 40 columns left");
    }
}
