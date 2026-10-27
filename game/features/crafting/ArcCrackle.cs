using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crafting;

/// <summary>
/// A station with the arc module fitted, at work (0086): violet-white arcs crawling along its body's outline, crisp lines
/// of <see cref="Bolts"/> in <see cref="BoltTint.Violet"/> bowed out from it, now and then a burst of two or three and a
/// longer one leaping from a top corner down its side; the room round it lit violet-white, flickering. One look for every
/// station: its body taken as its footprint below its top third, which on the smelter, the Former and the assembler is
/// their box. Stopped, nothing and no light. In pixels of the world, from the machine's top-left.
/// </summary>
public partial class ArcCrackle : Node2D
{
    /// <summary>Between arcs, at least and at most, in ms; the chance one is a burst, and one leaps down a side.</summary>
    private const double LeastMs = 70, MostMs = 260, Burst = 0.35, Leap = 0.2;
    /// <summary>How far in from the footprint's sides the body's outline runs, in pixels, and how far down its top lies, a share.</summary>
    private const float Inset = 2.5f, Top = 0.3f;
    /// <summary>The light's strength at its dimmest and brightest flicker.</summary>
    private const float Dim = 0.35f, Bright = 0.85f;
    private static readonly Color Light = new(0.8f, 0.64f, 1f);

    private readonly Random _random = new();
    private readonly Glow _glow = new();
    private double _clock, _next;

    public ArcCrackle()
    {
        AddChild(_glow);
        _next = Between(0, MostMs);
    }

    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();

    /// <summary>The station at <paramref name="corner"/>, <paramref name="size"/> in pixels, at work <paramref name="elapsedMs"/> on, or stopped.</summary>
    public void Step(Vector2 corner, Vector2 size, bool works, double elapsedMs)
    {
        _glow.Visible = works;
        if (!works) return;
        _clock += elapsedMs;
        float top = size.Y * Top;
        // A quick flutter over a slower swell: an arc's light is never still.
        double flutter = 0.5 + 0.5 * Math.Sin(_clock / 47 + 2 * Math.Sin(_clock / 173));
        _glow.Position = corner + new Vector2(size.X / 2, top);
        _glow.Colour = Light * (float)(Dim + (Bright - Dim) * flutter);
        Stages.Of(Layers.Machines).Flares.Bloom(corner.X + size.X / 2, corner.Y + top, size.X * 0.7, (200, 160, 255), (0.08 + 0.06 * flutter) * Fx.Glow, 0.6);
        if (_clock < _next) return;
        _next = _clock + Between(LeastMs, MostMs);
        for (int n = _random.NextDouble() < Burst ? _random.Next(2, 4) : 1; n > 0; n--)
        {
            if (_random.NextDouble() < Leap) LeapDown(corner, size, top);
            else Crawl(corner, size, top);
        }
    }

    /// <summary>
    /// A short arc along the body's outline, four to ten pixels, bowed out from it: along its top mostly, else down the upper
    /// half of a side; two to five frames.
    /// </summary>
    private void Crawl(Vector2 corner, Vector2 size, float top)
    {
        float left = Inset, right = size.X - Inset, length = (float)Between(4, 10);
        Vector2 a, b;
        double bow;
        int edge = _random.Next(4);
        if (edge < 2)
        {
            float x = (float)Between(left, right - length);
            (a, b) = (new(x, top), new(x + length, top));
            // Left to right, a fold of negative share bows up, out of the body.
            bow = -Between(0.15, 0.45);
        }
        else
        {
            bool onRight = edge == 3;
            float x = onRight ? right : left, y = (float)Between(top, top + (size.Y - top) * 0.5f);
            (a, b) = (new(x, y), new(x, y + length));
            // Downwards, a positive share bows left: out of the left side, in from the right.
            bow = (onRight ? -1 : 1) * Between(0.15, 0.45);
        }
        var (p, q) = (corner + a, corner + b);
        Stages.Of(Layers.Machines).Bolts.Add(() => (p.X, p.Y, q.X, q.Y, bow), _random.Next(2, 6), Between(0.85, 1), 0.65,
            branchy: _random.NextDouble() < 0.35, tint: BoltTint.Violet);
    }

    /// <summary>A longer arc from a top corner of the body down that side, bowed out from it: four to eight frames.</summary>
    private void LeapDown(Vector2 corner, Vector2 size, float top)
    {
        bool right = _random.Next(2) == 0;
        float x = right ? size.X - Inset : Inset;
        var p = corner + new Vector2(x, top);
        var q = corner + new Vector2(x, (float)Between(top + (size.Y - top) * 0.6f, size.Y - 1));
        double bow = (right ? -1 : 1) * Between(0.2, 0.4);
        Stages.Of(Layers.Machines).Bolts.Add(() => (p.X, p.Y, q.X, q.Y, bow), _random.Next(4, 9), 1, 0.8, branchy: true, tint: BoltTint.Violet);
    }
}
