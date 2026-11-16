using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;
using Looks = Stillwake.Game.VesselLooks;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// The signs of abandonment inside a derelict (derelict-looks, <see cref="VesselSigns"/>): frost, soot and the burst pipe on the back
/// wall, the hanging cable, the fallen panel and the toppled crate before it, a burnt machine's picture darkened under its soot; each
/// while its cells stay as stamped. The cable throws sparks now and then, its flash a light through <see cref="LightView"/>; the pipe
/// puffs gas that drifts, swells and fades, clipped to its room. Particles only for the signs in view. Drawn only: no sign holds
/// anything or is named by the pointer, its gas no air, its frost no cold.
/// </summary>
public partial class VesselSignsView : Node2D, ILightSource
{
    private const int Cell = ShipCamera.Cell;

    private readonly IGridQueries _grid;
    private readonly IReadOnlyList<VesselSign> _signs;
    private readonly bool[] _shows;
    /// <summary>Whether each burnt machine still stands under its soot.</summary>
    private readonly bool[] _burnt;
    private readonly Sketch _wall = new(Layers.Pipes, lit: true), _front = new(Layers.Machines, lit: true), _darkened = new(Layers.Fittings, lit: true),
        _gas = new(Layers.Gas);
    private readonly List<LightSource> _lights = [];
    /// <summary>When each cable throws next and its flash ends, on this view's clock; a gas sign's next puff.</summary>
    private readonly double[] _next, _flashUntil;
    private readonly List<Puff> _puffs = [];
    private int _version = -1;
    private double _timeMs;

    private sealed class Puff
    {
        public double X, Y, Vx, Vy, Size, AgeMs, LifeMs;
        public Rect2 Room;
    }

    private static readonly Color Darkened = new(0.05f, 0.04f, 0.03f, 0.6f);
    private static readonly Luminous Sparks = Luminous.CrackSpark with { Gravity = Looks.SparkFall, LifeMs = (300, 750), Bounce = 0.3 };

    public VesselSignsView(IGridQueries grid, VesselSigns signs)
    {
        (_grid, _signs) = (grid, signs.Signs);
        (_shows, _burnt, _next, _flashUntil) = (new bool[_signs.Count], new bool[_signs.Count], new double[_signs.Count], new double[_signs.Count]);
        for (int i = 0; i < _signs.Count; i++) _next[i] = Between(0, Looks.SparkEveryMs.To);
        foreach (var sketch in (Sketch[])[_wall, _front, _darkened])
        {
            Lit.Set(sketch);
            AddChild(sketch);
        }
        _wall.Paint = g => Paint(g, wall: true);
        _front.Paint = g => Paint(g, wall: false);
        _darkened.Paint = PaintBurnt;
        AddChild(_gas);
        _gas.Paint = PaintGas;
    }

    public IReadOnlyList<LightSource> Lights => _lights;

    public override void _Process(double delta)
    {
        double ms = delta * 1000;
        _timeMs += ms;
        if (_grid.Version != _version) Follow();
        var view = GetCanvasTransform().AffineInverse() * GetViewportRect();
        _lights.Clear();
        for (int i = 0; i < _signs.Count; i++)
        {
            var s = _signs[i];
            if (!_shows[i] || s.Kind is not (SignKind.Cable or SignKind.Gas)) continue;
            var at = Corner(s);
            bool seen = view.Intersects(new Rect2(new Vector2(s.RoomLeft + Content.Vessels.Margin, s.RoomTop + Content.Vessels.Margin) * Cell,
                new Vector2(s.RoomWidth, Content.Vessels.Open) * Cell).Grow(4 * Cell));
            if (s.Kind == SignKind.Cable)
            {
                var tip = at + new Vector2(s.Dir < 0 ? s.Width * Cell - Looks.CableTip.X : Looks.CableTip.X, Looks.CableTip.Y);
                if (seen && _timeMs >= _next[i]) Throw(i, tip, s);
                if (_timeMs < _flashUntil[i])
                {
                    var f = L.SparkFlash;
                    float left = (float)((_flashUntil[i] - _timeMs) / Looks.SparkFlashMs);
                    _lights.Add(new LightSource(tip.X, tip.Y, f.Colour, left * f.Strength, f.Reach, f.Keep, 1));
                }
            }
            else if (seen) Puffs(i, at, s);
        }
        Step(ms);
    }

    /// <summary>What still shows and which burnt machines stand, read again only when the grid changed.</summary>
    private void Follow()
    {
        _version = _grid.Version;
        bool changed = false;
        for (int i = 0; i < _signs.Count; i++)
        {
            var s = _signs[i];
            bool shows = VesselSigns.Shows(_grid, s);
            bool burnt = shows && s.Machine != null && VesselLights.Stands(_grid, s.Machine, VesselSigns.CellOf(s.MachineX, s.MachineY, _grid.Width));
            changed |= shows != _shows[i] || burnt != _burnt[i];
            (_shows[i], _burnt[i]) = (shows, burnt);
        }
        if (!changed) return;
        foreach (var sketch in (Sketch[])[_wall, _front, _darkened]) sketch.QueueRedraw();
    }

    /// <summary>A sign's picture's top-left in the world's pixels; soot's from its machine, its foot two rows down it.</summary>
    private static Vector2 Corner(VesselSign s)
    {
        int m = Content.Vessels.Margin, top = s.Machine != null ? s.MachineY + 2 - VesselSigns.Size(s.Kind).Height : s.Y;
        return new Vector2(s.X + m, top + m) * Cell;
    }

    private void Paint(Sketch g, bool wall)
    {
        for (int i = 0; i < _signs.Count; i++)
        {
            var s = _signs[i];
            if (!_shows[i] || (VesselSigns.OnTheWall(s.Kind) || s.Kind == SignKind.Gas) != wall) continue;
            var picture = PixelArt.Picture(VesselSigns.PictureOf(s.Kind));
            var at = Corner(s);
            // Mirrored about its box's middle when it faces the other way.
            g.DrawSetTransform(s.Dir < 0 ? at + new Vector2(s.Width * Cell, 0) : at, 0, new Vector2(s.Dir, 1));
            g.DrawTexture(picture, Vector2.Zero);
        }
        g.DrawSetTransform(Vector2.Zero);
    }

    /// <summary>A burnt machine's picture laid again over it, dark: the soot over its face, not the wall round it.</summary>
    private void PaintBurnt(Sketch g)
    {
        int m = Content.Vessels.Margin;
        for (int i = 0; i < _signs.Count; i++)
        {
            var s = _signs[i];
            if (!_burnt[i]) continue;
            g.DrawTexture(PixelArt.Picture($"machine_{s.Machine}"),
                new Vector2(s.MachineX + m - ShipTiles.ReachOf(s.Machine!), s.MachineY + m - ShipTiles.RiseOf(s.Machine!)) * Cell, Darkened);
        }
    }

    /// <summary>A burst of sparks off the cable's frayed end, falling to its room's floor, and its flash.</summary>
    private void Throw(int i, Vector2 tip, VesselSign s)
    {
        var glims = Stages.Of(Layers.Effects).Glims;
        double floor = (s.RoomTop + Content.Vessels.Open + Content.Vessels.Margin) * Cell;
        int count = (int)Between(Looks.SparkCount.From, Looks.SparkCount.To + 1);
        for (int k = 0; k < count; k++)
            glims.Add(Sparks, tip.X, tip.Y, Between(-Looks.SparkAcross, Looks.SparkAcross), Between(-Looks.SparkUp, Looks.SparkDown), floor, size: Between(0.6, 1.1));
        _next[i] = _timeMs + Between(Looks.SparkEveryMs.From, Looks.SparkEveryMs.To);
        _flashUntil[i] = _timeMs + Looks.SparkFlashMs;
    }

    /// <summary>The puffs a burst pipe gives off since its last, drifting its way from where it burst.</summary>
    private void Puffs(int i, Vector2 at, VesselSign s)
    {
        if (_next[i] < _timeMs - 500) _next[i] = _timeMs;
        int m = Content.Vessels.Margin;
        var room = new Rect2(new Vector2(s.RoomLeft + m, s.RoomTop + m) * Cell, new Vector2(s.RoomWidth, Content.Vessels.Open) * Cell);
        var from = at + new Vector2(s.Dir < 0 ? s.Width * Cell - Looks.BurstAt.X : Looks.BurstAt.X, Looks.BurstAt.Y);
        while (_next[i] < _timeMs)
        {
            _next[i] += Looks.PuffEveryMs;
            _puffs.Add(new Puff
            {
                X = from.X + s.Dir * 1.6,
                Y = from.Y,
                Vx = s.Dir * Between(Looks.PuffSpeed.From, Looks.PuffSpeed.To),
                Vy = Between(Looks.PuffSink.From, Looks.PuffSink.To),
                Size = Looks.PuffSize,
                LifeMs = Between(Looks.PuffLifeMs.From, Looks.PuffLifeMs.To),
                Room = room,
            });
        }
    }

    private void Step(double ms)
    {
        if (_puffs.Count == 0) return;
        double s = ms / 1000, keep = Math.Pow(Looks.PuffDrag, s);
        for (int i = _puffs.Count - 1; i >= 0; i--)
        {
            var p = _puffs[i];
            p.AgeMs += ms;
            if (p.AgeMs >= p.LifeMs)
            {
                _puffs[i] = _puffs[^1];
                _puffs.RemoveAt(_puffs.Count - 1);
                continue;
            }
            (p.X, p.Y, p.Vx, p.Vy, p.Size) = (p.X + p.Vx * s, p.Y + p.Vy * s, p.Vx * keep, p.Vy * keep, p.Size + Looks.PuffGrow * s);
        }
        _gas.QueueRedraw();
    }

    /// <summary>Each puff a soft round of gas, cut to its room: its rectangle and the glow's matching share of it.</summary>
    private void PaintGas(Sketch g)
    {
        var glow = PixelArt.Glow;
        var size = glow.GetSize();
        var colour = new Color(Looks.PuffColour << 8 | 0xff);
        foreach (var p in _puffs)
        {
            var whole = new Rect2((float)(p.X - p.Size), (float)(p.Y - p.Size), (float)(2 * p.Size), (float)(2 * p.Size));
            var cut = whole.Intersection(p.Room);
            if (cut.Size.X <= 0 || cut.Size.Y <= 0) continue;
            var share = new Rect2((cut.Position - whole.Position) / whole.Size * size, cut.Size / whole.Size * size);
            g.DrawTextureRectRegion(glow, cut, share, colour with { A = (float)(Looks.PuffAlpha * (1 - p.AgeMs / p.LifeMs)) });
        }
    }

    /// <summary>A draw from the effects' own random: when and how particles fly is each client's, never a look.</summary>
    private static double Between(double from, double to) => Stages.Of(Layers.Effects).Glims.Between(from, to);
}
