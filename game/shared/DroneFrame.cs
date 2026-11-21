using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Shared;

/// <summary>
/// The drone's sparks framing the open drone window (drone-frame, the pick «Нити»), one per client, over the windows: sparks fly
/// straight from the drone's eye to the outline the window handed <see cref="Ui.Soot"/>, the corner nearest the drone first, and
/// stay as rows of small squares shimmering round it; closing, they fly home, the farthest first (<see cref="SootRun"/>). It tells
/// the rest whether a drone window is out (<see cref="Ui.DroneWindow"/>). Screen px.
/// </summary>
public partial class DroneFrame : CanvasLayer
{
    /// <summary>A landed square's shimmer, as the pick's <c>frameSq</c>: a wave running round its row, a little noise, added.</summary>
    private static readonly string ShimmerShader = $$"""
        shader_type canvas_item;
        render_mode blend_add, unshaded;
        // UV: the square's share of the way round its row, its row.
        float hash(float n) { return fract(sin(n) * 43758.5453); }
        float noise(float x) { float i = floor(x); float f = fract(x); return mix(hash(i), hash(i + 1.0), f * f * (3.0 - 2.0 * f)); }
        void fragment() {
            float wave = pow(0.5 + 0.5 * sin(UV.x * TAU * {{Num(I.ShimmerWaves)}} - TIME * {{Num(I.ShimmerPace)}} + UV.y), {{Num(I.ShimmerSharp)}});
            float heat = min(1.0, {{Num(I.ShimmerLeast)}} + {{Num(I.Shimmer)}} * wave + {{Num(I.ShimmerNoise)}} * noise(TIME * {{Num(I.ShimmerNoisePace)}} + UV.x * 977.0 + UV.y * 31.0));
            COLOR = vec4(mix(COLOR.rgb, vec3(1.0), 0.25 * heat), 0.95 * heat);
        }
        """;

    private static readonly Color Ember = new(I.Ember.R / 255f, I.Ember.G / 255f, I.Ember.B / 255f), Dark = new(I.Dark.R / 255f, I.Dark.G / 255f, I.Dark.B / 255f);

    private readonly Ui _ui;
    private readonly SootFade _fade = new();
    /// <summary>The landed squares, drawn once and moved as the window sways: their dark, their shimmering glow.</summary>
    private readonly Node2D _landedDark = new(), _landedGlow = new();
    /// <summary>The squares in flight, drawn each frame: their dark, their tails and threads.</summary>
    private readonly Node2D _flyingDark = new(), _flyingLight = new();
    private readonly Triangles _dark = new(), _light = new();
    private readonly List<Vector2> _points = [], _uvs = [];
    private readonly List<Color> _colours = [];
    private readonly List<int> _indices = [];
    /// <summary>The squares round the outline as it was last laid, the outline moved to start at nought, its near corner.</summary>
    private SootSpots? _spots;
    private (float X, float Y)[] _shape = [];
    private int _near = -1, _landed = -1;
    private (float X, float Y) _at;
    private (float X, float Y)? _drone;
    private bool _wasOpen, _flew;

    public DroneFrame(Ui ui)
    {
        _ui = ui;
        Layer = ScreenLayers.DroneFrame;
        _landedGlow.Material = new ShaderMaterial { Shader = new Shader { Code = ShimmerShader } };
        _flyingLight.Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
        _landedDark.Draw += DrawLandedDark;
        _landedGlow.Draw += DrawLandedGlow;
        _flyingDark.Draw += DrawFlyingDark;
        _flyingLight.Draw += DrawFlyingLight;
        foreach (var node in (ReadOnlySpan<Node2D>)[_landedDark, _landedGlow, _flyingDark, _flyingLight]) AddChild(node);
    }

    public override void _Process(double delta)
    {
        var soot = _ui.Soot;
        var canvas = GetViewport().GetCanvasTransform();
        _drone = null;
        if (_ui.DroneLens is var (x, y, w, h))
        {
            var lens = canvas * new Vector2(x + w / 2, y + h / 2);
            _drone = (lens.X, lens.Y);
        }
        bool open = soot.Open && soot.Outline.Count >= 3;
        if (soot.Outline.Count >= 3) Lay(soot.Outline, open);
        _fade.Step(delta, open);
        _ui.DroneWindow = (open, _fade.Shown ? Math.Max(_fade.Out, 1e-3) : 0);
        var origin = new Vector2(_at.X, _at.Y);
        _landedDark.Position = _landedGlow.Position = origin;
        int landed = 0;
        bool flying = false;
        for (int i = 0; _spots != null && i < _spots.Count; i++)
        {
            float q = _fade.Landed(_spots.Lag(i));
            if (q >= 1 || q > 0 && _drone == null) landed++;
            else if (q > 0) flying = true;
        }
        // Landing goes by lag, so how many have landed says which.
        if (landed != _landed)
        {
            _landed = landed;
            _landedDark.QueueRedraw();
            _landedGlow.QueueRedraw();
        }
        if (flying || _flew)
        {
            _flyingDark.QueueRedraw();
            _flyingLight.QueueRedraw();
        }
        _flew = flying;
    }

    /// <summary>
    /// The squares round <paramref name="outline"/>, laid again only when its shape changes, or when it opens from nothing with another
    /// corner nearest the drone, so none ever shifts mid-flight; it is only moved as it sways.
    /// </summary>
    private void Lay(IReadOnlyList<(float X, float Y)> outline, bool open)
    {
        var origin = outline[0];
        bool same = _shape.Length == outline.Count;
        for (int k = 0; same && k < outline.Count; k++)
            same = MathF.Abs(outline[k].X - origin.X - _shape[k].X) < 0.5f && MathF.Abs(outline[k].Y - origin.Y - _shape[k].Y) < 0.5f;
        int near = _near;
        if (!same || open && !_wasOpen && !_fade.Shown) near = _drone is { } drone ? SootFrame.NearCorner(outline, drone) : 0;
        _wasOpen = open;
        _at = origin;
        if (same && near == _near && _spots != null) return;
        _shape = new (float X, float Y)[outline.Count];
        for (int k = 0; k < outline.Count; k++) _shape[k] = (outline[k].X - origin.X, outline[k].Y - origin.Y);
        _near = near;
        _spots = SootFrame.Spots(_shape, near);
        _landed = -1;
    }

    private bool LandedNow(int i) => _fade.Landed(_spots!.Lag(i)) is var q && (q >= 1 || q > 0 && _drone == null);

    /// <summary>The landed squares' dark, in the outline's own coordinates.</summary>
    private void DrawLandedDark()
    {
        _dark.Clear();
        const float Side = I.Square;
        for (int i = 0; _spots != null && i < _spots.Count; i++)
            if (LandedNow(i))
            {
                var (x, y) = _spots.Spot(i);
                _dark.Rect(x - Side / 2, y - Side / 2, Side, Side, Dark);
            }
        _dark.Flush(_landedDark.GetCanvasItem());
    }

    /// <summary>The landed squares' glow, each carrying its place round its row for the shimmer.</summary>
    private void DrawLandedGlow()
    {
        _points.Clear();
        _uvs.Clear();
        _colours.Clear();
        _indices.Clear();
        const float Half = I.SquareGlow / 2;
        for (int i = 0; _spots != null && i < _spots.Count; i++)
        {
            if (!LandedNow(i)) continue;
            var (x, y) = _spots.Spot(i);
            var uv = new Vector2(_spots.Per(i), _spots.Row(i));
            int at = _points.Count;
            foreach (var corner in (ReadOnlySpan<Vector2>)[new(-Half, -Half), new(Half, -Half), new(Half, Half), new(-Half, Half)])
            {
                _points.Add(new Vector2(x, y) + corner);
                _uvs.Add(uv);
                _colours.Add(Ember);
            }
            foreach (int k in (ReadOnlySpan<int>)[0, 1, 2, 0, 2, 3]) _indices.Add(at + k);
        }
        if (_indices.Count == 0) return;
        RenderingServer.CanvasItemAddTriangleArray(_landedGlow.GetCanvasItem(), [.. _indices], [.. _points], [.. _colours], [.. _uvs],
            ReadOnlySpan<int>.Empty, ReadOnlySpan<float>.Empty, default, -1);
    }

    /// <summary>The squares in flight: a spark's dark square each.</summary>
    private void DrawFlyingDark()
    {
        _dark.Clear();
        const float Side = I.Flying;
        for (int i = 0; _spots != null && i < _spots.Count; i++)
        {
            var square = SootRun.At(_spots, i, _fade, _at, _drone);
            if (square.Out && !square.Landed) _dark.Rect(square.X - Side / 2, square.Y - Side / 2, Side, Side, Dark);
        }
        _dark.Flush(_flyingDark.GetCanvasItem());
    }

    /// <summary>The squares in flight: each one's glowing tail, and every few's thread from where it left.</summary>
    private void DrawFlyingLight()
    {
        _light.Clear();
        const float Side = I.Flying;
        for (int i = 0; _spots != null && i < _spots.Count; i++)
        {
            var square = SootRun.At(_spots, i, _fade, _at, _drone);
            if (!square.Out || square.Landed) continue;
            if (SootRun.Threads(i))
                _light.Quad(new Vector2(square.FromX, square.FromY), new Vector2(square.X, square.Y), I.ThreadLine, new Color(Ember, I.ThreadAlpha));
            for (int m = 0; m < I.Tail; m++)
            {
                float back = m * I.TailStep, x = square.X - square.Dx * back, y = square.Y - square.Dy * back;
                var colour = Ember.Lerp(Colors.White, I.TailPale * (1 - m / (float)I.Tail));
                _light.Rect(x - Side / 2, y - Side / 2, Side, Side, new Color(colour, (I.TailAlpha - m * I.TailFade) * I.FlyingHeat));
            }
        }
        _light.Flush(_flyingLight.GetCanvasItem());
    }

    private static string Num(float value) => value.ToString("0.0###", System.Globalization.CultureInfo.InvariantCulture);
}
