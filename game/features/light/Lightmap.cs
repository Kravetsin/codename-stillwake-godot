using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// All the light of lamps, fires and glows, composed on the GPU each frame into two maps over the view
/// (0029): <see cref="Main"/> for lamps and fires, <see cref="Spots"/> for embers, sparks and the like,
/// capped apart by <see cref="Lit"/>. Each light is one additive quad over its reach whose shader walks
/// the cells from each texel to the light and drops the texel if a wall or a closed door's or hatch's panel stands
/// between; a light with a size is seen from the texel as a line across that wide, and where its two
/// ends disagree, in a shadow's edge, more points along it are walked and the share seen kept: soft
/// edges where Godot's PCF gave them. A wall is lit on the faces the light reaches, and no limit on how many
/// lights touch a thing, as Godot's 2D lights have. A texel a world pixel, or more pixels a texel when
/// the view is too big for <see cref="MostTexels"/> across.
/// </summary>
public partial class Lightmap : Node
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The most texels a map has across: past it, a texel covers more pixels of the world.</summary>
    private const int MostTexels = 4096;
    /// <summary>Cells of map beyond the view, so a frame's lag in the camera shows no dark edge.</summary>
    private const int Margin = 8;

    private static readonly Shader Shader = new()
    {
        Code = $$"""
            shader_type canvas_item;
            render_mode unshaded, blend_add;
            // A texel a cell: 0 open, a quarter a closed hatch, half a closed door, whole a wall, 33 to 36 of 255 a wall cut to a shape; over what is built, its first texel at cell origin.
            uniform sampler2D occupancy : filter_nearest, repeat_disable;
            uniform ivec2 origin = ivec2(0);
            // The columns of its cell a closed door's panel fills, and the rows a closed hatch's does, in pixels: from, to.
            uniform vec2 panel = vec2(6.0, 10.0);
            uniform vec2 hatch = vec2(6.0, 10.0);
            // A beam's half width, as the cosine of its half angle, and the soft edge either side of it (0077: the helmet lamp).
            uniform float beam_cos = 0.866;
            uniform float beam_soft = 0.08;
            // What light keeps of itself a cell of the way into solid ground (0077): it fades within four to six cells.
            uniform float wall_keep = 0.592;
            // Under a planet (0080): light reaches the open behind solid ground too, keeping wall_keep a cell of it, so a shadow cast by
            // rock fades in as the rock between thickens rather than starting along the rock's face. Aboard, a wall stops it.
            uniform bool soak_through = false;
            const float CELL = {{Cell}}.0;
            varying vec2 world;
            varying flat vec2 centre;
            varying flat vec4 light;
            varying flat vec4 shape;

            void vertex() {
                world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
                // reach in cells, keep a cell, most a colour, size in pixels; the quad's UV runs -1..1 across its reach.
                shape = INSTANCE_CUSTOM;
                centre = world - UV * shape.x * CELL;
                light = COLOR;
            }

            // 2 a wall, 1 a closed door, 3 a closed hatch, 0 open, past what is built too; 4 to 7 a wall cut to ◢ ◣ ◤ ◥ (0069).
            int kind(ivec2 c) {
                c -= origin;
                ivec2 size = textureSize(occupancy, 0);
                if (c.x < 0 || c.y < 0 || c.x >= size.x || c.y >= size.y) return 0;
                float v = texelFetch(occupancy, c, 0).r;
                return v > 0.75 ? 2 : (v > 0.375 ? 1 : (v > 0.19 ? 3 : (v > 0.1 ? int(v * 255.0 + 0.5) - 29 : 0)));
            }

            // Whether point p, in cells from a shaped wall's corner, lies in its solid half: kind 4 ◢, 5 ◣, 6 ◤, 7 ◥.
            bool solid(int k, vec2 p) {
                return k == 4 ? p.x + p.y >= 1.0 : (k == 5 ? p.y >= p.x : (k == 6 ? p.x + p.y <= 1.0 : p.y <= p.x));
            }

            // How much of the light reaches a point, cell by cell along the line: none when a wall or a door's or hatch's panel
            // stands between with the open on both sides of it. The walls the point is in are passed until the line comes out
            // of them, keeping wall_keep a cell of the way through them (0077), so light goes into solid ground and fades
            // there smoothly, cell edges or not; so is the panel it is on, along the doorway's cells. A point beside a panel
            // in its cell is shadowed by it.
            float through(vec2 from, vec2 to) {
                vec2 a = from / CELL, d = to / CELL - a;
                ivec2 c = ivec2(floor(a)), end = ivec2(floor(to / CELL));
                ivec2 stp = ivec2(sign(d));
                vec2 delta = vec2(d.x != 0.0 ? abs(1.0 / d.x) : 1e30, d.y != 0.0 ? abs(1.0 / d.y) : 1e30);
                vec2 next = vec2(
                    d.x > 0.0 ? (floor(a.x) + 1.0 - a.x) * delta.x : (d.x < 0.0 ? (a.x - floor(a.x)) * delta.x : 1e30),
                    d.y > 0.0 ? (floor(a.y) + 1.0 - a.y) * delta.y : (d.y < 0.0 ? (a.y - floor(a.y)) * delta.y : 1e30));
                bool leaving = true;
                // How far, in cells, the line runs through the walls the point is in before it comes out of them.
                float inside = 0.0, span_cells = length(d);
                float t0 = 0.0;
                // As far as the light reaches at its most, twice the 16 px cells it was tuned in (0104).
                for (int i = 0; i < 160; i++) {
                    float t1 = min(min(next.x, next.y), 1.0);
                    int k = kind(c);
                    if (k == 2) {
                        if (!leaving && !soak_through) return 0.0;
                        inside += (t1 - t0) * span_cells;
                    } else if (k >= 4) {
                        // A wall cut to a shape: its solid half as a wall, passed till the line comes out of it, its open
                        // half as the open. The solid is a half plane in the cell, so the line's two ends there tell it.
                        bool in0 = solid(k, a + d * t0 - vec2(c)), in1 = solid(k, a + d * t1 - vec2(c));
                        if (leaving && in0) {
                            leaving = in1;
                            inside += (t1 - t0) * span_cells * (in1 ? 1.0 : 0.5);
                        } else {
                            leaving = false;
                            if (in0 || in1) {
                                if (!soak_through) return 0.0;
                                inside += (t1 - t0) * span_cells * (in0 && in1 ? 1.0 : 0.5);
                            }
                        }
                    } else if (k == 1 || k == 3) {
                        // A door's panel spans columns, a hatch's rows: where the line is across that, in pixels.
                        bool across = k == 3;
                        vec2 span = across ? hatch : panel;
                        float x0 = across ? (a.y + d.y * t0 - float(c.y)) * CELL : (a.x + d.x * t0 - float(c.x)) * CELL;
                        float x1 = across ? (a.y + d.y * t1 - float(c.y)) * CELL : (a.x + d.x * t1 - float(c.x)) * CELL;
                        bool on = x0 >= span.x && x0 <= span.y;
                        if (leaving && on) {
                            leaving = x1 >= span.x && x1 <= span.y;
                        } else {
                            leaving = false;
                            if (max(min(x0, x1), span.x) < min(max(x0, x1), span.y)) return 0.0;
                        }
                    } else {
                        leaving = false;
                    }
                    if (c == end || t1 >= 1.0) break;
                    if (next.x < next.y) {
                        c.x += stp.x;
                        t0 = next.x;
                        next.x += delta.x;
                    } else {
                        c.y += stp.y;
                        t0 = next.y;
                        next.y += delta.y;
                    }
                }
                return pow(wall_keep, inside);
            }

            void fragment() {
                float reach = shape.x;
                float dist = length(world - centre) / CELL;
                if (dist >= reach) discard;
                // As the TypeScript game spread light: keep a cell, faded out over the last cells.
                float fall = pow(shape.y, dist) * clamp((reach - dist) / min(8.0, reach / 2.0), 0.0, 1.0);
                // Most past 1 marks a beam: 2 and its heading as a share of a turn, its most whole. Off its way, it fades out.
                float most = shape.z;
                if (most > 1.5) {
                    float heading = (most - 2.0) * 6.2831853;
                    vec2 to = world - centre;
                    float far = length(to);
                    float along = far > 0.5 ? dot(to / far, vec2(cos(heading), sin(heading))) : 1.0;
                    fall *= smoothstep(beam_cos - beam_soft, beam_cos + beam_soft, along);
                    most = 1.0;
                }
                if (fall <= 0.0) discard;
                // The light seen from here: a line across the way to it, size wide; its ends first.
                vec2 across = normalize(vec2(centre.y - world.y, world.x - centre.x) + vec2(1e-6, 0.0)) * shape.w * 0.5;
                float seen;
                if (shape.w < 1.0) {
                    seen = through(world, centre);
                } else {
                    float e0 = through(world, centre - across), e1 = through(world, centre + across);
                    seen = (e0 + e1) * 0.5;
                    if ((e0 > 0.0) != (e1 > 0.0)) {
                        // A shadow's edge: the points between the ends too.
                        float lit = e0 + e1;
                        for (int s = 1; s < 8; s++) lit += through(world, centre + across * (float(s) / 4.0 - 1.0));
                        seen = lit / 9.0;
                    }
                }
                if (seen <= 0.0) discard;
                COLOR = vec4(min(vec3(most), light.rgb * fall) * light.a * seen, 1.0);
            }
            """,
    };

    private readonly SubViewport _main = Map(), _spots = Map();
    private readonly SubViewport[] _maps;
    private readonly MultiMesh _mainLights = Lights(), _spotLights = Lights();
    private readonly ShaderMaterial _material = new() { Shader = Shader };
    private float[] _mainBuffer = [], _spotBuffer = [];
    private byte[] _cells = [];
    /// <summary>The cells the occupancy texture covers: what was built when it was last laid out whole.</summary>
    private Box _occupied = Box.None;
    private Image? _occupancy;
    private ImageTexture? _occupancyTexture;

    public Lightmap()
    {
        _maps = [_main, _spots];
        foreach (var (map, lights) in new[] { (_main, _mainLights), (_spots, _spotLights) })
        {
            AddChild(map);
            map.AddChild(new MultiMeshInstance2D { Multimesh = lights, Material = _material });
        }
        Lit.ShowLightmaps(_main.GetTexture(), _spots.GetTexture());
    }

    /// <summary>
    /// Whether light reaches the open past solid ground, keeping <c>wall_keep</c> a cell of rock between (a planet's, 0080): a
    /// shadow cast by rock fades in, not along the rock's face; aboard, walls stop it.
    /// </summary>
    public bool SoakThrough { set => _material.SetShaderParameter("soak_through", value); }

    /// <summary>The lights drawn the last frame, both maps together: what the dev readout shows.</summary>
    public int Drawn { get; private set; }

    /// <summary>
    /// Where light stops, from the grid's structure a texel a cell over <paramref name="built"/>, what is
    /// built: past it every cell is open. <paramref name="panels"/> the door panel's columns and the hatch panel's rows in pixels.
    /// </summary>
    public void Occupy(Func<int, int> structureAt, Func<int, int> shapeAt, int width, int size, Box built, Panels panels)
    {
        // Nothing built: one open texel, for a texture cannot be empty.
        if (built.IsEmpty) built = new Box(0, 0, 0, 0);
        _occupied = built;
        int w = built.Width, h = built.Height;
        if (_cells.Length != w * h) _cells = new byte[w * h];
        Sources.Occupy(structureAt, shapeAt, width, size, built, _cells);
        Upload(built, panels);
    }

    /// <summary>
    /// After the structure of <paramref name="changed"/> cells moved: their texels and those above and below, whose
    /// doors may have turned hatches or back (0067), set again, or, when one lies
    /// past the texture, the whole of what is built laid out anew (<see cref="Occupy"/>). The texture may cover
    /// more than is built: past what is built every cell is open, inside the texture or past it alike.
    /// </summary>
    public void Follow(Func<int, int> structureAt, Func<int, int> shapeAt, int width, int size, Box built, IReadOnlyList<int> changed, Panels panels)
    {
        var box = _occupied;
        foreach (int c in changed)
            if (_occupancy == null || !box.Contains(c % width, c / width))
            {
                Occupy(structureAt, shapeAt, width, size, built, panels);
                return;
            }
        foreach (int changedCell in changed)
            for (int c = changedCell - width; c <= changedCell + width; c += width)
                if (c >= 0 && c < size && box.Contains(c % width, c / width))
                    _cells[(c / width - box.Top) * box.Width + c % width - box.Left] = Sources.Of(structureAt, shapeAt, width, size, c);
        Upload(box, panels);
    }

    /// <summary>What of a closed doorway stops light, in pixels of its cell: a door's panel's columns, a hatch's panel's rows.</summary>
    public readonly record struct Panels((int From, int To) Door, (int From, int To) Hatch);

    private void Upload(Box built, Panels panels)
    {
        int w = built.Width, h = built.Height;
        if (_occupancy != null && _occupancyTexture != null && _occupancy.GetWidth() == w && _occupancy.GetHeight() == h)
        {
            _occupancy.SetData(w, h, false, Image.Format.R8, _cells);
            _occupancyTexture.Update(_occupancy);
        }
        else
        {
            _occupancy = Image.CreateFromData(w, h, false, Image.Format.R8, _cells);
            _occupancyTexture = ImageTexture.CreateFromImage(_occupancy);
            _material.SetShaderParameter("occupancy", _occupancyTexture);
        }
        _material.SetShaderParameter("origin", new Vector2I(built.Left, built.Top));
        _material.SetShaderParameter("panel", new Vector2(panels.Door.From, panels.Door.To));
        _material.SetShaderParameter("hatch", new Vector2(panels.Hatch.From, panels.Hatch.To));
        _material.SetShaderParameter("beam_cos", (float)Math.Cos(L.HeadLampHalfAngle));
    }

    /// <summary>Lays both maps over <paramref name="view"/> (world pixels) and draws <paramref name="main"/> and <paramref name="spots"/> into them.</summary>
    public void Frame(Rect2 view, IReadOnlyList<LightSource> main, IReadOnlyList<LightSource> spots)
    {
        var area = view.Grow(Margin * Cell);
        int texel = Math.Max(1, (int)Math.Ceiling(Math.Max(area.Size.X, area.Size.Y) / MostTexels));
        var origin = (area.Position / texel).Floor() * texel;
        // From the view's size alone, a texel spare for where the origin falls in one: the same while the camera moves, for a new size
        // makes both maps' textures anew, some 4 ms: sized to where the view fell on the pixels, it changed by a texel nearly every frame.
        var size = new Vector2I(Spare(area.Size.X / texel), Spare(area.Size.Y / texel));
        var place = new Transform2D(0, new Vector2(1f / texel, 1f / texel), 0, -origin / texel);
        foreach (var map in _maps)
        {
            if (map.Size != size) map.Size = size;
            map.CanvasTransform = place;
        }
        var box = ((double)origin.X, (double)origin.Y, (double)size.X * texel, (double)size.Y * texel);
        Drawn = Draw(_mainLights, main, box, ref _mainBuffer) + Draw(_spotLights, spots, box, ref _spotBuffer);
        Lit.PlaceLightmaps(origin, texel);
    }

    /// <summary>Texels enough for <paramref name="texels"/> wherever the origin falls; a float a hair over a whole number is that number.</summary>
    private static int Spare(float texels) => (int)Math.Ceiling(texels - 0.01f) + 1;

    private static int Draw(MultiMesh lights, IReadOnlyList<LightSource> sources, (double, double, double, double) box, ref float[] buffer)
    {
        int n = Sources.Pack(sources, box, Cell, ref buffer);
        int capacity = buffer.Length / Sources.Stride;
        if (lights.InstanceCount != capacity) lights.InstanceCount = capacity;
        if (n > 0) lights.Buffer = buffer;
        lights.VisibleInstanceCount = n;
        return n;
    }

    private static SubViewport Map() => new()
    {
        TransparentBg = true,
        UseHdr2D = true,
        Disable3D = true,
        RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
        Size = new Vector2I(16, 16),
    };

    /// <summary>A multimesh of quads -1..1 across, their UV the same, each with a colour and custom data.</summary>
    private static MultiMesh Lights()
    {
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        Vector2[] corners = [new(-1, -1), new(1, -1), new(1, 1), new(-1, 1)];
        arrays[(int)Mesh.ArrayType.Vertex] = corners;
        arrays[(int)Mesh.ArrayType.TexUV] = corners;
        arrays[(int)Mesh.ArrayType.Index] = new[] { 0, 1, 2, 0, 2, 3 };
        var quad = new ArrayMesh();
        quad.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            UseCustomData = true,
            Mesh = quad,
            // Godot culls a multimesh by the box its quads filled when it was first drawn, and keeps
            // that box: one first drawn empty, as the spots are before anything glows, never showed.
            CustomAabb = new Aabb(new Vector3(-1e7f, -1e7f, -1), new Vector3(2e7f, 2e7f, 2)),
        };
    }
}
