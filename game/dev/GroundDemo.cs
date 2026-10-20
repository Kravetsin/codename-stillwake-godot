using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --ground=cave</c>, <c>--ground=drill</c> or <c>--ground=shaft</c> (0077): a planet opened, its hero carrying the axe and the drill with the
/// helmet lamp worn, for recording the ground and digging. <c>cave</c> beams the hero down into the cave nearest under the
/// spawn, deep in the dark, and digs a tunnel into the stone beside its head, by the lamp's light; <c>shaft</c> beams it to
/// the spawn and digs a shaft down under its feet, so it drops in as it goes; <c>drill</c> digs the tunnel with the drill in the
/// hero's hands, aimed by <see cref="GroundHands"/> as a mouse would. An axe hit every <see cref="Pace"/> ticks,
/// sent as a strike sends them: cracks, chips, dust, and the sky falling into the shaft.
/// </summary>
public sealed class GroundDemo(string mode)
{
    /// <summary>Ticks between two hits: an axe's pace, a little slower, so each crack shows.</summary>
    public const int Pace = 12;

    /// <summary>How many cells the shaft goes down, and the tunnel along.</summary>
    private const int Depth = 14, Along = 3;

    private int? _planet;
    private bool _opened;
    private int _hits;
    /// <summary>The cells to dig, in turn; the first not yet broken is hit.</summary>
    private readonly List<int> _cells = [];

    /// <summary>The world with peer 0 carrying the axe and the drill, a full battery on its belt and the helmet lamp on its head.</summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        var items = root["slices"]!["items"]!;
        var slots = new JsonArray();
        int head = Spec.Inventory.Slots, belt = Spec.Inventory.Slots + 2;
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == 0 ? Stack(Spec.Weapons.Axe.Item)
                : i == 1 ? Stack(Spec.Weapons.Drill.Item)
                : i == head ? Stack(Spec.HelmetLamp.Item)
                : i == belt ? new JsonObject { ["item"] = "battery", ["count"] = 1, ["charge"] = Spec.Charge.Battery }
                : null);
        items["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), modules());
    }

    private static JsonObject Stack(string item) => new() { ["item"] = item, ["count"] = 1 };

    /// <summary>A frame of the demo: the planet opened, the hero beamed where the mode wants it, then its cells dug.</summary>
    public void Frame(Client client, Action<string> log)
    {
        var world = client.World;
        if (_planet == null)
        {
            if (world.Surfaces.Count == 1 && !_opened) client.Send(new OpenSurface(Spec.Surfaces.Planet.Id));
            _opened = true;
            if (world.Surfaces.Count < 2) return;
            _planet = world.Surfaces.Count - 1;
            var m = Registry.On(world, _planet.Value);
            int w = m.Grid.Width;
            bool tunnel = mode is "cave" or "drill";
            int at = tunnel ? CaveUnder(m) ?? m.Crew.Spawn : m.Crew.Spawn;
            // The shaft under the feet; the tunnel at head height the way the hero faces, right.
            for (int i = 1; i <= (tunnel ? Along : Depth); i++) _cells.Add(tunnel ? at - w + i : at + i * w);
            client.Send(new Transfer(_planet.Value, at));
            log($"ground demo: beamed down to ({at % w}, {at / w})");
            return;
        }
        if (world.SurfaceOf(client.Id) != _planet) return;
        var here = Registry.On(world, _planet.Value);
        int next = _cells.FindIndex(c => here.Grid.WallAt(c) is { Breaks: true });
        // drill: the hands dig, aimed at the cell by GroundHands; the others send an axe's hits as a strike would.
        if (mode == "drill")
        {
            Target = next >= 0 ? (_cells[next] % here.Grid.Width, _cells[next] / here.Grid.Width) : null;
            return;
        }
        if (world.Tick - _hits < Pace) return;
        _hits = world.Tick;
        if (next >= 0) client.Send(new Dig(_cells[next], Spec.Weapons.Axe.Item));
    }

    /// <summary>With <c>drill</c>, the cell the hands aim the drill at, by column and row: the tunnel's next not dug yet.</summary>
    public (int X, int Y)? Target { get; private set; }

    /// <summary>The open cell nearest under the spawn, at least twenty rows down, standing on ground with room above: in a cave.</summary>
    private static int? CaveUnder(Registry m)
    {
        var grid = m.Grid;
        int w = grid.Width, spawn = m.Crew.Spawn, sx = spawn % w, sy = spawn / w;
        bool Open(int x, int y) => x > 0 && y > 0 && x < w && y < grid.Height && grid.StructureAt(y * w + x) == Sim.Grid.Structure.Empty;
        return Enumerable.Range(sy + 20, grid.Height - sy - 24)
            .SelectMany(y => Enumerable.Range(-60, 121).Select(dx => (X: sx + dx, Y: y)))
            .Where(p => Open(p.X, p.Y) && Open(p.X, p.Y - 1) && Open(p.X, p.Y - 2) && !Open(p.X, p.Y + 1) && !Open(p.X + 1, p.Y - 1))
            .OrderBy(p => Math.Abs(p.X - sx) + 2 * (p.Y - sy))
            .Select(p => (int?)(p.Y * w + p.X))
            .FirstOrDefault();
    }
}
