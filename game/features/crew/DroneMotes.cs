using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using L = Stillwake.Game.Features.Crew.DroneMotesLook;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The drone's hologram before the hero (<see cref="DroneMotesLook"/>): a small cloud of motes over the drone for a drone window, a
/// blueprint's grid of them with the plan out, and a faint cone up to it from the lens, through the edge keys' shader, unlit; a glow
/// under it; and a mote flown to each plan placed, flashing there. Drawn in the world's pixels, on the drone's layer.
/// </summary>
public partial class DroneMotes : Node2D
{
    private static readonly Color Tint = new(DroneSign.Tint);

    private readonly Glow _glow = new();
    private readonly Vector2[] _cone = new Vector2[3];
    private readonly Color[] _coneColours = new Color[3];
    /// <summary>The ribbon's points kept behind a mote sent to a plan, one each 60th of a second, as the loot's (<see cref="Items.PickUpFlights"/>).</summary>
    private const int Kept = 10;
    private static readonly (int, int, int) Ribbon = (168, 238, 255);

    /// <summary>A mote sent to a plan: from where, to where, when on its clock, and the ribbon behind it.</summary>
    private sealed class Sent(Vector2 from, Vector2 to, double ms)
    {
        public Vector2 From { get; } = from;
        public Vector2 To { get; } = to;
        public double Ms { get; } = ms;
        public double Since;
        public bool Landed;
        public readonly (double X, double Y)[] Trail = new (double, double)[Kept];
        public int Points;
    }

    private readonly List<Sent> _sent = [];
    private Vector2 _middle, _lens;
    private double _shown, _timeMs, _seed;
    private bool _grid;
    private float _alpha;

    public DroneMotes()
    {
        TopLevel = true;
        (ZAsRelative, ZIndex) = (false, Layers.Drones);
        Visible = false;
        Material = DroneHolo.Holo(grey: false);
        AddChild(_glow);
    }

    /// <summary>
    /// Shows it <paramref name="shown"/> in (0 hides all but the motes still flying to plans), as the plans' grid if
    /// <paramref name="grid"/>, its middle at <paramref name="middle"/>, its cone from <paramref name="lens"/>.
    /// </summary>
    public void Show(double shown, bool coming, bool grid, Vector2 middle, Vector2 lens, double timeMs, double seed)
    {
        double elapsedMs = Math.Max(0, timeMs - _timeMs);
        _timeMs = timeMs;
        FlySent(elapsedMs);
        Visible = shown > 0 || _sent.Count > 0;
        _glow.Visible = shown > 0;
        if (!Visible) return;
        double bright = SheetHoloLook.Brightness(shown, coming);
        _alpha = (float)(DroneSign.Alpha * (1 - DroneSign.FlickerDepth * SheetHoloLook.Flicker(timeMs, seed)));
        (_shown, _grid, _middle, _lens, _seed) = (shown, grid, middle, lens, seed);
        _glow.Position = middle;
        float glow = (float)(SheetHoloLook.Light * bright * 0.25);
        _glow.Colour = new Color(Tint.R * glow, Tint.G * glow, Tint.B * glow);
        _bright = (float)bright;
        QueueRedraw();
    }

    private float _bright;

    /// <summary>Sends a mote from the hologram to the plan placed at <paramref name="to"/>, in the world's pixels.</summary>
    public void Send(Vector2 to) => _sent.Add(new Sent(_middle, to, _timeMs));

    /// <summary>Each mote sent on its way, its ribbon and glow drawn as the loot's spark, a glint where it lands; those done gone.</summary>
    private void FlySent(double elapsedMs)
    {
        var flares = Stages.Of(Layers.Effects).Flares;
        for (int i = _sent.Count - 1; i >= 0; i--)
        {
            var s = _sent[i];
            double ms = _timeMs - s.Ms;
            if (L.Sent((s.From.X, s.From.Y), (s.To.X, s.To.Y), ms) is not { } flying)
            {
                _sent.RemoveAt(i);
                continue;
            }
            if (ms >= L.FlyMs)
            {
                if (!s.Landed) flares.Glint(s.To.X, s.To.Y, 3, Ribbon);
                s.Landed = true;
                continue;
            }
            if ((s.Since += elapsedMs) >= Glim.SampleMs || s.Points == 0)
            {
                s.Since = 0;
                Array.Copy(s.Trail, 0, s.Trail, 1, Kept - 1);
                s.Points = Math.Min(Kept, s.Points + 1);
            }
            s.Trail[0] = flying.At;
            if (s.Points > 1) flares.Trail(s.Trail.AsSpan(0, s.Points), Ribbon, 0.7, 0.85, 0.5);
            flares.Bloom(flying.At.X, flying.At.Y, 5, Ribbon, 0.5);
        }
    }

    public override void _Draw()
    {
        float alpha = _alpha * _bright;
        if (_shown > 0)
        {
            float half = (float)(L.HalfWide * L.ConeWide);
            if ((_lens - _middle).Length() > 2)
            {
                (_cone[0], _cone[1], _cone[2]) = (_lens, _middle + new Vector2(-half, 0), _middle + new Vector2(half, 0));
                (_coneColours[0], _coneColours[1], _coneColours[2]) =
                    (Tint with { A = alpha * (float)L.ConeAtLens }, Tint with { A = alpha * (float)L.ConeAtCloud }, Tint with { A = alpha * (float)L.ConeAtCloud });
                DrawPolygon(_cone, _coneColours);
            }
            var lens = _lens - _middle;
            for (int i = 0; i < L.Count; i++)
            {
                var (x, y) = L.At(i, _shown, _timeMs, _seed, (lens.X, lens.Y), _grid);
                Square(_middle + new Vector2((float)x, (float)y), (float)L.Side, alpha * (float)L.Twinkle(i, _timeMs, _seed));
            }
        }
        foreach (var s in _sent)
            if (L.Sent((s.From.X, s.From.Y), (s.To.X, s.To.Y), _timeMs - s.Ms) is { } flying)
                Square(new Vector2((float)flying.At.X, (float)flying.At.Y), (float)flying.Side, _alpha * (float)flying.Alpha);
    }

    /// <summary>A rect, not a polygon: Godot fails to triangulate one this small and logs it every frame, which cost a third of a frame.</summary>
    private void Square(Vector2 at, float side, float alpha)
    {
        if (side <= 0 || alpha <= 0.004f) return;
        DrawRect(new Rect2(at.X - side / 2, at.Y - side / 2, side, side), Tint with { A = alpha });
    }
}
