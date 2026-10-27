using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using L = Stillwake.Game.DroneLook;

namespace Stillwake.Game.Features.Logistics;

/// <summary>
/// A flying drone's light and wash (0062, 0075): over each rotor a faint disc of light for its blur, a glint running round
/// its rim; the air its rotors push is seen only as the air under the nearest drones bending (heat shimmer's wobble) and,
/// low over a floor, fine dust and faint veils spreading sideways along it, nothing falling through the air (slice 3: the
/// streaks once pushed down read as rain); its work lamp's short soft trail while it flies. Setting down or lifting off a
/// pad, a puff of dust and a faint flat ring. Restrained: a few drones about a room stay quiet. In pixels of the world,
/// through <see cref="Hover.Body"/>.
/// </summary>
public partial class DroneLight : Node2D
{
    /// <summary>
    /// Dust motes and faint veils a second where a drone's wash meets a floor at full spin, at its lowest, times
    /// <see cref="Fx.Count"/>; a third as many at the edge of <see cref="WashReach"/>, the cells over a floor it stirs.
    /// </summary>
    public const double DustPerSecond = 14, VeilsPerSecond = 2, WashReach = 5;
    /// <summary>The most drones whose air shimmers: each reads the screen again.</summary>
    public const int Shimmers = 3;
    /// <summary>The rotors' middles, from the body's, as it faces right: either side and up.</summary>
    private const double RotorX = 5.5, RotorY = -5.2;
    /// <summary>The lamp's last points kept for its trail, and the speed under which it leaves none, pixels a second.</summary>
    private const int TrailPoints = 8;
    private const double TrailFrom = 8;
    private static readonly (int, int, int) Disc = (200, 225, 250), Rim = (215, 235, 255), Lamp = (125, 211, 252), Ring = (226, 232, 242);

    private sealed class Seen
    {
        public double Dust, Veils, Clock, Since;
        public readonly (double X, double Y)[] Lamp = new (double, double)[TrailPoints];
        public int Points;
        public bool Kept;
    }

    private readonly Dictionary<int, Seen> _seen = new();
    private readonly List<int> _gone = [];
    private readonly Shimmer[] _shimmers = new Shimmer[Shimmers];
    private readonly (double X, double Y)[] _rim = new (double, double)[4];
    private readonly (double X, double Y)[] _trail = new (double, double)[TrailPoints];
    private readonly List<(double Far, Vector2 At)> _near = [];
    private readonly Random _random = new();

    public DroneLight()
    {
        for (int i = 0; i < Shimmers; i++) AddChild(_shimmers[i] = new Shimmer { Rise = -18 });
    }

    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();

    /// <summary>Starts a frame: every drone shown in it is then told with <see cref="Drone"/>, and <see cref="End"/> lets go of the rest.</summary>
    public void Begin()
    {
        foreach (var s in _seen.Values) s.Kept = false;
        _near.Clear();
    }

    /// <summary>
    /// Drone <paramref name="id"/> as <paramref name="h"/> moves it, <paramref name="bob"/> pixels down, <paramref name="elapsedMs"/> on;
    /// <paramref name="floor"/> the floor under it, a row of pixels, none past a few cells.
    /// </summary>
    public void Drone(int id, Hover h, double bob, double? floor, double elapsedMs)
    {
        if (!_seen.TryGetValue(id, out var s)) _seen[id] = s = new Seen();
        s.Kept = true;
        s.Clock += elapsedMs;
        double spin = h.Spin;
        if (spin < 0.05) return;
        Rotors(h, bob, spin, s.Clock);
        Wash(s, h, spin, floor, elapsedMs);
        LampTrail(s, h, bob, elapsedMs);
        var middle = h.Body(0, 8, bob);
        var camera = GetViewport().GetCamera2D()?.GetScreenCenterPosition() ?? Vector2.Zero;
        var at = new Vector2((float)middle.X, (float)middle.Y);
        _near.Add((at.DistanceSquaredTo(camera), at));
    }

    /// <summary>Lets go of the drones not told this frame; the nearest few shimmer.</summary>
    public void End()
    {
        _gone.Clear();
        foreach (var (id, s) in _seen)
            if (!s.Kept) _gone.Add(id);
        foreach (int id in _gone) _seen.Remove(id);
        _near.Sort((a, b) => a.Far.CompareTo(b.Far));
        for (int i = 0; i < Shimmers; i++)
        {
            if (i < _near.Count) _shimmers[i].Box = new Rect2(_near[i].At - new Vector2(12, 11), new Vector2(24, 26));
            _shimmers[i].Amplitude = i < _near.Count ? 0.4f : 0;
        }
    }

    /// <summary>A puff of dust either way and a faint flat ring where a drone sets down on or lifts off <paramref name="pad"/>.</summary>
    public static void Touched(Vector2 pad)
    {
        var glims = Stages.Of(Layers.Drones).Glims;
        double floor = pad.Y + L.DockHeight / 2.0;
        foreach (int side in (ReadOnlySpan<int>)[-1, 1])
            for (int i = 0; i < 3; i++)
                glims.Add(Luminous.WashDust, pad.X + side * glims.Between(2, 6), floor - 0.5, side * glims.Between(8, 22), -glims.Between(0, 4), floor);
        Stages.Of(Layers.Drones).Flares.Shock(pad.X, floor - 0.3, 2, 2 + 12 * Fx.Length, 9 * Glim.SampleMs, Ring, 0.22 * Fx.Glow, 0.26, 0.2);
    }

    /// <summary>Each rotor's disc of blur and the glint running round its rim, as bright as it spins.</summary>
    private void Rotors(Hover h, double bob, double spin, double clockMs)
    {
        for (int i = 0; i < 2; i++)
        {
            double rx = i == 0 ? -RotorX : RotorX;
            var (cx, cy) = h.Body(rx, RotorY, bob);
            Stages.Of(Layers.Drones).Flares.Bloom(cx, cy, 10, Disc, (0.14 * Fx.Glow + 0.04) * spin, 0.26);
            // The rim's glint: four points a quarter of the way round an ellipse, turning at the blades' pace.
            double turn = clockMs / Glim.SampleMs * 1.7 * spin + i * 2.1;
            for (int k = 0; k < _rim.Length; k++)
            {
                double u = turn - k * 0.28;
                _rim[k] = h.Body(rx + Math.Cos(u) * 4.4, RotorY + Math.Sin(u) * 1.05, bob);
            }
            Stages.Of(Layers.Drones).Flares.Trail(_rim, Rim, 0.6, 0.5 * spin, 0.3);
        }
    }

    /// <summary>
    /// The air its rotors push, seen only where it meets a floor within <see cref="WashReach"/>: fine dust creeping out
    /// sideways along it and now and then a faint veil curling, more the lower the drone; nothing falls through the air.
    /// </summary>
    private void Wash(Seen s, Hover h, double spin, double? floor, double elapsedMs)
    {
        if (floor is not double f || f - h.Y > WashReach * ShipCamera.Cell) return;
        var glims = Stages.Of(Layers.Drones).Glims;
        double near = 1 - Math.Max(0, f - h.Y) / (WashReach * ShipCamera.Cell), rate = spin * Fx.Count * (0.35 + 0.65 * near) * elapsedMs / 1000;
        for (s.Dust += DustPerSecond * rate; s.Dust >= 1; s.Dust--)
        {
            int side = _random.NextDouble() < 0.5 ? -1 : 1;
            glims.Add(Luminous.WashDust, h.X + side * Between(1, 6), f - 0.4, side * Between(28, 60), -Between(0, 4), f, size: Between(0.7, 1.05));
        }
        for (s.Veils += VeilsPerSecond * rate; s.Veils >= 1; s.Veils--)
        {
            int side = _random.NextDouble() < 0.5 ? -1 : 1;
            glims.Add(Luminous.WashVeil, h.X + side * Between(1, 5), f - 1, side * Between(18, 32), -Between(0, 2), f, bright: 0.6);
        }
    }

    /// <summary>The work lamp's last few points, and while the drone flies fast enough, a short soft trail through them.</summary>
    private void LampTrail(Seen s, Hover h, double bob, double elapsedMs)
    {
        // Sampled every 60th of a second, as a streak's path is, whatever the frame rate; its head where the lamp is now.
        if ((s.Since += elapsedMs) >= Glim.SampleMs || s.Points == 0)
        {
            s.Since = 0;
            Array.Copy(s.Lamp, 0, s.Lamp, 1, TrailPoints - 1);
            s.Points = Math.Min(TrailPoints, s.Points + 1);
        }
        s.Lamp[0] = h.Body(L.LampX - L.AnchorX, L.LampY - L.AnchorY, bob);
        double speed = Math.Sqrt(h.Vx * h.Vx + h.Vy * h.Vy);
        if (speed < TrailFrom || s.Points < 3) return;
        int n = Math.Max(2, (int)Math.Round(s.Points * Math.Min(1, Fx.Length)));
        Array.Copy(s.Lamp, _trail, n);
        Stages.Of(Layers.Drones).Flares.Trail(_trail.AsSpan(0, n), Lamp, 0.5, Math.Min(0.5, speed / 90) * Fx.Glow, 0.7);
    }
}
