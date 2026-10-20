using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crafting;

/// <summary>
/// An electric smelter at work, in light (0075), one a smelter, over its coils' own glow: electric heating at work, steady
/// and restrained (slice 3). A faint crackle along the element, a fine arc along its top or bottom edge every so often and now
/// and then a little burst of two or three; a tiny hot fleck drifting up off it now and then; the element's glow pulsing
/// with the work; a small blue arc snapping across its contacts every second or two; the air over it shimmering faintly.
/// Stopped, nothing. In pixels of the world, from the machine's top-left.
/// </summary>
public partial class SmelterHeat : Node2D
{
    /// <summary>The element's window in <c>machine_smelter.png</c>, its middle and size, in pixels; where its coils' tops meet their contacts.</summary>
    private static readonly Vector2 Window = new(16, 18), WindowSize = new(16, 10);
    private static readonly float[] Contacts = [10, 13, 18, 21];
    private const float ContactsY = 13;
    /// <summary>The element's two stacks of coil, their left and right; its top and bottom edges, each a row and the way out of it (-1 up), in pixels.</summary>
    private static readonly (float From, float To)[] Stacks = [(9, 15), (17, 23)];
    private static readonly (float Y, int Bow)[] Edges = [(12.9f, -1), (23.4f, 1)];
    /// <summary>How long a pulse of the glow takes; between crackles, flecks and contact arcs, at least and at most; all in ms.</summary>
    private const double PulseMs = 1400, CrackleLeastMs = 300, CrackleMostMs = 1000, FleckLeastMs = 600, FleckMostMs = 1600,
        ArcLeastMs = 1000, ArcMostMs = 2500;
    /// <summary>The chance a crackle is a little burst of two or three arcs.</summary>
    private const double Burst = 0.25;
    private static readonly (int, int, int) Heat = (255, 120, 50);

    private readonly Shimmer _shimmer = new() { Rise = 8 };
    private readonly Random _random = new();
    private double _clock, _nextArc, _nextCrackle, _nextFleck;

    public SmelterHeat()
    {
        AddChild(_shimmer);
        _nextArc = Between(ArcLeastMs, ArcMostMs);
        _nextCrackle = Between(CrackleLeastMs, CrackleMostMs);
        _nextFleck = Between(FleckLeastMs, FleckMostMs);
    }

    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();

    /// <summary>The smelter at <paramref name="corner"/> at work <paramref name="elapsedMs"/> on, or stopped with <paramref name="works"/> false.</summary>
    public void Step(Vector2 corner, bool works, double elapsedMs)
    {
        _shimmer.Box = new Rect2(corner + new Vector2(6, 4), new Vector2(20, 22));
        _shimmer.Amplitude = works ? 0.35f : 0;
        if (!works) return;
        _clock += elapsedMs;
        // The glow pulses with the work: a slow swell, a quick flutter over it.
        double pulse = 0.5 - 0.5 * Math.Cos(_clock / PulseMs * Math.Tau), flutter = 0.85 + 0.15 * Math.Sin(_clock / 90 + 1.3 * Math.Sin(_clock / 230));
        var middle = corner + Window;
        Flares.Shared.Bloom(middle.X, middle.Y, WindowSize.X * 1.3, Heat, (0.1 + 0.14 * pulse) * flutter * Fx.Glow, 0.75);
        if (_clock >= _nextCrackle)
        {
            _nextCrackle = _clock + Between(CrackleLeastMs, CrackleMostMs);
            for (int n = _random.NextDouble() < Burst ? _random.Next(2, 4) : 1; n > 0; n--) Crackle(corner);
        }
        if (_clock >= _nextFleck)
        {
            _nextFleck = _clock + Between(FleckLeastMs, FleckMostMs);
            var (from, to) = Stacks[_random.Next(Stacks.Length)];
            var at = corner + new Vector2((float)Between(from, to), Edges[0].Y);
            Glims.Shared.Add(Luminous.HeatSpark, at.X, at.Y, Between(-4, 4), -Between(8, 18));
        }
        if (_clock < _nextArc) return;
        _nextArc = _clock + Between(ArcLeastMs, ArcMostMs);
        // Across a pair of contacts at the top of the element, bowed up: a snap, three to five frames.
        int pair = _random.Next(2);
        var a = corner + new Vector2(Contacts[pair * 2], ContactsY);
        var b = corner + new Vector2(Contacts[pair * 2 + 1], ContactsY);
        Bolts.Shared.Add(() => (a.X, a.Y, b.X, b.Y, -0.5), 3 + _random.Next(3), 0.9, 0.3);
    }

    /// <summary>
    /// A fine arc along the top or the bottom of the element, where its coils meet their terminals, bowed out over the dark
    /// frame, a couple of pixels, two to four frames: over the glowing coils themselves it would not show.
    /// </summary>
    private void Crackle(Vector2 corner)
    {
        var (y, bow) = Edges[_random.Next(Edges.Length)];
        var (from, to) = Stacks[_random.Next(Stacks.Length)];
        double x = Between(from, to - 2);
        var a = corner + new Vector2((float)x, y + (float)Between(-0.3, 0.3));
        var b = corner + new Vector2((float)(x + Between(1.4, 2.6)), y + (float)Between(-0.3, 0.3));
        double bulge = bow * Between(0.15, 0.45);
        Bolts.Shared.Add(() => (a.X, a.Y, b.X, b.Y, bulge), _random.Next(2, 5), Between(0.7, 1), width: 0.3, fine: true);
    }
}
