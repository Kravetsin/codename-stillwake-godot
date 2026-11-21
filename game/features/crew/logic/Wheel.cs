using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>What every key's wheel shares (<see cref="Wheel{T}"/>): its timing and sizes, and how an offset reads as a side.</summary>
public static class Wheel
{
    /// <summary>A press let go sooner than this is a tap, unless it moved out of the middle; held longer, the wheel shows.</summary>
    public const double TapMs = 200;

    /// <summary>The middle's radius, where letting go picks nothing, and how far out each side sits, in pixels at the normal slot size.</summary>
    public const double Middle = 18, Radius = 54;

    /// <summary>
    /// The side of <paramref name="count"/> an offset <paramref name="dx"/>, <paramref name="dy"/> from the middle points to, screen down
    /// positive, from the top clockwise: the one whose even share of the turn, centred on it, holds the offset's angle; none within the middle.
    /// </summary>
    public static int? SideOf(double dx, double dy, double middle, int count)
    {
        if (dx * dx + dy * dy < middle * middle) return null;
        double share = Math.Tau / count;
        int side = (int)Math.Floor((Math.Atan2(dx, -dy) + share / 2) / share);
        return (side % count + count) % count;
    }
}

/// <summary>
/// A key's wheel (gestures-for-words, light-drone): a tap asks for what the caller says a tap is; held, a wheel opens round where the pointer
/// was, its <paramref name="sides"/> shared out from the top clockwise, and letting go picks the one the pointer has moved toward, or none from
/// the middle or from a side shut (<see cref="Opens"/>). A quick flick toward one picks it without waiting. Distances are the screen's pixels,
/// the caller's to scale.
/// </summary>
public sealed class Wheel<T>(IReadOnlyList<T> sides) where T : struct
{
    private bool _held;
    private double _heldMs, _middle = Wheel.Middle;

    /// <summary>The sides, from the top clockwise.</summary>
    public IReadOnlyList<T> Sides => sides;

    /// <summary>Whether a side may be picked now: one that may not is drawn shut and never picked. Every side, unless set.</summary>
    public Func<T, bool> Opens { get; set; } = _ => true;

    /// <summary>Where the key was pressed, and where the pointer is now, on the screen.</summary>
    public (double X, double Y) From { get; private set; }
    public (double X, double Y) Pointer { get; private set; }

    /// <summary>Whether the wheel shows: the key held past a tap.</summary>
    public bool Open => _held && _heldMs >= Wheel.TapMs;

    /// <summary>The index of the side the pointer has moved toward, open or shut; none in the middle.</summary>
    public int? TowardAt => Wheel.SideOf(Pointer.X - From.X, Pointer.Y - From.Y, _middle, sides.Count);

    /// <summary>The side the pointer has moved toward, picked if the key is let go now; none in the middle or on a shut side.</summary>
    public T? Toward => TowardAt is int at && Opens(sides[at]) ? sides[at] : null;

    /// <summary>
    /// Follows the key, <paramref name="held"/> or not, and the pointer at <paramref name="pointer"/> on the screen, the middle
    /// <paramref name="middle"/> pixels across: the side asked for as the key is let go, or <paramref name="tap"/> for a tap; else none.
    /// </summary>
    public T? Step(bool held, double elapsedMs, (double X, double Y) pointer, double middle, T? tap)
    {
        (Pointer, _middle) = (pointer, middle);
        if (held)
        {
            if (!_held) (_held, _heldMs, From) = (true, 0, pointer);
            else _heldMs += elapsedMs;
            return null;
        }
        if (!_held) return null;
        _held = false;
        if (TowardAt != null) return Toward;
        return _heldMs < Wheel.TapMs ? tap : null;
    }
}
