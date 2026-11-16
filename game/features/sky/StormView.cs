using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Sky;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Sky;

/// <summary>
/// A planet's weather (docs/plans/surface-storms.md) blowing over its open sky: its streaks and wisps (<see cref="Streaks"/>), laid in
/// view each frame from the time alone and drawn in one triangle array, only over air with no back wall above the surface's top
/// row, read once as the surface loads; and its wind's loop, as loud as the storm is high, stopped once silent. Main reads
/// <see cref="Now"/> and <see cref="Weather"/> to dim the daylight, haze the sky and land and blow the plants.
/// </summary>
public partial class StormView : Node2D
{
    private const int Cell = ShipCamera.Cell;

    private readonly IGridQueries _grid;
    private readonly ISkyQueries _sky;
    private readonly Content.Surfaces.Kind _rules;
    private readonly Func<double> _clock;
    private readonly StretchShown _stretch = new();
    private readonly int[] _tops;
    private readonly float[] _line;
    private readonly Func<float, float, bool> _open;
    private readonly Func<float, float> _ground;
    private readonly List<Streaks.Line> _lines = [];
    private readonly List<Streaks.Puff> _puffs = [];
    private AudioStreamPlayer? _wind;
    private S.Cue? _windCue;
    private bool _drawn;
    private Vector2[] _points = new Vector2[256];
    private Color[] _colours = new Color[256];
    private int[] _indices = new int[255];
    private static readonly int[] Quad = [0, 1, 2, 0, 2, 3];
    private static readonly CanvasItemMaterial Unlit = new() { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    /// <param name="clock">The world's ticks now, between them too: what the sky's easing back and the streaks' flight count by.</param>
    public StormView(IGridQueries grid, ISkyQueries sky, Content.Surfaces.Kind rules, Func<double> clock)
    {
        (_grid, _sky, _rules, _clock) = (grid, sky, rules, clock);
        // Once, as the surface loads: digging and building do not move where the sky ends.
        _tops = Backdrop.GroundLine.Tops(grid.StructureLayer, grid.BackLayer, grid.Width, grid.Height);
        _open = Open;
        _line = Streaks.Smoothed(_tops, Streaks.GroundReach, Cell);
        _ground = x => Streaks.GroundAt(_line, x, Cell);
        ZIndex = Layers.Weather;
        // Lit by the daylight they are drawn in (Light), not again by the world's lamps and its dark ambient, as the wash is.
        Material = Unlit;
        _wisps = new Wisps(this) { ShowBehindParent = true, Material = Unlit };
        AddChild(_wisps);
        AddChild(_wash);
    }

    /// <summary>The weather shown, coming, running or easing back; none.</summary>
    public StretchLooks.Weather? Weather { get; private set; }

    /// <summary>How far it has come (<see cref="Storm"/>): none with no weather.</summary>
    public Storm Now { get; private set; } = Storm.None;

    /// <summary>The daylight on the land behind now (<c>Daylight.Backdrop</c>, dimmed by the storm): what the streaks, the wisps and the wash's haze are drawn in, as dim as what they cross.</summary>
    public (float R, float G, float B) Light { get; set; } = (1, 1, 1);

    public override void _Process(double delta)
    {
        var now = _sky.Now;
        _stretch.See(_rules, now.Stretch, now.Coming, now.ComingIn, _clock(), now.Ran);
        Weather = _stretch.Look?.Weather;
        Now = Storm.Of(_stretch);
        Hear();
        Lay();
        if (_lines.Count > 0 || _drawn) QueueRedraw();
        if (_puffs.Count > 0 || _wisps.Drawn) _wisps.QueueRedraw();
        _reach = Eye is { } eye ? Storm.Reach(eye.Y - _ground(eye.X)) : 1;
        float washed = Weather?.Visibility != null ? Now.Washed * _reach : 0;
        _wash.Show(Weather?.Visibility, washed, Light, Weather?.Haze ?? (1, 1, 1), Eye, GetViewport());
    }

    /// <summary>The middle of this player's hero's body, in pixels of the world: the clear patch round it in the wash. None with no hero.</summary>
    public Vector2? Eye { get; set; }

    private readonly Washout _wash = new();
    private readonly Wisps _wisps;
    private float _reach = 1;

    /// <summary>A pixel of the screen in pixels of the world, as last laid: the thinnest a streak is drawn.</summary>
    private float _pixel = 1;

    /// <summary>
    /// The weather's wash (<see cref="StretchLooks.Wash"/>, «Блёклый мир») over the world in view, under the overlays and what is read:
    /// the screen as drawn so far, blurred to a pixel of the world through its mipmaps, greyed and flattened toward the haze, clear in a
    /// soft patch round the hero. Hidden at 0, so no screen is copied without a storm.
    /// </summary>
    private sealed partial class Washout : Node2D
    {
        private static readonly Shader Shader = new()
        {
            Code = """
                shader_type canvas_item;
                render_mode unshaded;
                uniform sampler2D screen : hint_screen_texture, filter_linear_mipmap, repeat_disable;
                uniform float amount = 0.0;
                uniform vec3 haze = vec3(1.0);
                uniform float grey = 0.0;
                uniform float flat_share = 0.0;
                uniform vec2 eye;
                uniform float eyed = 0.0;
                uniform float inner = 16.0;
                uniform float outer = 34.0;
                uniform float tall = 1.0;
                varying vec2 world;
                void vertex() {
                    world = VERTEX;
                }
                void fragment() {
                    // A pixel of the world's worth of screen pixels: the pick's scene downscaled to the world's pixels and back.
                    float lod = log2(max(1.0 / max(length(dFdx(world)), 1e-4), 1.0));
                    vec3 sharp = textureLod(screen, SCREEN_UV, 0.0).rgb;
                    vec3 c = textureLod(screen, SCREEN_UV, lod).rgb;
                    c = mix(c, vec3(dot(c, vec3(0.3, 0.59, 0.11))), grey);
                    c = mix(c, haze, flat_share);
                    vec2 d = world - eye;
                    d.y /= tall;
                    float clear = eyed * clamp((outer - length(d)) / (outer - inner), 0.0, 1.0);
                    COLOR = vec4(mix(sharp, c, amount * (1.0 - clear)), 1.0);
                }
                """,
        };

        private readonly ShaderMaterial _material = new() { Shader = Shader };
        private Rect2 _view;

        public Washout()
        {
            Material = _material;
            ZAsRelative = false;
            ZIndex = Layers.Weather;
            // Before its parent on the same layer: the streaks and wisps blow close, over the washed-out distance, not lost in it.
            ShowBehindParent = true;
            Visible = false;
        }

        /// <summary>
        /// <paramref name="wash"/> at <paramref name="amount"/> of the way, toward <paramref name="haze"/> in <paramref name="light"/>, as
        /// dim as the daylight it lies under; clear round <paramref name="eye"/>. Hidden with none.
        /// </summary>
        public void Show(StretchLooks.Wash? wash, float amount, (float R, float G, float B) light, (float R, float G, float B) haze, Vector2? eye, Viewport viewport)
        {
            Visible = wash != null && amount > 0;
            if (wash == null || !Visible) return;
            _material.SetShaderParameter("amount", amount);
            _material.SetShaderParameter("haze", new Vector3(haze.R * light.R, haze.G * light.G, haze.B * light.B));
            _material.SetShaderParameter("grey", wash.Grey);
            _material.SetShaderParameter("flat_share", wash.Flat);
            _material.SetShaderParameter("inner", wash.Clear.Inner);
            _material.SetShaderParameter("outer", wash.Clear.Outer);
            _material.SetShaderParameter("tall", wash.Tall);
            _material.SetShaderParameter("eyed", eye != null ? 1f : 0f);
            if (eye is { } at) _material.SetShaderParameter("eye", at);
            var view = viewport.GetCanvasTransform().AffineInverse() * viewport.GetVisibleRect();
            if (view == _view) return;
            _view = view;
            QueueRedraw();
        }

        public override void _Draw() => DrawRect(_view.Grow(2), Colors.White);
    }

    public override void _ExitTree() => _wind?.Stop();

    /// <summary>The wind's loop at the storm's loudness; stopped with none, a weather without a wind, or once silent.</summary>
    private void Hear()
    {
        var cue = Weather?.Wind;
        float loudness = Now.Heard(Weather) * _reach;
        if (cue != _windCue)
        {
            _wind?.QueueFree();
            (_wind, _windCue) = (null, cue);
        }
        if (loudness <= 0)
        {
            if (_wind is { Playing: true }) _wind.Stop();
            return;
        }
        if (_wind == null) AddChild(_wind = Cues.Player(cue!, S.World));
        _wind.VolumeDb = cue!.VolumeDb + Mathf.LinearToDb(loudness);
        if (!_wind.Playing) _wind.Play();
    }

    /// <summary>The streaks and wisps in view now, laid from the time alone; none with no storm.</summary>
    private void Lay()
    {
        _lines.Clear();
        _puffs.Clear();
        if (Weather is not { } weather || Now.Share <= 0) return;
        var canvas = GetViewport().GetCanvasTransform();
        var view = canvas.AffineInverse() * GetViewport().GetVisibleRect();
        var box = (view.Position.X, view.Position.Y, view.End.X, view.End.Y);
        double seconds = _clock() / Sim.World.TicksPerSecond;
        Streaks.Fill(weather, Now, box, seconds, _open, _ground, _lines);
        Streaks.Puffs(weather, Now, box, seconds, _open, _ground, _puffs);
        _pixel = 1 / Math.Max(canvas.Scale.X, 1e-3f);
    }

    public override void _Draw()
    {
        _drawn = false;
        if (Weather is not { } weather || _lines.Count == 0) return;
        DrawLines(weather, new Color(Light.R, Light.G, Light.B));
        _drawn = true;
    }

    /// <summary>
    /// The storm's wisps, under its wash as on the pick page: soft dust that fades with the distance, where over the wash they would
    /// read as dark bars. Drawn before the wash on the same layer.
    /// </summary>
    private sealed partial class Wisps(StormView storm) : Node2D
    {
        public bool Drawn { get; private set; }

        public override void _Draw()
        {
            Drawn = false;
            if (storm.Weather?.Wisps is not { } wisps || storm._puffs.Count == 0) return;
            var light = new Color(storm.Light.R, storm.Light.G, storm.Light.B);
            foreach (var puff in storm._puffs)
            {
                string name = wisps.Pictures[puff.Picture];
                if (!HiRes.Has(name)) continue;
                var picture = PixelArt.Picture(name);
                var size = picture.GetSize();
                DrawTextureRect(picture, new Rect2(new Vector2(puff.X, puff.Y) - size / 2, size), false, light with { A = puff.Alpha });
            }
            Drawn = true;
        }
    }

    /// <summary>Every streak a sliver from its head to its tail, and its speck, in one triangle array.</summary>
    private void DrawLines(StretchLooks.Weather weather, Color light)
    {
        var spec = weather.Streaks;
        float dx = -MathF.Cos(spec.Angle), dy = MathF.Sin(spec.Angle);
        int points = 0, indices = 0;
        foreach (var line in _lines)
        {
            Room(points + 7, indices + 9);
            var (r, g, b, a) = spec.Colours[line.Colour];
            // A sliver thinner than a screen pixel is not drawn at all: widened to one, at its own alpha, about as it read on the page.
            float width = Math.Max(line.Width, _pixel);
            var colour = new Color(r * light.R, g * light.G, b * light.B, a);
            float half = width / 2;
            _points[points] = new Vector2(line.TailX, line.TailY);
            _points[points + 1] = new Vector2(line.HeadX + dy * half, line.HeadY - dx * half);
            _points[points + 2] = new Vector2(line.HeadX - dy * half, line.HeadY + dx * half);
            for (int k = 0; k < 3; k++) (_colours[points + k], _indices[indices + k]) = (colour, points + k);
            (points, indices) = (points + 3, indices + 3);
            if (line.Speck is not { } speck) continue;
            var (sx, sy) = speck;
            _points[points] = new Vector2(sx, sy);
            _points[points + 1] = new Vector2(sx + 0.5f, sy);
            _points[points + 2] = new Vector2(sx + 0.5f, sy + 0.45f);
            _points[points + 3] = new Vector2(sx, sy + 0.45f);
            for (int k = 0; k < 4; k++) _colours[points + k] = colour;
            for (int k = 0; k < 6; k++) _indices[indices + k] = points + Quad[k];
            (points, indices) = (points + 4, indices + 6);
        }
        // Past the last streak, triangles of one point: nothing drawn, the arrays kept for the next frame.
        Array.Clear(_indices, indices, _indices.Length - indices);
        RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), _indices, _points, _colours);
    }

    /// <summary>The arrays grown, doubling, to hold <paramref name="points"/> points and <paramref name="indices"/> indices.</summary>
    private void Room(int points, int indices)
    {
        if (points > _points.Length)
        {
            int size = Math.Max(points, _points.Length * 2);
            Array.Resize(ref _points, size);
            Array.Resize(ref _colours, size);
        }
        // Godot draws no triangle array whose indices are not a whole number of triangles.
        if (indices > _indices.Length) Array.Resize(ref _indices, (Math.Max(indices, _indices.Length * 2) + 2) / 3 * 3);
    }

    /// <summary>Whether a pixel of the world is open sky: above its column's top row as loaded, and still neither wall nor back wall.</summary>
    private bool Open(float x, float y)
    {
        int row = (int)MathF.Floor(y / Cell), column = Column(x);
        if (row < 0) return true;
        if (row >= _tops[column]) return false;
        int cell = row * _grid.Width + column;
        return _grid.StructureLayer[cell] == 0 && _grid.BackLayer[cell] == 0;
    }

    private int Column(float x)
    {
        int width = _tops.Length, column = (int)MathF.Floor(x / Cell);
        return ((column % width) + width) % width;
    }
}
