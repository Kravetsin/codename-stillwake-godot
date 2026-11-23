using System;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Places;

namespace Stillwake.Game.Dev;

/// <summary>
/// The new game's world with the host on a generated vessel's world (vessel-worlds): <c>--vessel=class</c> finds one of that class
/// as the world is made and jumps to it once the hero is in, as built; <c>--vessel</c> alone a freighter. The hero arrives just
/// inside its door; "to ship" on the dev panel brings it home, and the vessel's world closes. <c>--breakers</c> hands it what
/// breaks hulls, creative, to try the damage scale on the vessel and the ship alike (damage-scale). <c>--wrecked=blast,fire,rock:old</c>
/// forces the vessel's chain of events (vessel-wrecks), each fresh but one marked <c>:old</c>; a step not allowed at its turn is skipped and
/// said so; <c>--wrecked=none</c> an intact one, <c>--wrecked</c> alone the chain its seed draws.
/// </summary>
[Dev("--vessel", "the new game's world with the hero on a vessel's world: =class, a freighter by default (VesselScenario)", Shape = DevShape.Either)]
[Dev("--dock", "with --vessel, the hero arrives beside its first guard's dock, on an intact vessel: =spider (by default) or =escort (derelict-enemies)", Shape = DevShape.Either)]
[Dev("--wrecked", "with --vessel, its chain of events: =blast,fire,ice,rock,break each, :old to age one; =none intact; alone the seed's (vessel-wrecks)", Shape = DevShape.Either)]
[Dev("--breakers", "with --vessel, the pickaxe, sledgehammer, fire axe, fifty blasting charges and the railgun with nails on 1-6, a battery on the belt, creative (damage-scale)")]
public sealed class VesselScenario : DevScenario
{
    private bool _jumped, _creative;

    private bool Breakers => Args.Has("--breakers");

    public override Layout? Map => Registry.Wreck;

    public override Func<Registry, int?>? Arrive => Args.Given("--dock") is { } species ? m => Beside(m, Species(species)) : null;

    private static string Species(string given) => given.Length == 0 ? Content.Vessels.Aboard.Capsule.Species : given;

    /// <summary>Whether contact <paramref name="k"/>'s vessel as built has a dock of <paramref name="species"/>'s guard: an intact one only.</summary>
    private static bool Docks(Contact k, string species) =>
        Shipyard.Guards(Shipyard.Build(Content.Vessels.Class(k.Class)!, k.Seed, k.Wreck)).Any(d => d.Species == species);

    /// <summary>
    /// The cell a hero's feet stand on beside the first dock of <paramref name="species"/>'s guard, on its floor: two columns past its right
    /// side where open, else two before its left; for a stand in its big hall, on along its floor up to ten more short of a wall or a door,
    /// out of its escort's claw's reach so it shoots first; null on a surface with none.
    /// </summary>
    private static int? Beside(Registry m, string species)
    {
        if (m.Creatures.Docks.FirstOrDefault(d => d.Species == species) is not { } dock) return null;
        int margin = Content.Vessels.Margin, w = m.Grid.Width, y = margin + dock.Y + dock.Height - 1;
        int right = margin + dock.X + dock.Width + 1, left = margin + dock.X - 2;
        bool Open(int x) => !m.Grid.Solid(y * w + x) && !m.Grid.Solid((y - 4) * w + x) && !Sim.Grid.Doorways.IsDoorway(m.Grid.StructureAt((y - 4) * w + x));
        int at = Open(right) ? right : left, way = at == right ? 1 : -1;
        for (int k = 0; dock.Rule.Big && k < 10 && Open(at + way * 2); k++) at += way;
        return y * w + at;
    }

    public override World Seed(World world, DevSeed seed)
    {
        // With --dock, every contact the radar holds, so one whose vessel has that guard's dock is jumped to.
        for (int i = Args.Given("--dock") == null ? 1 : Content.Vessels.MaxContacts; i > 0; i--)
            world.Step([new Find(Args.Given("--vessel") ?? "freighter", Args.Given("--wrecked") is { } chain ? WreckChain.Parse(chain) : null)]);
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
        int to = Args.Given("--dock") is { } species && m.Places.Contacts.FirstOrDefault(k => Docks(k, Species(species))) is { } docked ? docked.Id : 1;
        if (m.Places.Contacts.FirstOrDefault(k => k.Id == to) is { } contact)
        {
            var built = Shipyard.Build(Content.Vessels.Class(contact.Class)!, contact.Seed, contact.Wreck);
            var made = built.Wreck?.Events.Select(e => new Content.WreckStep(e.Kind, e.Old)).ToList() ?? [];
            log($"wreck: {contact.Class} seed {contact.Seed}, {(made.Count == 0 ? "intact" : WreckChain.Write(made))}"
                + (contact.Wreck is { } asked && asked.Count != made.Count ? $" (asked {WreckChain.Write(asked)}: a step not allowed at its turn skipped)" : ""));
        }
        client.Send(new Jump(to));
    }
}
