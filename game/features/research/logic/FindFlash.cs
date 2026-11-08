using System;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The flash that says a note has started waiting on the board, seen by this player only (edge-flash): light runs in from both ends of the tablet's edge
/// to the research key, and the key flares as it lands and its number takes the finds. One flash for a batch: finds while one is under way, or until it
/// has been quiet a while, go onto the number with no flash of their own. Times are seconds since the flash began.
/// </summary>
public sealed class FindFlash
{
    /// <summary>How long the light takes to run in to the key, and how long the key glows after.</summary>
    public const double WaveS = 0.45, GlowS = 0.6;

    /// <summary>How long after a flash began a find flashes nothing of its own.</summary>
    public const double QuietS = 1.5;

    private object? _seen;
    private int _unread;

    /// <summary>Seconds since the flash began, while a find flashes nothing; null after.</summary>
    public double? Time { get; private set; }

    /// <summary>The finds that came before the light reached the key.</summary>
    private int _held;

    /// <summary>
    /// A read of what is new on the board: <paramref name="seen"/> is whose, so another world's starts over with nothing new. Open, the tablet shows it
    /// all, and nothing flashes.
    /// </summary>
    public void Read(object seen, int unread, bool open)
    {
        int more = seen == _seen ? unread - _unread : 0;
        (_seen, _unread) = (seen, unread);
        if (open) (Time, _held) = (null, 0);
        else if (more <= 0) return;
        else if (Time is not { } t) (Time, _held) = (0, more);
        else if (t < WaveS) _held += more;
    }

    /// <summary>Moves it <paramref name="seconds"/> on.</summary>
    public void Step(double seconds)
    {
        if (Time is not { } t) return;
        t += seconds;
        Time = t >= QuietS ? null : t;
    }

    /// <summary>The finds not yet on the key's number: held while the light runs in. The number is the unread less these.</summary>
    public int Holding => Time is { } t && t < WaveS ? _held : 0;

    /// <summary>How far the light has run in, 0 at the edge's ends to 1 at the key; null outside the run.</summary>
    public double? Wave => Time is { } t && t < WaveS ? EaseIn(t / WaveS) : null;

    /// <summary>How brightly the key flares, 1 as the light lands, fading to 0.</summary>
    public double Glow => Time is { } t && t >= WaveS ? Math.Max(0, 1 - (t - WaveS) / GlowS) : 0;

    // Gathering speed toward the key, so the eye is pulled along to it.
    private static double EaseIn(double t) => t * t;
}
