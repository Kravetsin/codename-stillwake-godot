using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Sky;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Sky;

/// <summary>
/// A planet's weather (docs/plans/surface-storms.md) blowing over its open sky: its streaks, drawn by one shader over the view from
/// the time alone, in the slanting cells <see cref="Streaks.Cells"/> lays out, nothing laid on the CPU a frame, only above each
/// column's first wall or back wall from the top, kept as the grid changes; its wash under them; and its wind's loop, as loud as the
/// storm is high, stopped once silent. Main reads <see cref="Now"/> and <see cref="Weather"/> to dim the daylight, haze the sky and
/// land and blow the plants.
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
    private readonly Image _columns;
    private readonly ImageTexture _columnsTexture;
    private readonly ShaderMaterial _material = new() { Shader = StreaksShader };
    private int _seen;
    private Rect2 _view;
    private bool _shown;
    private AudioStreamPlayer? _wind;
    private S.Cue? _windCue;

    /// <param name="clock">The world's ticks now, between them too: what the sky's easing back and the streaks' flight count by.</param>
    public StormView(IGridQueries grid, ISkyQueries sky, Content.Surfaces.Kind rules, Func<double> clock)
    {
        (_grid, _sky, _rules, _clock) = (grid, sky, rules, clock);
        // Once, as the surface loads; then a column at a time, as it is built or dug in (Follow).
        _tops = Backdrop.GroundLine.Tops(grid.StructureLayer, grid.BackLayer, grid.Width, grid.Height);
        _line = Streaks.Smoothed(_tops, Streaks.GroundReach, Cell);
        _columns = Image.CreateEmpty(grid.Width, 1, false, Image.Format.Rgf);
        for (int x = 0; x < grid.Width; x++) _columns.SetPixel(x, 0, new Color(_tops[x] * Cell, _line[x], 0));
        _columnsTexture = ImageTexture.CreateFromImage(_columns);
        _seen = grid.Version;
        _material.SetShaderParameter("columns", _columnsTexture);
        _material.SetShaderParameter("cell_px", (float)Cell);
        _material.SetShaderParameter("low_reach", Streaks.LowReach);
        ZIndex = Layers.Weather;
        Material = _material;
        AddChild(_wash);
    }

    /// <summary>The weather shown, coming, running or easing back; none.</summary>
    public StretchLooks.Weather? Weather { get; private set; }

    /// <summary>How far it has come (<see cref="Storm"/>): none with no weather.</summary>
    public Storm Now { get; private set; } = Storm.None;

    /// <summary>The daylight on the land behind now (<c>Daylight.Backdrop</c>, dimmed by the storm): what the streaks and the wash's haze are drawn in, as dim as what they cross.</summary>
    public (float R, float G, float B) Light { get; set; } = (1, 1, 1);

    /// <summary>The middle of this player's hero's body, in pixels of the world: how deep under the land the weather is felt (<see cref="Storm.Reach"/>). None with no hero.</summary>
    public Vector2? Eye { get; set; }

    private readonly Washout _wash = new();
    private float _reach = 1;

    public override void _Process(double delta)
    {
        long started = Game.Dev.FrameLog.Begin();
        var now = _sky.Now;
        _stretch.See(_rules, now.Stretch, now.Coming, now.ComingIn, _clock(), now.Ran);
        Weather = _stretch.Look?.Weather;
        Now = Storm.Of(_stretch);
        _reach = Eye is { } eye ? Storm.Reach(eye.Y - Streaks.GroundAt(_line, eye.X, Cell)) : 1;
        Hear();
        Blow();
        Game.Dev.FrameLog.End("storm", started);
        float washed = Weather?.Visibility != null ? Now.Washed * _reach : 0;
        _wash.Show(Weather?.Visibility, washed, Light, Weather?.Haze ?? (1, 1, 1), GetViewport());
    }

    /// <summary>The streaks' shader given the weather and the time; the view drawn again only as it moves, or as they come or go.</summary>
    private void Blow()
    {
        int count = Weather is { } shown ? Now.Streaks(shown) : 0;
        bool shows = count > 0;
        if (shows)
        {
            Follow();
            var weather = Weather!;
            if (weather != _given) Give(weather);
            _material.SetShaderParameter("seconds", Streaks.Flown(_clock() / Sim.World.TicksPerSecond));
            _material.SetShaderParameter("density", count / (float)weather.Count);
            _material.SetShaderParameter("light", new Vector3(Light.R, Light.G, Light.B));
        }
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewport().GetVisibleRect();
        if (shows == _shown && (!shows || view == _view)) return;
        (_shown, _view) = (shows, view);
        QueueRedraw();
    }

    /// <summary>The weather whose streaks the shader was last given (<see cref="Give"/>).</summary>
    private StretchLooks.Weather? _given;

    /// <summary>The shader given <paramref name="weather"/>'s streaks, which hold while it blows.</summary>
    private void Give(StretchLooks.Weather weather)
    {
        _given = weather;
        var spec = weather.Streaks;
        var (along, across) = Streaks.Cells(weather);
        _material.SetShaderParameter("dir", new Vector2(-MathF.Cos(spec.Angle), MathF.Sin(spec.Angle)));
        _material.SetShaderParameter("cells", new Vector2(along, across));
        _material.SetShaderParameter("speed", new Vector2(spec.Speed.Least, spec.Speed.Most));
        _material.SetShaderParameter("len", new Vector2(spec.Length.Least, spec.Length.Most));
        _material.SetShaderParameter("width", spec.Width);
        _material.SetShaderParameter("low", spec.Low);
        var colours = new Vector4[3];
        for (int k = 0; k < 3; k++)
        {
            var (r, g, b, a) = spec.Colours[k % spec.Colours.Count];
            colours[k] = new Vector4(r, g, b, a);
        }
        _material.SetShaderParameter("colours", colours);
    }

    public override void _Draw()
    {
        if (_shown) DrawRect(_view.Grow(2), Colors.White);
    }

    /// <summary>Each column built or dug in since last seen read again, its sky's end moved in the shader's columns; every column if the log has run past.</summary>
    private void Follow()
    {
        int version = _grid.Version;
        if (version == _seen) return;
        var changes = _grid.ChangedSince(_seen);
        _seen = version;
        int width = _grid.Width;
        bool moved = false;
        if (changes == null)
            for (int x = 0; x < width; x++) moved |= Read(x);
        else
            foreach (var change in changes)
                if ((change.Layers & (GridLayers.Structure | GridLayers.Back)) != 0) moved |= Read(change.Cell % width);
        if (moved) _columnsTexture.Update(_columns);
    }

    /// <summary>Column <paramref name="x"/>'s top read again: whether it moved.</summary>
    private bool Read(int x)
    {
        int top = Backdrop.GroundLine.Top(_grid.StructureLayer, _grid.BackLayer, _grid.Width, _grid.Height, x);
        if (top == _tops[x]) return false;
        _tops[x] = top;
        _columns.SetPixel(x, 0, new Color(top * Cell, _line[x], 0));
        return true;
    }

    /// <summary>
    /// The streaks, as <c>Streaks.Fill</c> laid them on the CPU before: the world cut into cells along the fall (<c>cells.x</c>) and
    /// across it (<c>cells.y</c>), each lane across falling at its own speed; a cell holds a streak at <c>density</c>, its head anywhere
    /// in it, so a pixel reads its own cell and the next, whose streak may reach back into it. Each tapers from its head to its tail,
    /// at least a screen pixel wide at its own alpha; a low weather's thin with the height over the smoothed ground, jitter, and some
    /// carry a speck of grit. None at or under a column's first wall or back wall from the top.
    /// </summary>
    private static readonly Shader StreaksShader = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded;
            uniform sampler2D columns : filter_nearest, repeat_enable;
            uniform float cell_px = 8.0;
            uniform float seconds = 0.0;
            uniform vec2 dir = vec2(-1.0, 0.0);
            uniform vec2 cells = vec2(12.0, 10.0);
            uniform vec2 speed = vec2(100.0, 200.0);
            uniform vec2 len = vec2(4.0, 8.0);
            uniform float width = 0.4;
            uniform bool low = false;
            uniform float low_reach = 70.0;
            uniform float density = 0.0;
            uniform vec4 colours[3];
            uniform vec3 light = vec3(1.0);
            varying vec2 world;

            void vertex() {
                world = VERTEX;
            }

            float hash(int x, int y, uint k) {
                uint h = uint(x) * 374761393u ^ uint(y) * 668265263u ^ k * 3266489917u;
                h = (h ^ (h >> 13u)) * 1274126177u;
                h ^= h >> 16u;
                h *= 2654435761u;
                h ^= h >> 15u;
                return float(h >> 8u) / 16777216.0;
            }

            vec2 column(float c) {
                float n = float(textureSize(columns, 0).x);
                return texelFetch(columns, ivec2(int(mod(c, n)), 0), 0).rg;
            }

            float ground(float x) {
                float at = x / cell_px - 0.5;
                float left = floor(at);
                return mix(column(left).g, column(left + 1.0).g, at - left);
            }

            void fragment() {
                float px = length(dFdx(world));
                if (world.y >= column(floor(world.x / cell_px)).r) discard;
                vec2 across = vec2(-dir.y, dir.x);
                int lane = int(floor(dot(world, across) / cells.y));
                float s = mix(speed.x, speed.y, hash(lane, 0, 1u));
                int first = int(floor((dot(world, dir) - s * seconds) / cells.x));
                vec3 lit = vec3(0.0);
                float cover = 0.0;
                for (int k = 0; k < 2; k++) {
                    int i = first + k;
                    if (hash(i, lane, 2u) >= density) continue;
                    float jitter = low ? sin(mod(seconds * 7.0, 6.2831853) + hash(i, lane, 7u) * 6.0) * 0.8 : 0.0;
                    vec2 head = dir * ((float(i) + hash(i, lane, 3u)) * cells.x + s * seconds)
                        + across * ((float(lane) + 0.15 + 0.7 * hash(i, lane, 4u)) * cells.y) + vec2(0.0, jitter);
                    if (low && hash(i, lane, 8u) >= clamp(1.0 - (ground(head.x) - head.y) / low_reach, 0.0, 1.0)) continue;
                    vec4 tint = colours[int(hash(i, lane, 9u) * 2.999)];
                    vec2 d = world - head;
                    float behind = -dot(d, dir);
                    float f = 1.0 - behind / mix(len.x, len.y, hash(i, lane, 5u));
                    float w = max(width * (0.7 + 0.6 * hash(i, lane, 6u)), px);
                    float a = behind >= 0.0 && f >= 0.0 ? clamp((w * f * 0.5 + 0.5 * px - abs(dot(d, across))) / px, 0.0, 1.0) : 0.0;
                    if (low && hash(i, lane, 10u) < 0.35) {
                        vec2 o = abs(world - head - vec2(6.0 * hash(i, lane, 11u) + 0.25, 3.0 * (hash(i, lane, 12u) - 0.5) + 0.225));
                        if (o.x < 0.5 * px && o.y < 0.5 * px) a = max(a, min(1.0, 0.225 / (px * px)));
                    }
                    a *= tint.a;
                    lit = tint.rgb * a + lit * (1.0 - a);
                    cover = a + cover * (1.0 - a);
                }
                if (cover <= 0.0) discard;
                COLOR = vec4(lit / cover * light, cover);
            }
            """,
    };

    /// <summary>
    /// The weather's wash (<see cref="StretchLooks.Wash"/>, «Блёклый мир») over the world in view, under the overlays and what is read:
    /// the screen as drawn so far, blurred through its mipmaps alike at every zoom, greyed and flattened toward the haze, the hero
    /// too. Hidden at 0, so no screen is copied without a storm.
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
                uniform float blur = 0.7;
                void fragment() {
                    // As soft at every zoom, or closer in it would smear, and at every screen size (the user's, 2026-11-16).
                    // blur: mip levels at 1080 rows, between the far zoom's too sharp and the near one's smear.
                    float lod = clamp(log2(1.0 / SCREEN_PIXEL_SIZE.y / 1080.0) + blur, 0.0, 2.0);
                    vec3 sharp = textureLod(screen, SCREEN_UV, 0.0).rgb;
                    vec3 c = textureLod(screen, SCREEN_UV, lod).rgb;
                    c = mix(c, vec3(dot(c, vec3(0.3, 0.59, 0.11))), grey);
                    c = mix(c, haze, flat_share);
                    COLOR = vec4(mix(sharp, c, amount), 1.0);
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
            // Before its parent on the same layer: the streaks blow close, over the washed-out distance, not lost in it.
            ShowBehindParent = true;
            Visible = false;
        }

        /// <summary>
        /// <paramref name="wash"/> at <paramref name="amount"/> of the way, toward <paramref name="haze"/> in <paramref name="light"/>, as
        /// dim as the daylight it lies under. Hidden with none.
        /// </summary>
        public void Show(StretchLooks.Wash? wash, float amount, (float R, float G, float B) light, (float R, float G, float B) haze, Viewport viewport)
        {
            Visible = wash != null && amount > 0;
            if (wash == null || !Visible) return;
            _material.SetShaderParameter("amount", amount);
            _material.SetShaderParameter("haze", new Vector3(haze.R * light.R, haze.G * light.G, haze.B * light.B));
            _material.SetShaderParameter("grey", wash.Grey);
            _material.SetShaderParameter("flat_share", wash.Flat);
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
}
