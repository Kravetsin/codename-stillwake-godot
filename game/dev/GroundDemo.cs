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
/// acacia nearest the spawn, to see one close (0079); <c>pit</c> digs a pit under the spawn, a block left standing on its floor, to
/// tell the ground from its back wall (0088); <c>slope</c> beside the surface slopes nearest it, each hit once; <c>sprint</c> at the start of
/// its longest run of ground with no pit, for <see cref="Features.Crew.HeroAutopilot.Sprint"/> to measure; <c>band0</c>…<c>band3</c> in a
/// cave of that depth band, <c>hall</c> in the garden's great hall, looking at the growth there, the pointer on it (0080); <c>backs</c>
/// at the spawn, building soil and stone back walls beside it (0084). An
/// axe hit every <see cref="Pace"/> ticks,
/// sent as a strike sends them: cracks, chips, dust, and the sky falling into the shaft.
/// </summary>
public sealed class GroundDemo(string mode)
{
    /// <summary>Ticks between two hits: an axe's pace, a little slower, so each crack shows.</summary>
    public const int Pace = 12;

    /// <summary>How many cells the shaft goes down, and the tunnel along.</summary>
    private const int Depth = 14, Along = 3;

    /// <summary><c>pit</c>'s depth, its cells either side of the hero, and the column of the block left on its floor.</summary>
    private const int PitDepth = 3, PitSide = 3, PitBlock = 2;

    private int? _planet;
    private bool _opened;
    private int _hits;
    /// <summary>The cells to dig, in turn; the first not yet broken is hit.</summary>
    private readonly List<int> _cells = [];

    /// <summary>
    /// The world with peer 0 carrying the axe and the drill, a full battery on its belt and the helmet lamp on its head; with
    /// <paramref name="drill"/> (<c>--arc-drill</c>), that drill in the first slot, held, and the axe in the third, to dig down fast.
    /// </summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules, string? drill = null)
    {
        var root = JsonNode.Parse(world.Save())!;
        var items = root["slices"]!["items"]!;
        var slots = new JsonArray();
        int head = Spec.Inventory.Slots, belt = Spec.Inventory.Slots + 2;
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == 0 ? Stack(drill ?? Spec.Weapons.Axe.Item)
                : i == 1 ? Stack(Spec.Weapons.Drill.Item)
                : i == 2 && drill != null ? Stack(Spec.Weapons.Axe.Item)
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
            // In a cave of a band, or a great hall, looking at its growth (0080).
            if (Caves(mode) && CaveSpot(m, mode) is (int spot, var look))
            {
                Target = look;
                client.Send(new Transfer(_planet.Value, spot));
                log($"ground demo: beamed down to ({spot % w}, {spot / w}), looking at ({look.X:F0}, {look.Y:F0}), seed {world.Surfaces[_planet.Value].Seed}");
                return;
            }
            int at = tunnel ? CaveUnder(m) ?? m.Crew.Spawn : mode == "mouth" ? RimNear(m) ?? m.Crew.Spawn : mode == "sprint" ? Features.Crew.HeroAutopilot.SprintStart(m.Grid)
                : mode == "acacia" ? BesideAcacia(m) ?? m.Crew.Spawn : mode == "slope" ? BesideSlopes(m, _cells) ?? m.Crew.Spawn : m.Crew.Spawn;
            // The shaft under the feet; the tunnel at head height the way the hero faces, right.
            for (int i = 1; i <= (tunnel ? Along : Depth) && mode is not ("slope" or "acacia" or "sprint" or "pit"); i++) _cells.Add(tunnel ? at - w + i : at + i * w);
            // pit: a pit under the feet, a block of its ground left standing on its floor (0088).
            for (int dy = 1; dy <= PitDepth && mode == "pit"; dy++)
                for (int dx = -PitSide; dx <= PitSide; dx++)
                    if ((dx, dy) != (PitBlock, PitDepth)) _cells.Add(at + dy * w + dx);
            client.Send(new Transfer(_planet.Value, at));
            log($"ground demo: beamed down to ({at % w}, {at / w})");
            return;
        }
        if (world.SurfaceOf(client.Id) != _planet || mode is "mouth" or "acacia" or "sprint" || Caves(mode)) return;
        var here = Registry.On(world, _planet.Value);
        if (mode == "backs")
        {
            Backs(client, here);
            return;
        }
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

    private bool _planned;

    /// <summary>
    /// <c>backs</c> (0084): in creative mode, back walls of soil and of stone planned three by three on the ground right of
    /// the spawn, under the sky, then worked a plan at a time as E would.
    /// </summary>
    private void Backs(Client client, Registry m)
    {
        if (!_planned)
        {
            _planned = true;
            client.Send(new Sim.Items.SetCreative(true));
            int w = m.Grid.Width, spawn = m.Crew.Spawn;
            foreach (var (dx, material) in new[] { (2, "soil"), (6, "stone") })
                for (int dy = -2; dy <= 0; dy++)
                    for (int x = 0; x < 3; x++) client.Send(new PlacePlan(spawn + dy * w + dx + x, Sim.Grid.Placement.BackWall(material)));
            return;
        }
        if (m.Building.Plans.Count > 0) client.Send(new Work(m.Building.Plans[0].Id));
    }

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

    /// <summary>Whether <paramref name="mode"/> stands the hero in a cave of a band, <c>band0</c> to <c>band3</c>, or a great hall, <c>hall</c> (0080).</summary>
    public static bool Caves(string mode) => mode is "band0" or "band1" or "band2" or "band3" or "hall";

    /// <summary>
    /// With <c>band0</c>…<c>band3</c> or <c>hall</c> (0080): the cell to stand in, in a cave of that band — for <c>hall</c> the garden's,
    /// mostly open space sixteen rows high across the thirty columns before it, a great hall — with the most growth before it, a
    /// glowing one thrice, the nearest the spawn's column at a tie, within 700 columns of it; and that growth's middle, in
    /// cells, to look at.
    /// </summary>
    private static (int Cell, (double X, double Y) Look)? CaveSpot(Registry m, string mode)
    {
        var grid = m.Grid;
        int w = grid.Width, h = grid.Height, sx = m.Crew.Spawn % w, band = mode == "hall" ? 1 : mode[4] - '0';
        var ground = m.Rules.Ground!;
        var weight = new Dictionary<int, int>();
        foreach (var p in m.Building.Plants)
            if (CaveLooks.Of.TryGetValue(p.Kind, out var look)) weight[p.Cell] = look.Light != null ? 3 : 1;
        bool Open(int x, int y) => x > 0 && y > 0 && x < w && y < h && grid.StructureAt(y * w + x) == Sim.Grid.Structure.Empty;
        (int Score, int Far, int Cell, double X, double Y) best = (0, int.MaxValue, -1, 0, 0);
        for (int y = 3; y < h - 4; y++)
        {
            if (Terrain.BandOf(ground, y) != band) continue;
            for (int x = Math.Max(2, sx - 700); x < Math.Min(w - 24, sx + 700); x++)
            {
                if (!Open(x, y) || !Open(x, y - 1) || !Open(x, y - 2) || Open(x, y + 1) || !Open(x + 1, y)) continue;
                if (mode == "hall")
                {
                    int open = 0;
                    for (int dy = -16; dy <= 0; dy += 2)
                        for (int dx = 1; dx <= 30; dx += 2) open += Open(x + dx, y + dy) ? 1 : 0;
                    if (open < 9 * 15 * 7 / 10) continue;
                }
                int score = 0;
                double mx = 0, my = 0;
                int across = mode == "hall" ? 40 : 18, up = mode == "hall" ? 22 : 9;
                for (int dy = -up; dy <= 3; dy++)
                    for (int dx = 1; dx <= across; dx++)
                        if (weight.TryGetValue((y + dy) * w + x + dx, out int k)) (score, mx, my) = (score + k, mx + k * (x + dx + 0.5), my + k * (y + dy + 0.5));
                int far = Math.Abs(x - sx);
                if (score > best.Score || score == best.Score && score > 0 && far < best.Far) best = (score, far, y * w + x, mx / score, my / score);
            }
        }
        return best.Cell < 0 ? null : (best.Cell, (best.X, best.Y));
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
