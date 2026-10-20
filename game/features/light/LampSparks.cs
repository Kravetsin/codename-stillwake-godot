using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// A lamp short of power, in light (0075), over its <see cref="Flicker"/>, whose timing and fades it keeps: each time it
/// catches again, a small filament glint, and more often than not a short shower of sparks dropping from it, arcing down
/// and bouncing once, lighting the wall as they fall, and, half the time, a short arc or two crackling at its socket, the electric
/// spear's bolts in miniature; rarely an arc while it is lit; while dark, a dull orange ember in the filament,
/// cooling. Kept easy on the eyes: the glint is small and warm, the arcs short, nothing flashes the room white.
/// </summary>
public sealed class LampSparks
{
    /// <summary>The brightness a lamp catches at, of its full light.</summary>
    private const float Catches = 0.3f;
    /// <summary>The chance a catch drops sparks, and how many; arcs a second while it is lit; how long its ember cools, in ms.</summary>
    private const double ShowerChance = 0.6, ArcChance = 0.5, ArcsPerSecond = 0.4, EmberMs = 1000;
    /// <summary>Where its bulb's middle is from the middle of its cell, and its socket, in pixels (<c>machine_lamp.png</c>).</summary>
    private static readonly Vector2 Bulb = new(0, 1), Socket = new(0, -2);
    private static readonly (int, int, int) Filament = (255, 220, 160), Ember = (255, 110, 40);

    private sealed class Seen
    {
        public float Last;
        public double DarkMs = EmberMs;
        public bool Kept;
    }

    private readonly Dictionary<int, Seen> _seen = new();
    private readonly List<int> _gone = [];
    private readonly Random _random = new();

    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();

    public void Begin()
    {
        foreach (var s in _seen.Values) s.Kept = false;
    }

    /// <summary>
    /// Lamp <paramref name="id"/> at <paramref name="at"/>, its cell's middle, shining at <paramref name="shines"/> of its full light
    /// <paramref name="elapsedMs"/> on; <paramref name="floor"/> the floor under it, a row of pixels.
    /// </summary>
    public void Lamp(int id, Vector2 at, float shines, double floor, double elapsedMs)
    {
        if (!_seen.TryGetValue(id, out var s)) _seen[id] = s = new Seen { Last = shines };
        s.Kept = true;
        var bulb = at + Bulb;
        if (shines > Catches && s.Last <= Catches) Caught(at, bulb, floor);
        if (shines <= Catches && s.Last > Catches) s.DarkMs = 0;
        if (shines > Catches && _random.NextDouble() < ArcsPerSecond * elapsedMs / 1000) Arc(at);
        if (shines <= Catches)
        {
            s.DarkMs += elapsedMs;
            Flares.Shared.Bloom(bulb.X, bulb.Y - 0.3, 3.2, Ember, 0.85 * Math.Max(0, 1 - s.DarkMs / EmberMs) * (1 - shines));
        }
        s.Last = shines;
    }

    /// <summary>Lets go of the lamps not told since <see cref="Begin"/>.</summary>
    public void End()
    {
        _gone.Clear();
        foreach (var (id, s) in _seen)
            if (!s.Kept) _gone.Add(id);
        foreach (int id in _gone) _seen.Remove(id);
    }

    /// <summary>The lamp catching: a glint, a shower more often than not, half the time an arc or two at its socket.</summary>
    private void Caught(Vector2 at, Vector2 bulb, double floor)
    {
        Flares.Shared.Glint(bulb.X, bulb.Y - 0.5, 5, Filament);
        if (_random.NextDouble() < ShowerChance)
        {
            var glims = Glims.Shared;
            int sparks = (int)Math.Round(Between(3, 6) * Fx.Count);
            for (int i = 0; i < sparks; i++)
                glims.Add(Luminous.LampSpark, bulb.X + Between(-2, 2), bulb.Y + 2.5, Between(-28, 28), Between(-15, 25), floor);
        }
        if (_random.NextDouble() < ArcChance)
            for (int i = 1 + _random.Next(2); i > 0; i--) Arc(at);
    }

    /// <summary>A short arc from the socket's side to the ceiling over it or out beside the bulb, crackling for a few frames.</summary>
    private void Arc(Vector2 at)
    {
        int side = _random.NextDouble() < 0.5 ? -1 : 1;
        var socket = at + Socket;
        var from = socket + new Vector2(side * (float)Between(1, 3), (float)Between(-1, 1));
        var to = _random.NextDouble() < 0.5
            ? new Vector2(at.X + side * (float)Between(4, 8), at.Y - ShipCamera.Cell / 2f + (float)Between(0, 1))
            : at + Bulb + new Vector2(side * (float)Between(4, 7), (float)Between(-2, 3));
        float jx = (float)Between(-0.5, 0.5), kx = (float)Between(-1, 1);
        double bulge = side * 0.2;
        Bolts.Shared.Add(() => (from.X + jx, from.Y, to.X + kx, to.Y, bulge), 4 + _random.Next(6), 1, 0.28, branchy: true);
    }
}
