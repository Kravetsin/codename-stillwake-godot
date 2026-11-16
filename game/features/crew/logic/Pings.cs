using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A ping as sent with its hero (<see cref="Seen"/>): where, in thousandths of a cell, and how many its player has placed, so a new one
/// is news even on the same spot. Sent for as long as it lasts, a point that holds for the whole state, as the rod's is.
/// </summary>
public readonly record struct PingSeen(Point At, int Count);

/// <summary>
/// The spot a player marked with a tap of the gesture key, in its colour, for the crew to see: one a player, the next replacing it,
/// gone after <see cref="LastsMs"/>; so it needs no cooldown. Placed by this machine's keys or heard from a friend's peer, each on
/// its own clock.
/// </summary>
public sealed class Ping
{
    /// <summary>How long a ping shows, how long it takes to come in, and the fade out at its end, in milliseconds.</summary>
    public const double LastsMs = 8000, InMs = 150, OutMs = 1200;

    /// <summary>Where it is, in thousandths of a cell; none while none shows.</summary>
    public Point? At { get; private set; }

    /// <summary>How many pings its player has placed.</summary>
    public int Count { get; private set; }

    /// <summary>How long it has shown, in milliseconds.</summary>
    public double Ms { get; private set; }

    /// <summary>A new ping at <paramref name="at"/>, in place of the one before.</summary>
    public void Place(Point at)
    {
        (At, Ms) = (at, 0);
        Count++;
    }

    public void Clear() => At = null;

    /// <summary>Shows on by <paramref name="elapsedMs"/>; it goes as its time runs out.</summary>
    public void Step(double elapsedMs)
    {
        if (At == null) return;
        Ms += elapsedMs;
        if (Ms >= LastsMs) At = null;
    }

    /// <summary>What is sent of it while it shows.</summary>
    public PingSeen? Sent => At is { } at ? new PingSeen(at, Count) : null;

    /// <summary>Follows a friend's by what its peer sent: one numbered anew shows here from its start; the same one plays on.</summary>
    public void Hear(PingSeen? seen)
    {
        if (seen is { } sent && sent.Count != Count) (At, Count, Ms) = (sent.At, sent.Count, 0);
    }

    /// <summary>How much of it shows, 0 to 1: up over <see cref="InMs"/>, down over the last <see cref="OutMs"/>; none while none is.</summary>
    public double Shown => At == null ? 0 : Math.Clamp(Math.Min(Ms / InMs, (LastsMs - Ms) / OutMs), 0, 1);
}
