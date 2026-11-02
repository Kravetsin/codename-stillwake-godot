using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Grid;

/// <summary>What still moves on the wreck (<see cref="WreckMotion"/>): steps it, and paints its wires over the hull, unlit, as they were picked.</summary>
public partial class WreckMotionView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private const float WireWidth = 1;
    private static readonly Color Fitting = AsciiArt.Colour(0x1a1e25);

    private readonly WreckMotion _motion;
    private readonly Sketch _wires = new(Layers.Fixtures);
    private readonly Func<double, double, bool> _seen;
    private readonly (double X, double Y)[] _points = new (double, double)[WreckMotion.PointCount];
    private readonly (double X, double Y)[] _inCells = new (double, double)[WreckMotion.PointCount];
    private Rect2 _view;

    public WreckMotionView(IGridQueries grid)
    {
        var effects = Stages.Of(Layers.Effects);
        _motion = new WreckMotion(grid, effects.Glims, effects.Bolts);
        _seen = (x, y) => _view.HasPoint(new Vector2((float)x, (float)y));
        _wires.Paint = PaintWires;
        AddChild(_wires);
    }

    public override void _Process(double delta)
    {
        _view = (GetViewport().GetCanvasTransform().AffineInverse() * GetViewportRect()).Grow(4 * Cell);
        _motion.Step(delta * 1000, _seen);
        _wires.Visible = _motion.LiveWires.Count > 0;
        if (_wires.Visible) _wires.QueueRedraw();
    }

    private void PaintWires(Sketch g)
    {
        foreach (var wire in _motion.LiveWires)
        {
            _motion.PointsOf(wire, _points);
            var (root, tip) = (_points[0], _points[^1]);
            if (!_seen(root.X, root.Y) && !_seen(tip.X, tip.Y)) continue;
            for (int i = 0; i < _points.Length; i++) _inCells[i] = (_points[i].X / Cell, _points[i].Y / Cell);
            g.Rope(_inCells, Color.Color8((byte)wire.Colour.R, (byte)wire.Colour.G, (byte)wire.Colour.B), WireWidth);
            g.DrawRect(new Rect2(Mathf.Floor((float)root.X) - 1, Mathf.Floor((float)root.Y), 3, 1), Fitting);
        }
    }
}
