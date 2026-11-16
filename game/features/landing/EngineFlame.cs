using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// The engine's flame out of its nozzle past the stern while the ship flies between planets (solar-system): a white-hot core in an
/// amber plume, flickering, longest at full warp (<see cref="Warp"/>), and the light it throws on the hull (a <see cref="Glow"/>).
/// Unlit; nothing while the ship stands.
/// </summary>
public partial class EngineFlame : Node2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The plume's length in pixels at a standing burn and at full warp.</summary>
    private const float Short = 28, Long = 120;
    private static readonly Color Plume = new(1f, 0.62f, 0.25f), Core = new(1f, 0.95f, 0.82f);

    private readonly IGridQueries _grid;
    private readonly LandingModule _landing;
    private readonly Func<double> _progress;
    private readonly Sketch _flame;
    private readonly Glow _glow = new() { Reach = 14, Keep = 0.85f };
    /// <summary>The nozzle's tip and the plume's half-height, in pixels, found as a course sets off.</summary>
    private (Vector2 Tip, float Half)? _nozzle;
    private float _depth;
    private double _time;

    public EngineFlame(IGridQueries grid, LandingModule landing, Func<double> progress)
    {
        (_grid, _landing, _progress) = (grid, landing, progress);
        _flame = new Sketch(Layers.Screens) { Paint = Paint };
        AddChild(_flame);
        AddChild(_glow);
    }

    public override void _Process(double delta)
    {
        var course = _landing.Course;
        if (course == null)
        {
            if (_nozzle == null) return;
            _nozzle = null;
            _glow.Colour = default;
            _flame.QueueRedraw();
            return;
        }
        _nozzle ??= Nozzle();
        if (_nozzle is not { } n) return;
        _time += delta;
        _depth = Warp.Depth(course.TicksLeft, Stillwake.Content.Landing.FlightTicks, _progress());
        float strength = 0.35f + 0.65f * _depth;
        _glow.Position = n.Tip + new Vector2(-12, 0);
        _glow.Colour = Plume * (0.5f * strength * Flicker(0));
        _flame.QueueRedraw();
    }

    /// <summary>The engine's nozzle tip: its left side's middle, as far past its footprint as it reaches.</summary>
    private (Vector2, float)? Nozzle()
    {
        if (_landing.Engine is not { } m || Stillwake.Content.Machines.Get(m.Kind) is not { Exhaust: int reach } spec) return null;
        int col = m.Cell % _grid.Width, row = m.Cell / _grid.Width;
        return (new Vector2((col - reach) * Cell, (row + spec.Height / 2f) * Cell), spec.Height * Cell * 0.3f);
    }

    private float Flicker(float seed) => 0.85f + 0.15f * Mathf.Sin((float)_time * 31 + seed * 7) * Mathf.Sin((float)_time * 17 + seed * 3);

    private void Paint(Sketch g)
    {
        if (_nozzle is not { } n || _landing.Course == null) return;
        float strength = 0.35f + 0.65f * _depth;
        float length = Mathf.Lerp(Short, Long, _depth) * Flicker(1);
        Tongue(g, n.Tip, length, n.Half, Plume with { A = 0.35f + 0.4f * strength });
        Tongue(g, n.Tip, length * 0.75f, n.Half * 0.7f, Plume with { A = 0.6f + 0.3f * strength });
        Tongue(g, n.Tip, length * 0.45f * Flicker(2), n.Half * 0.4f, Core with { A = 0.9f });
    }

    /// <summary>A tongue of flame out of <paramref name="tip"/> to the left, <paramref name="half"/> across at its root, pointed.</summary>
    private static void Tongue(Sketch g, Vector2 tip, float length, float half, Color colour)
    {
        const int steps = 10;
        var points = new Vector2[2 * steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps, wide = half * (1 - t) * (1 - t * 0.3f);
            points[i] = tip + new Vector2(-length * t, -wide);
            points[2 * steps - i] = tip + new Vector2(-length * t, wide);
        }
        g.DrawColoredPolygon(points, colour);
    }
}
