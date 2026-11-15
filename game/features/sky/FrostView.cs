using System.Collections.Generic;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Sky;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Sky;

/// <summary>
/// The frost on the hull before the ice belt (docs/plans/stretches.md, the pick «Кромка инея»): over every wall cell facing space a tile
/// of rime by its open sides, one mesh of them all, lit as the walls are. Its shader grows each tile's band from the edge by the
/// warning's run and the cell's turn (<see cref="Frost"/>), holds it while the belt runs, then darkens and melts it back to the edge,
/// leaving the hull wet a while. The cells are found as the surface loads and kept by the grid's changes; a frame sets four numbers.
/// </summary>
public partial class FrostView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The sheet's tiles (tools/anime-art/subjects/stretches.js): slots this wide, a tile this far in, this many of them.</summary>
    private const int Slot = 16, Clear = 4, Slots = 30;

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

        // Whether a point `depth` in from an edge is under a band whose turn came at `delay`: softened over a third of a pixel.
        float frost_band(float depth, float delay) {
            float reach = {{Frost.Deepest:0.0###}} * frost_amount(delay);
            return reach <= 0.0 ? 0.0 : 1.0 - smoothstep(reach - 0.3, reach, depth);
        }
        """);

    private const string Vertex = """

        void vertex() {
            world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
            frost_tint = COLOR;
        }
        """;

    // A quad's UV carries its cell: x its corner's 0 or 1 and twice its sides open, y its corner's and twice its turn in hundredths.
    private static readonly string Paint = System.FormattableString.Invariant($$"""
            {
                float sides_at = floor(UV.x * 0.5), turn_at = floor(UV.y * 0.5);
                int sides = int(sides_at);
                float delay = turn_at / 100.0;
                vec2 local = vec2(UV.x - sides_at * 2.0, UV.y - turn_at * 2.0) * {{Cell}}.0;
                vec2 frost_cell = floor((world - local) / {{Cell}}.0 + 0.5);
                int slot = 2 * (sides - 1) + int(mod(frost_cell.x + frost_cell.y, 2.0));
                vec4 rime = texture(TEXTURE, vec2((float(slot) * {{Slot}}.0 + {{Clear}}.0 + local.x) / {{Slot * Slots}}.0, ({{Clear}}.0 + local.y) / {{Slot}}.0));
                float shown = 0.0;
                for (int i = 0; i < 4; i++) {
                    if ((sides & (1 << i)) == 0) continue;
                    float depth = i == 0 ? local.y : (i == 1 ? {{Cell}}.0 - local.x : (i == 2 ? {{Cell}}.0 - local.y : local.x));
                    float along = (i == 0 || i == 2) ? world.x : world.y;
                    // Each four pixels along the hull a little apart in its turn, as the pick's mask.
                    shown = max(shown, frost_band(depth, delay + 0.04 * frost_hash(floor(along / 4.0) + float(i) * 101.0)));
                }
                // Its outer corners first and thickest.
                for (int i = 0; i < 4; i++) {
                    int pair = (1 << i) | (1 << ((i + 1) % 4));
                    if ((sides & pair) != pair) continue;
                    vec2 corner = i == 0 ? vec2({{Cell}}.0, 0.0) : (i == 1 ? vec2({{Cell}}.0) : (i == 2 ? vec2(0.0, {{Cell}}.0) : vec2(0.0)));
                    shown = max(shown, frost_band(distance(local, corner), delay));
                }
                // Where it has melted, the hull wet a while; where it shows, darkening as it melts.
                vec3 thawed = mix(rime.rgb, vec3(40.0, 62.0, 80.0) / 255.0, 0.35 * frost_dark);
                vec4 wet = vec4(10.0, 16.0, 24.0, 255.0) / 255.0;
                COLOR = frost_tint * mix(vec4(wet.rgb, rime.a * frost_wet), vec4(thawed, rime.a), shown);
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

    /// <summary>The mesh made again from the hull's cells: a quad a cell, its turn and sides in its UV (see <see cref="Paint"/>).</summary>
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
            float u = 2 * sides, v = 2 * Mathf.Round(delays[cell] * 100);
            for (int k = 0; k < 4; k++)
            {
                int cx = k is 1 or 2 ? 1 : 0, cy = k >= 2 ? 1 : 0;
                vertices[i * 4 + k] = new Vector2((x + cx) * Cell, (y + cy) * Cell);
                uvs[i * 4 + k] = new Vector2(u + cx, v + cy);
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
