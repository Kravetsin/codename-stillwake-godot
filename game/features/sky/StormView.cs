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
    private readonly Func<float, float, bool> _open;
    private readonly Func<float, float> _ground;
    private readonly List<Streaks.Line> _lines = [];
    private readonly List<Streaks.Puff> _puffs = [];
    private AudioStreamPlayer? _wind;
    private S.Cue? _windCue;
    private bool _drawn;
    private Vector2[] _points = new Vector2[256];
    private Color[] _colours = new Color[256];
    private int[] _indices = new int[256];
    private static readonly int[] Quad = [0, 1, 2, 0, 2, 3];

    /// <param name="clock">The world's ticks now, between them too: what the sky's easing back and the streaks' flight count by.</param>
    public StormView(IGridQueries grid, ISkyQueries sky, Content.Surfaces.Kind rules, Func<double> clock)
    {
        (_grid, _sky, _rules, _clock) = (grid, sky, rules, clock);
        // Once, as the surface loads: digging and building do not move where the sky ends.
        _tops = Backdrop.GroundLine.Tops(grid.StructureLayer, grid.BackLayer, grid.Width, grid.Height);
        _open = Open;
        _ground = x => _tops[Column(x)] * (float)Cell;
        ZIndex = Layers.Effects;
    }

    /// <summary>The weather shown, coming, running or easing back; none.</summary>
    public StretchLooks.Weather? Weather { get; private set; }

    /// <summary>How far it has come (<see cref="Storm"/>): none with no weather.</summary>
    public Storm Now { get; private set; } = Storm.None;

    /// <summary>The daylight on the open sky now (<c>Daylight.Sky</c>, dimmed by the storm): what the streaks are drawn in.</summary>
    public (float R, float G, float B) Light { get; set; } = (1, 1, 1);

    public override void _Process(double delta)
    {
        var now = _sky.Now;
        _stretch.See(_rules, now.Stretch, now.Coming, now.ComingIn, _clock(), now.Ran);
        Weather = _stretch.Look?.Weather;
        Now = Storm.Of(_stretch);
        Hear();
        if (Weather != null && Now.Share > 0) QueueRedraw();
        else if (_drawn) QueueRedraw();
    }

    public override void _ExitTree() => _wind?.Stop();

    /// <summary>The wind's loop at the storm's loudness; stopped with none, a weather without a wind, or once silent.</summary>
    private void Hear()
    {
        var cue = Weather?.Wind;
        float loudness = Now.Heard(Weather);
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

    public override void _Draw()
    {
        _drawn = false;
        if (Weather is not { } weather || Now.Share <= 0) return;
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewport().GetVisibleRect();
        var box = (view.Position.X, view.Position.Y, view.End.X, view.End.Y);
        double seconds = _clock() / Sim.World.TicksPerSecond;
        _lines.Clear();
        Streaks.Fill(weather, Now, box, seconds, _open, _ground, _lines);
        _puffs.Clear();
        Streaks.Puffs(weather, Now, box, seconds, _open, _ground, _puffs);
        var light = new Color(Light.R, Light.G, Light.B);
        foreach (var puff in _puffs)
        {
            string name = weather.Wisps!.Pictures[puff.Picture];
            if (!HiRes.Has(name)) continue;
            var picture = PixelArt.Picture(name);
            var size = picture.GetSize();
            DrawTextureRect(picture, new Rect2(new Vector2(puff.X, puff.Y) - size / 2, size), false, light with { A = puff.Alpha });
        }
        if (_lines.Count > 0) DrawLines(weather, light);
        _drawn = true;
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
            var colour = new Color(r * light.R, g * light.G, b * light.B, a);
            float half = line.Width / 2;
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
        if (indices > _indices.Length) Array.Resize(ref _indices, Math.Max(indices, _indices.Length * 2));
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
