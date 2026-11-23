using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Sim;
using L = Stillwake.Game.Light;
using Looks = Stillwake.Game.VesselLooks;

namespace Stillwake.Game.Features.Wrecked;

/// <summary>
/// What still moves on a wreck (vessel-wrecks W-3, <see cref="WreckLife"/>): its motes as one multimesh a kind and chunk, swaying in their
/// shader, so a frame costs the chunks in view and no CPU; ice and fuel glinting, embers flickering, the same way; its drifters, sparks,
/// vents and hot edges drawn each frame from the chunks in view only; its embers' light given to the lightmap.
/// </summary>
public partial class WreckLifeView : Node2D, ILightSource
{
    private const int Cell = ShipCamera.Cell;

    private readonly WreckLife _life;
    private readonly uint _hull;
    private readonly Dictionary<(int, int), List<Drifter>> _drifters = new();
    private readonly Dictionary<(int, int), List<Sparker>> _sparkers = new();
    private readonly Dictionary<(int, int), List<Vent>> _vents = new();
    private readonly Dictionary<(int, int), List<Glow>> _glows = new();
    private readonly Sketch _drifting = new(Layers.WreckMotes, lit: true), _venting = new(Layers.WreckMotes, lit: true), _sparking = new(Layers.WreckSparks);
    private readonly List<LightSource> _lights = [];
    private readonly List<(int, int)> _seen = [];
    private double _t;

    private static readonly Dictionary<Mote, (uint Colour, float Alpha)> Kinds = new()
    {
        [Mote.Dust] = (0x9a8e7e, 0.6f),
        [Mote.DustLit] = (0xe9c99a, 0.75f),
        [Mote.RockDust] = (0x5d5246, 0.75f),
        [Mote.Paint] = (0xc8ccc9, 0.45f),
        [Mote.Bit] = (0x9aa0a6, 0.85f),
        [Mote.Rivet] = (0x6a6d72, 1f),
        [Mote.Ice] = (0xe4f4ff, 0.85f),
        [Mote.IceDull] = (0xa9b4bb, 0.45f),
        [Mote.Fuel] = (0xefe4b8, 0.85f),
        [Mote.Ash] = (0x1d1916, 0.85f),
        [Mote.AshLight] = (0x5d5650, 0.7f),
        [Mote.Ember] = (0xff7a26, 1f),
    };

    /// <summary>The motion of <paramref name="built"/>'s wreck, its looks' cable ends <paramref name="ends"/> and the lamps still lit; none for an intact vessel.</summary>
    public static WreckLifeView? Of(Built? built, IReadOnlyList<(double X, double Y, double Reach)> lights)
    {
        if (built?.Wreck == null) return null;
        var scene = new WreckScene(built);
        var ends = new List<WreckLooks.End>();
        var livery = Looks.LiveriesOf(built.Vessel.Class)[Looks.LiveryOf(built.Vessel.Class, built.Vessel.Seed)];
        uint hull = built.Wreck.Scavenged ? Looks.Faded(livery).Hull : livery.Hull;
        WreckLooks.Of(scene, hull, _ => null, ends);
        return new WreckLifeView(WreckLife.Of(scene, ends, lights), hull);
    }

    private WreckLifeView(WreckLife life, uint hull)
    {
        (_life, _hull) = (life, hull);
        foreach (var (kind, (colour, alpha)) in Kinds)
            foreach (var (chunk, specks) in life.Chunks(kind)) AddChild(Motes(kind, specks, colour, alpha));
        foreach (var kind in (ReadOnlySpan<Mote>)[Mote.Ice, Mote.Fuel])
            foreach (var (chunk, specks) in life.Chunks(kind)) AddChild(Glints(specks));
        Bucket(life.Drifters, _drifters, d => (d.X, d.Y));
        Bucket(life.Sparkers, _sparkers, s => (s.X, s.Y));
        Bucket(life.Vents, _vents, v => (v.X, v.Y));
        Bucket(life.Glows, _glows, g => (g.X, g.Y));
        foreach (var sketch in (Sketch[])[_drifting, _venting])
        {
            Lit.Set(sketch);
            AddChild(sketch);
        }
        _sparking.Material = Additive;
        AddChild(_sparking);
        _drifting.Paint = PaintDrifters;
        _venting.Paint = PaintVents;
        _sparking.Paint = PaintSparks;
    }

    public IReadOnlyList<LightSource> Lights => _lights;

    private static void Bucket<T>(List<T> items, Dictionary<(int, int), List<T>> into, Func<T, (double X, double Y)> at)
    {
        foreach (var item in items)
        {
            var (x, y) = at(item);
            var key = WreckPaint.ChunkOf((int)Math.Floor(x), (int)Math.Floor(y));
            if (!into.TryGetValue(key, out var list)) into[key] = list = [];
            list.Add(item);
        }
    }

    public override void _Process(double delta)
    {
        _t += delta;
        // The chunks in view, and one round them for what sways or sprays across a chunk's edge.
        var view = GetCanvasTransform().AffineInverse() * GetViewportRect();
        int m = Content.Vessels.Margin;
        var (x0, y0) = WreckPaint.ChunkOf((int)Math.Floor(view.Position.X / Cell) - m, (int)Math.Floor(view.Position.Y / Cell) - m);
        var (x1, y1) = WreckPaint.ChunkOf((int)Math.Ceiling(view.End.X / Cell) - m, (int)Math.Ceiling(view.End.Y / Cell) - m);
        _seen.Clear();
        for (int cy = y0 - 1; cy <= y1 + 1; cy++)
            for (int cx = x0 - 1; cx <= x1 + 1; cx++) _seen.Add((cx, cy));
        _lights.Clear();
        var embers = L.Embers;
        foreach (var key in _seen)
            if (_glows.TryGetValue(key, out var glows))
                foreach (var g in glows)
                    if (g.Light)
                        _lights.Add(new LightSource((g.X + m) * Cell, (g.Y + m) * Cell, embers.Colour, (float)g.Level(_t) * embers.Strength,
                            Math.Min(embers.Reach, (int)g.Reach), embers.Keep, 1));
        foreach (var key in _seen)
            if (_sparkers.TryGetValue(key, out var sparkers))
                foreach (var s in sparkers)
                    if (s.At(_t).Flash is var f && f > 0)
                        _lights.Add(new LightSource((s.X + m) * Cell, (s.Y + m) * Cell, L.SparkFlash.Colour, (float)f / 0.3f * L.SparkFlash.Strength, L.SparkFlash.Reach,
                            L.SparkFlash.Keep, 1));
        _drifting.QueueRedraw();
        _venting.QueueRedraw();
        _sparking.QueueRedraw();
    }

    private static Vector2 Px(double x, double y) => new Vector2((float)x + Content.Vessels.Margin, (float)y + Content.Vessels.Margin) * Cell;

    private static Color Of(uint rgb, float a = 1) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);

    private static readonly Color Ink = Of(0x121418);

    private void PaintDrifters(Sketch g)
    {
        var hull = Of(_hull);
        var rib = Of(Rgba.Mix(_hull, 0x404040, 0.35));
        foreach (var key in _seen)
        {
            if (!_drifters.TryGetValue(key, out var list)) continue;
            foreach (var d in list)
            {
                var (x, y, turn) = d.At(_t);
                g.DrawSetTransform(Px(x, y), (float)turn, Vector2.One * Cell);
                switch (d.Kind)
                {
                    case DriftKind.Rock:
                        var stone = d.Shape!.Select(p => new Vector2((float)(p.X * d.Size), (float)(p.Y * d.Size))).ToArray();
                        g.DrawColoredPolygon(stone, Of(0x5c5045));
                        g.DrawColoredPolygon(stone.Where(p => p.Y > -0.1f * d.Size).ToArray() is { Length: >= 3 } low ? low : stone, Of(0x3d342d));
                        g.DrawPolyline([.. stone, stone[0]], Ink, (float)Math.Max(0.06, d.Size * 0.06));
                        break;
                    case DriftKind.Shard:
                        var shard = d.Shape!.Select(p => new Vector2((float)(p.X * d.Size), (float)(p.Y * d.Size))).ToArray();
                        g.DrawColoredPolygon(shard, d.Colour == 1 ? rib : hull);
                        g.DrawPolyline([.. shard, shard[0]], Ink, 0.08f);
                        if (d.Bent && shard.Length > 2) g.DrawLine(shard[0], shard[2], Of(0x0f1012, 0.7f), 0.06f);
                        break;
                    default:
                        g.DrawRect(new Rect2(-(float)d.Size / 2, -(float)d.Size / 2, (float)d.Size, (float)d.Size * 0.7f), Of(d.Colour == 0 ? 0x9aa0a6 : d.Colour));
                        break;
                }
            }
        }
        g.DrawSetTransform(Vector2.Zero);
    }

    private void PaintVents(Sketch g)
    {
        foreach (var key in _seen)
        {
            if (!_vents.TryGetValue(key, out var list)) continue;
            foreach (var v in list)
            {
                var (colour, _) = Kinds[v.Kind];
                foreach (var (x, y, size, alpha) in v.At(_t))
                    g.DrawRect(new Rect2(Px(x, y), Vector2.One * (float)size * Cell), Of(colour, (float)alpha));
            }
        }
    }

    private void PaintSparks(Sketch g)
    {
        var glow = Of(0xffd890);
        var hot = Of(0xff6a1e);
        foreach (var key in _seen)
        {
            if (_glows.TryGetValue(key, out var glows))
                foreach (var h in glows)
                    if (!h.Light)
                    {
                        g.DrawSetTransform(Px(h.X, h.Y), 0, new Vector2(0.45f, 0.3f) * Cell);
                        g.DrawCircle(Vector2.Zero, 1, hot with { A = (float)h.Level(_t) });
                    }
            g.DrawSetTransform(Vector2.Zero);
            if (!_sparkers.TryGetValue(key, out var list)) continue;
            foreach (var s in list)
            {
                var (streaks, flash) = s.At(_t);
                if (flash > 0) g.DrawCircle(Px(s.X, s.Y), 1.4f * Cell, glow with { A = (float)flash });
                foreach (var (a, b, c, d, col) in streaks) g.DrawLine(Px(a, b), Px(c, d), new Color(col.R, col.G, col.B, col.A), 0.12f * Cell);
            }
        }
    }

    private static readonly CanvasItemMaterial Additive = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    // ---------- the motes' multimeshes ----------

    private static readonly QuadMesh Unit = new() { Size = Vector2.One };

    /// <summary>Each mote a quad scaled to its size; its sway in its custom data, run in the shader: x, y amplitude in pixels, rate, phase.</summary>
    private static MultiMeshInstance2D Motes(Mote kind, List<Speck> specks, uint colour, float alpha)
    {
        bool ember = kind == Mote.Ember;
        var mesh = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform2D, UseCustomData = true, UseColors = ember, Mesh = Unit };
        mesh.InstanceCount = specks.Count;
        for (int i = 0; i < specks.Count; i++)
        {
            var p = specks[i];
            // Never under a pixel across: the mockup's least, so the finest dust still reads.
            float size = Math.Max(1f, (ember ? Math.Max(p.Size, 0.7f) : p.Size) * Cell);
            mesh.SetInstanceTransform2D(i, new Transform2D(0, Vector2.One * size, 0, Px(p.X, p.Y)));
            mesh.SetInstanceCustomData(i, new Color(p.Ax * Cell, p.Ay * Cell, p.Rate, p.Phase));
            if (ember) mesh.SetInstanceColor(i, new Color(p.Size / Math.Max(p.Size, 0.7f), 0, 0, 1));
        }
        var node = new MultiMeshInstance2D { Multimesh = mesh, ZIndex = ember ? Layers.WreckSparks : Layers.WreckMotes };
        if (ember) node.Material = EmberMaterial;
        else
        {
            node.Material = MoteMaterial(kind);
            ((ShaderMaterial)node.Material).SetShaderParameter("tint", Of(colour, alpha));
        }
        return node;
    }

    private static MultiMeshInstance2D Glints(List<Speck> specks)
    {
        var mesh = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform2D, UseCustomData = true, Mesh = Unit };
        mesh.InstanceCount = specks.Count;
        for (int i = 0; i < specks.Count; i++)
        {
            var p = specks[i];
            mesh.SetInstanceTransform2D(i, new Transform2D(0, Vector2.One * 0.7f * Cell, 0, Px(p.X, p.Y)));
            mesh.SetInstanceCustomData(i, new Color(p.Ax * Cell, p.Ay * Cell, p.Rate, p.Phase));
        }
        return new MultiMeshInstance2D { Multimesh = mesh, ZIndex = Layers.WreckSparks, Material = GlintMaterial };
    }

    private const string Sway = """

            vec2 swayed(vec4 c) { return c.xy * sin(TIME * c.z + c.w); }
            """;

    private static readonly Dictionary<Mote, ShaderMaterial> MoteMaterials = new();

    /// <summary>A kind's motes lit as the hull is (<see cref="Lit.Painted"/>), each moved by its sway, its colour the kind's.</summary>
    private static ShaderMaterial MoteMaterial(Mote kind)
    {
        if (MoteMaterials.TryGetValue(kind, out var made)) return made;
        return MoteMaterials[kind] = Lit.Painted("uniform vec4 tint;" + Sway, """

            void vertex() {
                VERTEX += swayed(INSTANCE_CUSTOM);
                world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
            }
            """, "COLOR = tint;", 1f);
    }

    /// <summary>Embers: added over what is there, unlit, a core and (every other) a faint round glow, flickering a quarter of the time.</summary>
    private static readonly ShaderMaterial EmberMaterial = new()
    {
        Shader = new Shader
        {
            Code = """
                shader_type canvas_item;
                render_mode blend_add, unshaded;
                varying float core;
                varying float shown;
                varying float halo;
                void vertex() {
                    VERTEX += INSTANCE_CUSTOM.xy * sin(TIME * INSTANCE_CUSTOM.z + INSTANCE_CUSTOM.w);
                    core = COLOR.r;
                    float i = float(INSTANCE_ID);
                    shown = mod(floor(TIME * 6.0 + INSTANCE_CUSTOM.w * 5.0) + i, 4.0) == 0.0 ? 0.0 : 1.0;
                    halo = mod(i, 2.0) == 0.0 ? 1.0 : 0.0;
                }
                void fragment() {
                    vec2 d = abs(UV - 0.5);
                    float a = max(d.x, d.y) < core * 0.5 ? shown : (length(UV - 0.5) < 0.5 ? 0.25 * halo : 0.0);
                    COLOR = vec4(1.0, 0.478, 0.149, a);
                }
                """,
        },
    };

    /// <summary>Ice and fuel glinting: about one in eighty at a time, a white cross swelling and going, on the crystal's sway.</summary>
    private static readonly ShaderMaterial GlintMaterial = new()
    {
        Shader = new Shader
        {
            Code = """
                shader_type canvas_item;
                render_mode unshaded;
                varying float reach;
                void vertex() {
                    VERTEX += INSTANCE_CUSTOM.xy * sin(TIME * INSTANCE_CUSTOM.z + INSTANCE_CUSTOM.w);
                    float slot = floor(TIME * 1.7);
                    float h = fract(sin(dot(vec2(float(INSTANCE_ID), slot), vec2(12.9898, 78.233))) * 43758.5453);
                    reach = h < 0.012 ? 0.5 * sin(fract(TIME * 1.7) * 3.14159) : 0.0;
                }
                void fragment() {
                    vec2 d = abs(UV - 0.5);
                    float line = 0.05 / 0.7;
                    bool cross = (d.x < line * 0.5 && d.y < reach) || (d.y < line * 0.5 && d.x < reach);
                    COLOR = vec4(1.0, 1.0, 1.0, cross ? 0.95 : 0.0);
                }
                """,
        },
    };
}
