using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crafting;

/// <summary>
/// An electric smelter at work, in light (0075), one a smelter, over its coils' own glow: the heating element breathing
/// slowly in its window, a soft warm bloom swelling and easing; the air over it shimmering faintly with the heat; and,
/// rarely, a tiny blue arc snapping across the element's contacts. Restrained: a smelter at work is steady, not busy.
/// In pixels of the world, from the machine's top-left.
/// </summary>
public partial class SmelterHeat : Node2D
{
    /// <summary>The element's window in <c>machine_smelter.png</c>, its middle and size, in pixels; where its coils' tops meet their contacts.</summary>
    private static readonly Vector2 Window = new(16, 18), WindowSize = new(16, 10);
    private static readonly float[] Contacts = [10, 13, 18, 21];
    private const float ContactsY = 13;
    /// <summary>How long a breath of heat takes, in ms, and how long between arcs, at least and at most.</summary>
    private const double BreathMs = 3200, ArcLeastMs = 2500, ArcMostMs = 6500;
    private static readonly (int, int, int) Heat = (255, 120, 50);

    private readonly Shimmer _shimmer = new() { Rise = 8 };
    private readonly Random _random = new();
    private double _clock, _nextArc;

    public SmelterHeat()
    {
        AddChild(_shimmer);
        _nextArc = Between(ArcLeastMs, ArcMostMs);
    }

    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();

    /// <summary>The smelter at <paramref name="corner"/> at work <paramref name="elapsedMs"/> on, or stopped with <paramref name="works"/> false.</summary>
    public void Step(Vector2 corner, bool works, double elapsedMs)
    {
        _shimmer.Box = new Rect2(corner + new Vector2(6, 4), new Vector2(20, 22));
        _shimmer.Amplitude = works ? 0.35f : 0;
        if (!works) return;
        _clock += elapsedMs;
        double breath = 0.5 - 0.5 * Math.Cos(_clock / BreathMs * Math.Tau);
        var middle = corner + Window;
        Flares.Shared.Bloom(middle.X, middle.Y, WindowSize.X * 1.3, Heat, (0.1 + 0.12 * breath) * Fx.Glow, 0.75);
        if (_clock < _nextArc) return;
        _nextArc = _clock + Between(ArcLeastMs, ArcMostMs);
        // Across a pair of contacts at the top of the element, bowed up: a snap, three to five frames.
        int pair = _random.Next(2);
        var from = corner + new Vector2(Contacts[pair * 2], ContactsY);
        var to = corner + new Vector2(Contacts[pair * 2 + 1], ContactsY);
        Bolts.Shared.Add(() => (from.X, from.Y, to.X, to.Y, -0.5), 3 + _random.Next(3), 0.9, 0.3);
    }
}
