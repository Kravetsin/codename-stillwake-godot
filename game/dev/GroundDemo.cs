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
/// hero's hands, aimed by <see cref="GroundHands"/> as a mouse would; <c>poses</c> aims the drill round the hero eight ways near
/// and far; <c>mouth</c> stands it on the rim of the nearest pit, chasm or cave mouth, to see the sky down it; <c>acacia</c> beside the
/// acacia nearest the spawn, to see one close (0079); <c>slope</c> beside the surface slopes nearest it, each hit once. An axe hit every <see cref="Pace"/> ticks,
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
            int at = tunnel ? CaveUnder(m) ?? m.Crew.Spawn : mode == "mouth" ? RimNear(m) ?? m.Crew.Spawn
                : mode == "acacia" ? BesideAcacia(m) ?? m.Crew.Spawn : mode == "slope" ? BesideSlopes(m, _cells) ?? m.Crew.Spawn : m.Crew.Spawn;
            // The shaft under the feet; the tunnel at head height the way the hero faces, right.
            for (int i = 1; i <= (tunnel ? Along : Depth) && mode is not ("slope" or "acacia"); i++) _cells.Add(tunnel ? at - w + i : at + i * w);
            client.Send(new Transfer(_planet.Value, at));
            log($"ground demo: beamed down to ({at % w}, {at / w})");
            return;
        }
        if (world.SurfaceOf(client.Id) != _planet || mode is "mouth" or "acacia") return;
        var here = Registry.On(world, _planet.Value);
        int next = _cells.FindIndex(c => here.Grid.WallAt(c) is { Breaks: true });
        // drill: the hands dig, aimed at the cell by GroundHands; the others send an axe's hits as a strike would.
        if (mode == "drill")
        {
            Target = next >= 0 ? (_cells[next] % here.Grid.Width + 0.5, _cells[next] / here.Grid.Width + 0.5) : null;
            return;
        }
        // poses: the drill aimed round the hero standing at the spawn, eight ways near and far, a while each.
        if (mode == "poses")
        {
            _posing ??= world.Tick;
            int pose = (world.Tick - _posing.Value) / PoseTicks, spawn = here.Crew.Spawn, w = here.Grid.Width;
            if (pose >= 16) Target = null;
            else
            {
                double angle = pose % 8 * Math.PI / 4, far = pose < 8 ? 1.6 : 4;
                Target = (spawn % w + 0.5 + Math.Cos(angle) * far, spawn / w + 1 - 1.4 + Math.Sin(angle) * far);
            }
            return;
        }
        if (world.Tick - _hits < Pace) return;
        _hits = world.Tick;
        // slope: each of its cells hit once, so their cracks show on the slopes' solid halves.
        if (mode == "slope")
        {
            if (_struck < _cells.Count) client.Send(new Dig(_cells[_struck++], Spec.Weapons.Axe.Item));
            return;
        }
        if (next >= 0) client.Send(new Dig(_cells[next], Spec.Weapons.Axe.Item));
    }

    /// <summary>
    /// With <c>drill</c> or <c>poses</c>, where the hands aim the drill, in cells: the tunnel's next cell not dug yet, or a point
    /// round the hero, one of eight ways near and far for <see cref="PoseTicks"/> ticks each.
    /// </summary>
    public (double X, double Y)? Target { get; private set; }

    /// <summary>Ticks each pose of <c>poses</c> is held: a second.</summary>
    public const int PoseTicks = 20;

    private int? _posing;

    /// <summary>How many of <c>slope</c>'s cells were hit.</summary>
    private int _struck;

    /// <summary>The open cell three columns left of the acacia nearest the spawn, on the ground there: to see one close.</summary>
    private static int? BesideAcacia(Registry m)
    {
        int w = m.Grid.Width, sx = m.Crew.Spawn % w;
        var tree = m.Building.Plants.Where(p => p.Kind == "acacia").OrderBy(p => Math.Abs(p.Cell % w - sx)).FirstOrDefault();
        return tree == null ? null : (TopOf(m, tree.Cell % w - 3) - 1) * w + tree.Cell % w - 3;
    }

    /// <summary>
    /// The open cell two columns beside the surface slopes nearest the spawn, the three of them, and the whole cell under the
    /// first, put in <paramref name="cells"/> to be hit once each.
    /// </summary>
    private static int? BesideSlopes(Registry m, List<int> cells)
    {
        var grid = m.Grid;
        int w = grid.Width, sx = m.Crew.Spawn % w;
        var slopes = Enumerable.Range(2, w - 4).Select(x => (TopOf(m, x) * w + x)).Where(c => grid.ShapeAt(c) != Sim.Grid.Shape.None)
            .OrderBy(c => Math.Abs(c % w - sx)).Take(3).OrderBy(c => c % w).ToList();
        if (slopes.Count == 0) return null;
        cells.AddRange(slopes);
        cells.Add(slopes[0] + w);
        int x = slopes[0] % w - 2;
        return (TopOf(m, x) - 1) * w + x;
    }

    private static int TopOf(Registry m, int x) =>
        Enumerable.Range(0, m.Grid.Height).First(y => m.Grid.StructureAt(y * m.Grid.Width + x) != Sim.Grid.Structure.Empty);

    /// <summary>
    /// The open cell on the rim of the pit, chasm or cave mouth nearest the spawn: a column whose ground lies at least eight
    /// rows under that of the columns four either side, the hero standing on the higher of them.
    /// </summary>
    private static int? RimNear(Registry m)
    {
        var grid = m.Grid;
        int w = grid.Width, sx = m.Crew.Spawn % w;
        int Top(int x) => Enumerable.Range(0, grid.Height).First(y => grid.StructureAt(y * w + Math.Clamp(x, 1, w - 2)) != Sim.Grid.Structure.Empty);
        return Enumerable.Range(0, w / 2 - 8).SelectMany(d => new[] { sx + d, sx - d })
            .Where(x => x > 8 && x < w - 8 && Top(x) >= Math.Max(Top(x - 4), Top(x + 4)) + 8)
            .Select(x => (int?)((Top(Top(x - 4) < Top(x + 4) ? x - 4 : x + 4) - 1) * w + (Top(x - 4) < Top(x + 4) ? x - 4 : x + 4)))
            .FirstOrDefault();
    }

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
