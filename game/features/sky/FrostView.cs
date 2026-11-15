using System.Collections.Generic;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Sky;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Sky;

/// <summary>
/// The frost on the hull before the ice belt (docs/decisions/2026-11-15-stretches-of-space.md, the pick «Кромка инея»): over every wall cell facing space a tile
/// of rime by its open sides, one mesh of them all, lit as the walls are. Its shader grows each tile's band from the edge by the
/// warning's run and the cell's turn (<see cref="Frost"/>), holds it while the belt runs, then darkens and melts it back to the edge,
/// leaving the hull wet a while. The cells are found as the surface loads and kept by the grid's changes; a frame sets four numbers.
/// </summary>
public partial class FrostView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The sheet's tiles (tools/anime-art/subjects/stretches.js): slots this wide, a tile this far in, this many of them.</summary>
    private const int Slot = 16, Clear = 4, Slots = 30;
    /// <summary>How far a cell's quad reaches past it, in pixels of the world: room for the rime standing proud of the wall.</summary>
    private const float Margin = 2;

    private static readonly string Declarations = System.FormattableString.Invariant($$"""

        uniform float frost_grow = 0.0;
        uniform float frost_melt = 0.0;
        uniform float frost_dark = 0.0;
        uniform float frost_wet = 0.0;
        varying vec4 frost_tint;

        float frost_amount(float delay) {
            return clamp((frost_grow - delay) / {{Frost.Span:0.0###}}, 0.0, 1.0)
                * (1.0 - clamp((frost_melt - ({{Frost.Latest:0.0###}} - delay)) / {{Frost.Span:0.0###}}, 0.0, 1.0));
        }

        float frost_hash(float n) {
            return fract(sin(n * 12.9898) * 43758.5453);
        }

        // Between least and most at `along` on the hull, a point each few pixels: the same in every cell it crosses, so an edge runs on unbroken.
        float frost_scallop(float along, float seed, float least, float most) {
            float k = along / {{Frost.Scallop:0.0###}};
            return mix(least, most, mix(frost_hash(floor(k) + seed), frost_hash(floor(k) + 1.0 + seed), fract(k)));
        }

        // How far in from side i (up, right, down, left) a point of its cell lies; and where along the hull that side's band is.
        float frost_depth(vec2 local, int i) {
            return i == 0 ? local.y : (i == 1 ? {{Cell}}.0 - local.x : (i == 2 ? {{Cell}}.0 - local.y : local.x));
        }

        float frost_along(vec2 spot, int i) {
            return (i == 0 || i == 2) ? spot.x : spot.y;
        }

        // How much of side i's band is there at `along`: each scallop a little apart in its turn, as the pick's mask; all of it when whole.
        float frost_part(float along, int i, float delay, bool whole) {
            return whole ? 1.0 : frost_amount(delay + 0.04 * frost_hash(floor(along / {{Frost.Scallop:0.0###}}) + float(i) * 101.0));
        }

        // How far inside the rime's edge a point of a cell lies, negative outside: its bands from the open sides, standing proud of
        // them; its outer corners rounded; its inner corners filled where the bands of the two walls turn.
        float frost_inside(vec2 local, vec2 spot, int sides, int hollows, float delay, bool whole) {
            float inside = -9.0;
            bool in_cell = all(greaterThanEqual(local, vec2(0.0))) && all(lessThanEqual(local, vec2({{Cell}}.0)));
            for (int i = 0; i < 4; i++) {
                if ((sides & (1 << i)) == 0) continue;
                float depth = frost_depth(local, i), across = (i == 0 || i == 2) ? local.x : local.y;
                if (across < 0.0 || across > {{Cell}}.0 || depth > {{Cell}}.0) continue;
                float along = frost_along(spot, i), part = frost_part(along, i, delay, whole);
                if (part <= 0.0) continue;
                float reach = frost_scallop(along, float(i) * 101.0, {{Frost.Shallowest:0.0###}}, {{Frost.Deepest:0.0###}});
                float proud = frost_scallop(along, float(i) * 101.0 + 53.0, {{Frost.ProudLeast:0.0###}}, {{Frost.ProudMost:0.0###}});
                inside = max(inside, min(reach * part - depth, proud * part + depth));
            }
            float corner_amount = whole ? 1.0 : frost_amount(delay);
            for (int i = 0; i < 4; i++) {
                int j = (i + 1) % 4, pair = (1 << i) | (1 << j);
                vec2 corner = i == 0 ? vec2({{Cell}}.0, 0.0) : (i == 1 ? vec2({{Cell}}.0) : (i == 2 ? vec2(0.0, {{Cell}}.0) : vec2(0.0)));
                if ((sides & pair) == pair && corner_amount > 0.0) {
                    float away = distance(local, corner);
                    // The outer corners first and thickest; past both edges, the crust rounded round them.
                    if (in_cell) inside = max(inside, {{Frost.Corner:0.0###}} * corner_amount - away);
                    else if (frost_depth(local, i) < 0.0 && frost_depth(local, j) < 0.0)
                        inside = max(inside, {{(Frost.ProudLeast + Frost.ProudMost) / 2:0.0###}} * corner_amount - away);
                }
                if ((hollows & (1 << i)) != 0 && in_cell) {
                    float a = frost_along(spot, i), b = frost_along(spot, j);
                    float ra = frost_scallop(a, float(i) * 101.0, {{Frost.Shallowest:0.0###}}, {{Frost.Deepest:0.0###}}) * frost_part(a, i, delay, whole);
                    float rb = frost_scallop(b, float(j) * 101.0, {{Frost.Shallowest:0.0###}}, {{Frost.Deepest:0.0###}}) * frost_part(b, j, delay, whole);
                    if (ra > 0.0 && rb > 0.0) inside = max(inside, min(ra - frost_depth(local, i), rb - frost_depth(local, j)));
                }
            }
            return inside;
        }
        """);

    private const string Vertex = """

        void vertex() {
            world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
            frost_tint = COLOR;
        }
        """;

    // A quad's UV carries its cell: x four times its sides and inner corners, plus one and its corner's share of the cell; y the same
    // with its turn in hundredths. A quad reaches Margin past its cell, so a share runs a little under 0 and over 1.
    private static readonly string Paint = System.FormattableString.Invariant($$"""
            {
                float code_at = floor(UV.x * 0.25), turn_at = floor(UV.y * 0.25);
                int code = int(code_at), sides = code & 15, hollows = code >> {{Frost.HollowShift}};
                float delay = turn_at / 100.0;
                vec2 local = vec2(UV.x - code_at * 4.0 - 1.0, UV.y - turn_at * 4.0 - 1.0) * {{Cell}}.0;
                vec2 frost_cell = floor((world - local) / {{Cell}}.0 + 0.5);
                // The sheet holds only the rime's paint, read inside the cell: an inner corner's is its two walls' tile.
                int painted = sides;
                for (int i = 0; i < 4; i++)
                    if ((hollows & (1 << i)) != 0) painted |= (1 << i) | (1 << ((i + 1) % 4));
                int slot = 2 * (painted - 1) + int(mod(frost_cell.x + frost_cell.y, 2.0));
                vec2 at = clamp(local, vec2(0.0), vec2({{Cell}}.0));
                vec4 rime = texture(TEXTURE, vec2((float(slot) * {{Slot}}.0 + {{Clear}}.0 + at.x) / {{Slot * Slots}}.0, ({{Clear}}.0 + at.y) / {{Slot}}.0));
                float shown = 0.0, inside = frost_inside(local, world, sides, hollows, delay, false);
                if (inside > -0.12) shown = smoothstep(-0.12, 0.08, inside);
                // Its edges inked all round, as a pen would.
                float inked = 1.0 - smoothstep(0.22, 0.4, inside);
                // Where it has melted, the hull wet a while where the rime lay, on the wall only; where it shows, darkening as it melts.
                vec3 thawed = mix(mix(rime.rgb, vec3(40.0, 62.0, 80.0) / 255.0, 0.35 * frost_dark), vec3(26.0, 38.0, 48.0) / 255.0, 0.8 * inked);
                float lay = 0.0;
                if (frost_wet > 0.0 && local == at) lay = smoothstep(-0.12, 0.08, frost_inside(local, world, sides, hollows, delay, true));
                vec4 wet = vec4(10.0, 16.0, 24.0, 255.0) / 255.0;
                COLOR = frost_tint * mix(vec4(wet.rgb, frost_wet * lay), vec4(thawed, 1.0), shown);
            }
        """);

    private static ShaderMaterial? _material;

    private readonly IGridQueries _grid;
    private readonly ISkyQueries _sky;
    private readonly System.Func<double> _clock;
    private readonly HullCells _hull;
    private readonly StretchShown _stretch = new();
    private readonly MeshInstance2D _mesh = new();
    private int _seen, _built = -1;
    private (float, float, float, float) _set = (-1, -1, -1, -1);

    /// <param name="clock">The world's ticks now, between ticks too: what the frost melts by once the belt has gone.</param>
    public FrostView(IGridQueries grid, ISkyQueries sky, System.Func<double> clock)
    {
        (_grid, _sky, _clock) = (grid, sky, clock);
        ZIndex = Layers.Edges;
        _hull = new HullCells(grid.Width, grid.Height, IsWall, IsSpace);
        var built = grid.Built;
        if (!built.IsEmpty) _hull.Scan(built.Left - 1, built.Top - 1, built.Right + 1, built.Bottom + 1);
        _seen = grid.Version;
        _material ??= Lit.Painted(Declarations, Vertex, Paint, L.WallIn);
        _mesh.Material = _material;
        _mesh.Texture = PixelArt.Picture("frost");
        _mesh.Visible = false;
        AddChild(_mesh);
    }

    private bool IsWall(int cell) => Structures.Is(_grid.StructureAt(cell), Trait.Terrain);

    private bool IsSpace(int cell) => _grid.StructureAt(cell) == Structure.Empty && _grid.BackLayer[cell] == Back.None;

    public override void _Process(double delta)
    {
        Follow();
        var now = _sky.Now;
        _stretch.See(now.Stretch, now.Coming, now.ComingIn, _clock());
        bool shows = _stretch.Look is { Frost: true } && Frost.Shows(_stretch.Grow, _stretch.After);
        _mesh.Visible = shows;
        if (!shows) return;
        if (_built != _hull.Version) Build();
        var (dark, wet) = Frost.Thawing(_stretch.After);
        var set = (_stretch.Grow, _stretch.Melt, dark, wet);
        if (set == _set) return;
        _set = set;
        _material!.SetShaderParameter("frost_grow", set.Item1);
        _material.SetShaderParameter("frost_melt", set.Item2);
        _material.SetShaderParameter("frost_dark", set.Item3);
        _material.SetShaderParameter("frost_wet", set.Item4);
    }

    /// <summary>The hull's cells kept up with the grid: only the cells a change names and their neighbours, or all that is built if the log has run past.</summary>
    private void Follow()
    {
        int version = _grid.Version;
        if (version == _seen) return;
        var changes = _grid.ChangedSince(_seen);
        _seen = version;
        if (changes == null)
        {
            var built = _grid.Built;
            if (!built.IsEmpty) _hull.Scan(built.Left - 1, built.Top - 1, built.Right + 1, built.Bottom + 1);
            return;
        }
        foreach (var change in changes)
            if ((change.Layers & (GridLayers.Structure | GridLayers.Back)) != 0) _hull.Changed(change.Cell);
    }

    /// <summary>The mesh made again from the hull's cells: a quad a cell reaching a little past it, its turn, sides and inner corners in its UV (see <see cref="Paint"/>).</summary>
    private static readonly int[] Quad = [0, 1, 2, 0, 2, 3];

    private void Build()
    {
        _built = _hull.Version;
        var delays = Frost.Delays(_hull.Sides, _grid.Width);
        int n = _hull.Sides.Count, i = 0;
        var vertices = new Vector2[n * 4];
        var uvs = new Vector2[n * 4];
        var indices = new int[n * 6];
        foreach (var (cell, sides) in _hull.Sides)
        {
            int x = cell % _grid.Width, y = cell / _grid.Width;
            float u = 4 * sides + 1, v = 4 * Mathf.Round(delays[cell] * 100) + 1;
            for (int k = 0; k < 4; k++)
            {
                int cx = k is 1 or 2 ? 1 : 0, cy = k >= 2 ? 1 : 0;
                float px = cx == 1 ? Margin : -Margin, py = cy == 1 ? Margin : -Margin;
                vertices[i * 4 + k] = new Vector2((x + cx) * Cell + px, (y + cy) * Cell + py);
                uvs[i * 4 + k] = new Vector2(u + cx + px / Cell, v + cy + py / Cell);
            }
            for (int k = 0; k < 6; k++) indices[i * 6 + k] = i * 4 + Quad[k];
            i++;
        }
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        var mesh = new ArrayMesh();
        if (n > 0) mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        _mesh.Mesh = mesh;
    }
}
