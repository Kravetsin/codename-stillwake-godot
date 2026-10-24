using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Spec = Stillwake.Content;
using Structure = Stillwake.Content.Structure;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --ground=cave</c>, <c>--ground=drill</c> or <c>--ground=shaft</c> (0077): a planet opened, its hero carrying the axe and the drill with the
/// helmet lamp worn, for recording the ground and digging. <c>cave</c> beams the hero down into the cave nearest under the
/// spawn, deep in the dark, and digs a tunnel into the stone beside its head, by the lamp's light; <c>shaft</c> beams it to
/// the spawn and digs a shaft down under its feet, so it drops in as it goes; <c>drill</c> digs the tunnel with the drill in the
/// hero's hands, aimed by <see cref="GroundHands"/> as a mouse would; <c>poses</c> aims the drill round the hero eight ways near
/// and far; <c>mouth</c> stands it on the rim of the nearest pit, chasm or cave mouth, to see the sky down it; <c>acacia</c> beside the
/// acacia nearest the spawn, to see one close (0079); <c>pit</c> digs a pit under the spawn, a block left standing on its floor, to
/// tell the ground from its back wall (0088); <c>sprint</c> at the start of
/// its longest run of ground with no pit, for <see cref="Features.Crew.HeroAutopilot.Sprint"/> to measure; <c>band0</c>…<c>band3</c> in a
/// cave of that depth band, <c>hall</c> in the garden's great hall, looking at the growth there, the pointer on it (0080); <c>backs</c>
/// at the spawn, building soil and stone back walls beside it (0084); <c>lava</c> on the floor of the cave nearest the spawn's
/// column whose lava is open, looking at it; <c>spire</c> on the roof of the spire nearest the spawn beside its left plug,
/// <c>spire-in</c> in its chamber by the cache, <c>spire-far</c> on the ground forty columns left of it, to see its tip over the
/// hills (0085), <c>spire-climb</c> on its chamber's floor under the light, its right plug dug, for <c>--autopilot</c> to climb out
/// (0096). An
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

    /// <summary><c>--take-at SECONDS</c> at a spire: the world's tick its nearest relic is struck from, till it is taken (0096).</summary>
    public int? TakeAt { get; init; }

    /// <summary><c>--ground-offset N</c>: the hero stood N columns right of where its mode stands it (left for less than none).</summary>
    public int Offset { get; init; }

    private int? _planet;
    private bool _opened;
    private int _hits;
    /// <summary>The cells to dig, in turn; the first not yet broken is hit.</summary>
    private readonly List<int> _cells = [];

    /// <summary>
    /// The world with peer 0 carrying the axe and the drill, a full battery on its belt and the helmet lamp on its head; with
    /// <paramref name="module"/> (<c>--arc-drill</c>), a drill with that module fitted in the first slot, held, and the axe in the
    /// third, to dig down fast (0098).
    /// </summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules, string? module = null)
    {
        var root = JsonNode.Parse(world.Save())!;
        var items = root["slices"]!["items"]!;
        var slots = new JsonArray();
        int head = Spec.Inventory.Slots, belt = Spec.Inventory.Slots + 2;
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == 0 ? module != null ? Testbed.FittedDrill(module) : Stack(Spec.Weapons.Axe.Item)
                : i == 1 ? Stack(Spec.Weapons.Drill.Item)
                : i == 2 && module != null ? Stack(Spec.Weapons.Axe.Item)
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
            if (world.Surfaces.Count == 1 && !_opened) client.Send(new OpenSurface(Spec.PlanetTypes.First.Id));
            _opened = true;
            if (world.Surfaces.Count < 2) return;
            _planet = world.Surfaces.Count - 1;
            var m = Registry.On(world, _planet.Value);
            int w = m.Grid.Width;
            bool tunnel = mode is "cave" or "drill";
            // hit-…: at the edge of the hero's reach from one kind of object, striking it (0094).
            if (Hits(mode))
            {
                if (HitSpot(m, mode[4..]) is not (int stand, int struck))
                {
                    log($"ground demo: no {mode[4..]} found to strike");
                    return;
                }
                _striking = struck;
                Target = (struck % w + 0.5, struck / w + 0.5);
                client.Send(new Transfer(_planet.Value, stand));
                log($"ground demo: beamed down to ({stand % w}, {stand / w}), striking {mode[4..]} at ({struck % w}, {struck / w})");
                return;
            }
            // In a cave of a band, or a great hall, looking at its growth (0080).
            if (Caves(mode) && (mode == "lava" ? LavaSpot(m) : CaveSpot(m, mode)) is (int spot, var look))
            {
                Target = look;
                client.Send(new Transfer(_planet.Value, spot));
                log($"ground demo: beamed down to ({spot % w}, {spot / w}), looking at ({look.X:F0}, {look.Y:F0}), seed {world.Surfaces[_planet.Value].Seed}");
                return;
            }
            int at = Spires(mode) ? SpireSpot(m, mode) ?? m.Crew.Spawn : Homes(mode) ? HomeSpot(m, mode) ?? m.Crew.Spawn : tunnel ? CaveUnder(m) ?? m.Crew.Spawn : mode == "mouth" ? RimNear(m) ?? m.Crew.Spawn : mode == "sprint" ? Features.Crew.HeroAutopilot.SprintStart(m.Grid)
                : mode == "acacia" ? BesideAcacia(m) ?? m.Crew.Spawn : m.Crew.Spawn;
            // The shaft under the feet; the tunnel at head height the way the hero faces, right.
            for (int i = 1; i <= (tunnel ? Along : Depth) && mode is not ("acacia" or "sprint" or "pit") && !Spires(mode) && !Homes(mode); i++) _cells.Add(tunnel ? at - w + i : at + i * w);
            // pit: a pit under the feet, a block of its ground left standing on its floor (0088).
            for (int dy = 1; dy <= PitDepth && mode == "pit"; dy++)
                for (int dx = -PitSide; dx <= PitSide; dx++)
                    if ((dx, dy) != (PitBlock, PitDepth)) _cells.Add(at + dy * w + dx);
            // spire-climb: under the light, its right plug dug open for the climb out (0096).
            if (mode == "spire-climb")
                for (int dy = 0; dy < Spec.Ruins.Spire.Count; dy++)
                    if (Spec.Ruins.Spire[dy].LastIndexOf(Spec.Ruins.Plug) is int px and >= 0)
                        _cells.Add((at / w - (Spec.Ruins.Spire.Count - 3) + dy) * w + at % w - 4 + px);
            // --ground-offset N: stood N columns off where the mode stands it, for a recording (0096).
            at += Offset;
            client.Send(new Transfer(_planet.Value, at));
            log($"ground demo: beamed down to ({at % w}, {at / w})");
            return;
        }
        if (world.SurfaceOf(client.Id) == _planet && (Spires(mode) || Homes(mode)) && TakeAt is int take && world.Tick >= take && world.Tick - _hits >= Pace)
        {
            // --take-at: the nearest spire's relic, or home's chip, struck till it is taken, an axe's swing at a time (0096).
            _hits = world.Tick;
            var m = Registry.On(world, _planet.Value);
            int sx = m.Crew.HeroOf(client.Id)?.Cell % m.Grid.Width ?? 0;
            string kind = Spires(mode) ? Spec.Ruins.Cradle : Spec.Ruins.Niche;
            if (m.Building.Relics.Where(r => r.Kind == kind).OrderBy(r => Math.Abs(r.Cell % m.Grid.Width - sx)).FirstOrDefault() is { Taken: false } relic)
                client.Send(new Dig(relic.Cell, Spec.Weapons.Axe.Item));
        }
        if (world.SurfaceOf(client.Id) != _planet || mode is "mouth" or "acacia" or "sprint" || Caves(mode) || (Spires(mode) && mode != "spire-climb") || Homes(mode)) return;
        var here = Registry.On(world, _planet.Value);
        if (Hits(mode))
        {
            // Struck till it breaks, then the next of its kind as far off within reach.
            if (!Is(here, _striking, mode[4..]) && here.Crew.HeroOf(client.Id) is { } hero)
                _striking = Within(here, hero.Cell, mode[4..]) ?? -1;
            Target = _striking >= 0 ? (_striking % here.Grid.Width + 0.5, _striking / here.Grid.Width + 0.5) : null;
            return;
        }
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


    /// <summary>The cell <c>hit-…</c> strikes now; -1 once none of its kind is left within reach.</summary>
    private int _striking = -1;

    private bool _planned;

    /// <summary>
    /// Whether <paramref name="mode"/> strikes one kind of object from the edge of the hero's reach (0094): <c>hit-soil</c>,
    /// <c>hit-stone</c>, <c>hit-alloy</c> (a natural wall of it, open to the air), <c>hit-tree</c> (an acacia), <c>hit-crystal</c>
    /// (a cave's crystal); with the drill, or with <c>--axe</c> the axe.
    /// </summary>
    public static bool Hits(string mode) => mode.StartsWith("hit-", StringComparison.Ordinal);

    /// <summary>Whether <paramref name="cell"/> holds <paramref name="what"/> to strike, as <see cref="Hits"/> names it.</summary>
    private static bool Is(Registry m, int cell, string what)
    {
        if (cell < 0) return false;
        if (what is "tree" or "crystal")
            return m.Grid.WallAt(cell) == null && m.Building.PlantAt(cell)?.Kind is { } kind && (what == "tree" ? kind == "acacia" : kind.StartsWith("crystal", StringComparison.Ordinal));
        if (m.Grid.WallAt(cell) is not { Natural: true } wall || wall.Id != what) return false;
        int w = m.Grid.Width;
        return new[] { cell - w, cell + w, cell - 1, cell + 1 }.Any(c => c >= 0 && c < w * m.Grid.Height && m.Grid.StructureAt(c) == Structure.Empty);
    }

    /// <summary>The body's middle of a hero standing in <paramref name="cell"/>, in cells.</summary>
    private static (double X, double Y) BodyOf(int cell, int w) => (cell % w + 0.5, cell / w + 1 - Spec.Hero.Height / 2000.0);

    private static double Distance((double X, double Y) a, int cell, int w) =>
        Math.Sqrt((a.X - cell % w - 0.5) * (a.X - cell % w - 0.5) + (a.Y - cell / w - 0.5) * (a.Y - cell / w - 0.5));

    /// <summary>
    /// The nearest <paramref name="what"/> to the spawn's column with a place to stand on the ground beside it at the edge of the
    /// hero's reach, 4.6 to 5.8 cells from its body to the cell's middle, as level with it as can be; and that place and the cell.
    /// </summary>
    private static (int Stand, int Struck)? HitSpot(Registry m, string what)
    {
        var grid = m.Grid;
        int w = grid.Width, h = grid.Height, sx = m.Crew.Spawn % w;
        bool Open(int x, int y) => x > 0 && y > 0 && x < w - 1 && y < h - 1 && grid.StructureAt(y * w + x) == Structure.Empty;
        bool Stands(int x, int y) => Open(x, y) && Open(x, y - 1) && Open(x, y - 2) && grid.WallAt((y + 1) * w + x) != null;
        for (int d = 0; d < Math.Min(w / 2, 600); d++)
            foreach (int x in d == 0 ? new[] { sx } : new[] { sx + d, sx - d })
                for (int y = 2; y < h - 2 && x > 8 && x < w - 8; y++)
                {
                    int cell = y * w + x;
                    if (!Is(m, cell, what)) continue;
                    (int Stand, double Score)? best = null;
                    for (int dy = -6; dy <= 6; dy++)
                        for (int dx = -7; dx <= 7; dx++)
                        {
                            if (!Stands(x + dx, y + dy)) continue;
                            int stand = (y + dy) * w + x + dx;
                            var body = BodyOf(stand, w);
                            double far = Distance(body, cell, w);
                            if (far < 4.6 || far > 5.8) continue;
                            double score = far - 0.6 * Math.Abs(body.Y - (y + 0.5));
                            if (best == null || score > best.Value.Score) best = (stand, score);
                        }
                    if (best is { } found) return (found.Stand, cell);
                }
        return null;
    }

    /// <summary>The <paramref name="what"/> farthest from a hero in <paramref name="cell"/> still within its reach, past four cells; none for none.</summary>
    private static int? Within(Registry m, int cell, string what)
    {
        int w = m.Grid.Width;
        var body = (cell % w + 0.5, cell / w + 0.5);
        return Enumerable.Range(-6, 13).SelectMany(dy => Enumerable.Range(-6, 13).Select(dx => cell + dy * w + dx))
            .Where(c => Is(m, c, what) && Distance(body, c, w) is >= 4 and <= 5.6)
            .OrderByDescending(c => Distance(body, c, w)).Select(c => (int?)c).FirstOrDefault();
    }

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


    private static int TopOf(Registry m, int x) =>
        Enumerable.Range(0, m.Grid.Height).First(y => m.Grid.StructureAt(y * m.Grid.Width + x) != Structure.Empty);

    /// <summary>
    /// The open cell on the rim of the pit, chasm or cave mouth nearest the spawn: a column whose ground lies at least eight
    /// rows under that of the columns four either side, the hero standing on the higher of them.
    /// </summary>
    private static int? RimNear(Registry m)
    {
        var grid = m.Grid;
        int w = grid.Width, sx = m.Crew.Spawn % w;
        int Top(int x) => Enumerable.Range(0, grid.Height).First(y => grid.StructureAt(y * w + Math.Clamp(x, 1, w - 2)) != Structure.Empty);
        return Enumerable.Range(0, w / 2 - 8).SelectMany(d => new[] { sx + d, sx - d })
            .Where(x => x > 8 && x < w - 8 && Top(x) >= Math.Max(Top(x - 4), Top(x + 4)) + 8)
            .Select(x => (int?)((Top(Top(x - 4) < Top(x + 4) ? x - 4 : x + 4) - 1) * w + (Top(x - 4) < Top(x + 4) ? x - 4 : x + 4)))
            .FirstOrDefault();
    }

    /// <summary>Whether <paramref name="mode"/> stands the hero at a spire (0085): <c>spire</c>, <c>spire-in</c> or <c>spire-far</c>.</summary>
    public static bool Spires(string mode) => mode is "spire" or "spire-in" or "spire-far" or "spire-climb";

    /// <summary>
    /// The cell to stand in at the spire nearest the spawn, found by its lamp in its tip's top cell: on its roof beside its left plug,
    /// in its chamber beside its cache, or on the ground forty columns left of it.
    /// </summary>
    private static int? SpireSpot(Registry m, string mode)
    {
        int w = m.Grid.Width, sx = m.Crew.Spawn % w, rise = ((Spec.PlanetType)m.Rules).Ground.SpireRise;
        var lamp = m.Building.Plants.Where(p => p.Kind == Spec.Ruins.SpireLamp).OrderBy(p => Math.Abs(p.Cell % w - sx)).FirstOrDefault();
        if (lamp == null) return null;
        int x0 = lamp.Cell % w - Spec.Ruins.Spire[0].IndexOf(Spec.Ruins.Lamp), y0 = lamp.Cell / w;
        if (mode == "spire-far") return (TopOf(m, x0 - 40) - 1) * w + x0 - 40;
        return mode == "spire" ? (y0 + rise - 1) * w + x0 + 1 : (y0 + Spec.Ruins.Spire.Count - 3) * w + x0 + (mode == "spire-climb" ? 4 : 2);
    }

    /// <summary>Whether <paramref name="mode"/> stands the hero in a home of the arc makers (0091): <c>home-hearth</c>, <c>home-bedroom</c>, <c>home-study</c>.</summary>
    public static bool Homes(string mode) => Spec.Ruins.Homes.Any(h => mode == $"home-{h.Id}");

    /// <summary>
    /// The cell to stand in in the home of <paramref name="mode"/>'s scheme nearest the spawn, found by a piece of its own no other
    /// scheme has: on its floor in its middle, the whole room in view.
    /// </summary>
    private static int? HomeSpot(Registry m, string mode)
    {
        var home = Spec.Ruins.Homes.First(h => mode == $"home-{h.Id}");
        var own = home.Pieces.First(p => Spec.Ruins.Homes.Where(o => o != home).All(o => o.Pieces.All(q => q.Kind != p.Kind)));
        int w = m.Grid.Width, sx = m.Crew.Spawn % w;
        var piece = m.Building.Plants.Where(p => p.Kind == own.Kind).OrderBy(p => Math.Abs(p.Cell % w - sx)).FirstOrDefault();
        if (piece == null) return null;
        int x0 = piece.Cell % w - own.X, y0 = piece.Cell / w - own.Y;
        return (y0 + home.Tall - 2) * w + x0 + home.Wide / 2;
    }

    /// <summary>Whether <paramref name="mode"/> stands the hero in a cave of a band, <c>band0</c> to <c>band3</c>, a great hall, <c>hall</c> (0080), or by the lava, <c>lava</c>.</summary>
    public static bool Caves(string mode) => mode is "band0" or "band1" or "band2" or "band3" or "hall" or "lava";

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
        var planet = (Spec.PlanetType)m.Rules;
        var weight = new Dictionary<int, int>();
        foreach (var p in m.Building.Plants)
            if (CaveLooks.Of.TryGetValue(p.Kind, out var look)) weight[p.Cell] = look.Light != null ? 3 : 1;
        bool Open(int x, int y) => x > 0 && y > 0 && x < w && y < h && grid.StructureAt(y * w + x) == Structure.Empty;
        (int Score, int Far, int Cell, double X, double Y) best = (0, int.MaxValue, -1, 0, 0);
        for (int y = 3; y < h - 4; y++)
        {
            if (Terrain.BandOf(planet, y) != band) continue;
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

    /// <summary>
    /// With <c>lava</c>: a cell on a whole floor of stone with room above, within twenty rows over the lava and two to fourteen
    /// columns beside a cell of its surface open above, the nearest the spawn's column; and that open lava's middle, in cells.
    /// </summary>
    private static (int Cell, (double X, double Y) Look)? LavaSpot(Registry m)
    {
        var grid = m.Grid;
        int w = grid.Width, h = grid.Height, sx = m.Crew.Spawn % w, top = h - ((Spec.PlanetType)m.Rules).Ground.CoreRows;
        bool Open(int x, int y) => x > 0 && y > 0 && x < w && y < h && grid.StructureAt(y * w + x) == Structure.Empty;
        bool Lava(int x) => Open(x, top - 1);
        foreach (int x in Enumerable.Range(0, w).OrderBy(x => Math.Abs(x - sx)).Where(Lava))
            for (int d = 2; d <= 14; d++)
                foreach (int fx in new[] { x - d, x + d })
                    for (int y = top - 2; y > top - 20; y--)
                        if (Open(fx, y) && Open(fx, y - 1) && Open(fx, y - 2) && grid.WallAt((y + 1) * w + fx) is { Id: "stone" })
                            return (y * w + fx, (x + 0.5, top + 0.5));
        return null;
    }

    /// <summary>The open cell nearest under the spawn, at least twenty rows down, standing on ground with room above: in a cave.</summary>
    private static int? CaveUnder(Registry m)
    {
        var grid = m.Grid;
        int w = grid.Width, spawn = m.Crew.Spawn, sx = spawn % w, sy = spawn / w;
        bool Open(int x, int y) => x > 0 && y > 0 && x < w && y < grid.Height && grid.StructureAt(y * w + x) == Structure.Empty;
        return Enumerable.Range(sy + 20, grid.Height - sy - 24)
            .SelectMany(y => Enumerable.Range(-60, 121).Select(dx => (X: sx + dx, Y: y)))
            .Where(p => Open(p.X, p.Y) && Open(p.X, p.Y - 1) && Open(p.X, p.Y - 2) && !Open(p.X, p.Y + 1) && !Open(p.X + 1, p.Y - 1))
            .OrderBy(p => Math.Abs(p.X - sx) + 2 * (p.Y - sy))
            .Select(p => (int?)(p.Y * w + p.X))
            .FirstOrDefault();
    }
}
