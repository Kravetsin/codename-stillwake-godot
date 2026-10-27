using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Net;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// <c>-- --fly</c>: the big world (0048) with a jetpack worn and canisters carried, the hero starting out in
/// space above the hull, past the stream's box, where the far field spawns round it (0052), and the
/// autopilot's <c>HeroAutopilot.Fly</c> flying it through. <c>--far=N</c> puts N pieces of the far field
/// out round the start at once, through the save, to measure the view with many out; <c>--bow N</c> starts it N
/// columns ahead of the bow instead, in the stream (<see cref="Bow"/>). While it runs a
/// canister is breathed whenever the suit is low, and every 5 s the pieces out and the pictures drawn are printed.
/// </summary>
public partial class FarDemo(DebrisView view, Func<(Client Client, Registry M)?> world) : Node
{
    /// <summary>Rows above the top of what is built the hero starts: out of the stream's box (its margin is 24).</summary>
    private const int Above = 60;
    private double _seconds, _printed, _breathed;

    /// <summary>
    /// <c>--bow=N</c>: the hero starts N columns right of what is built, on its middle row, in the stream ahead of
    /// the bow, to see where the stream comes in from (0052); null to start above the hull.
    /// </summary>
    public static int? Bow { get; set; }

    /// <summary>Where the hero starts: above the spawn, <see cref="Above"/> rows over the top of what is built, or <see cref="Bow"/> ahead of it.</summary>
    public static int StartOf(Registry m) => Bow is { } bow
        ? (m.Grid.Built.Top + m.Grid.Built.Bottom) / 2 * m.Grid.Width + Math.Min(m.Grid.Width - 1, m.Grid.Built.Right + bow)
        : Math.Max(0, m.Grid.Built.Top - Above) * m.Grid.Width + m.Crew.Spawn % m.Grid.Width;

    /// <summary>
    /// <paramref name="world"/>, read through <paramref name="m"/>, with a suit and a jetpack on peer 0 and canisters in its first
    /// slot, and <paramref name="pieces"/> pieces of the far field out round the start, mostly right of it so the drift brings them by.
    /// </summary>
    public static World Seed(World world, Registry m, Func<IReadOnlyList<Module>> modules, int pieces)
    {
        var root = JsonNode.Parse(world.Save())!;
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == Spec.Inventory.Slots + 1 ? new JsonObject { ["item"] = Spec.Jetpack.Item, ["count"] = 1 }
                : i == Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf(Spec.Suit.Slot) ? new JsonObject { ["item"] = Spec.Suit.Basic, ["count"] = 1 }
                : i == Spec.Inventory.Slots ? new JsonObject { ["item"] = Spec.Helmet.Basic, ["count"] = 1 }
                : i == 0 ? new JsonObject { ["item"] = Spec.Crew.Canister, ["count"] = 20 } : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        var debris = root["slices"]!["debris"]!;
        var list = debris["pieces"]!.AsArray();
        int next = (int)debris["nextId"]!, clock = (int)debris["clock"]!;
        int start = StartOf(m), sx = start % m.Grid.Width, sy = start / m.Grid.Width;
        // The hero already out at the start, so the pieces round it are kept from the first tick.
        root["slices"]!["crew"]!["heroes"] = new JsonArray(new JsonObject
        {
            ["id"] = 0,
            ["cell"] = start,
            ["air"] = Spec.Items.Get(Spec.Suit.Basic).Tank,
            ["out"] = 0,
            ["exerting"] = false,
            ["thrusting"] = false,
            ["thrustTicks"] = 0,
        });
        var kinds = Spec.Debris.Far.Tiers[1].Kinds;
        var random = new Random(51);
        for (int i = 0; i < pieces; i++)
        {
            int x = (sx - 80 + random.Next(321)) * 1000 + 500, y = Math.Max(0, sy - 80 + random.Next(161)) * 1000 + 500;
            list.Add(new JsonObject
            {
                ["id"] = next++,
                ["zone"] = Spec.Debris.Far.Id,
                ["item"] = kinds[random.Next(kinds.Count)].Item,
                ["x"] = x,
                ["y"] = y,
                ["speed"] = Spec.Debris.Far.DriftMin + random.Next(Spec.Debris.Far.DriftMax - Spec.Debris.Far.DriftMin + 1),
                ["start"] = x,
                ["end"] = 0,
                ["damage"] = 0,
                ["reel"] = null,
                ["seenAt"] = clock,
            });
        }
        debris["nextId"] = next;
        return World.Load(root.ToJsonString(), modules());
    }

    public override void _Process(double delta)
    {
        _seconds += delta;
        if (world() is not var (client, m)) return;
        var hero = m.Crew.HeroOf(client.Id);
        if (hero != null && hero.Air < m.Crew.CapacityOf(client.Id) / 2 && _seconds - _breathed > 2)
        {
            client.Send(new Sim.Crew.Refill());
            _breathed = _seconds;
        }
        if (_seconds - _printed < 5) return;
        _printed = _seconds;
        int far = 0;
        foreach (var p in m.Debris.Pieces)
            if (p.Zone == Spec.Debris.Far.Id) far++;
        var (drawn, made) = view.Pictures;
        GD.Print($"FAR {_seconds:F0}s tick {client.World.Tick}: {m.Debris.Pieces.Count} pieces out, {far} far; {drawn} drawn, {made} pictures made; hero at {hero?.Cell % m.Grid.Width},{hero?.Cell / m.Grid.Width}");
    }
}
