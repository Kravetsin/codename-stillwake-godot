using System;

namespace Stillwake.Game.Features.Sound;

/// <summary>
/// A train of one-shots heard while something works, apart from what it does (0054): the drill's strikes, a
/// hammer drill's buzz rather than its damage's ten a second. While on, a strike is due every 1/<c>rate</c> s,
/// the first the moment it starts; off, none, and the next start begins afresh. A stall owes at most
/// <see cref="MostPerFrame"/>, never a burst.
/// </summary>
public struct StrikeTrain
{
    /// <summary>The most strikes one frame plays, however long it took.</summary>
    public const int MostPerFrame = 2;

    private double _owed;
    private bool _on;

    /// <summary>How many strikes are due this frame of <paramref name="elapsedMs"/>, at <paramref name="rate"/> a second, while <paramref name="on"/>.</summary>
    public int Due(double elapsedMs, bool on, double rate)
    {
        if (!on)
        {
            _on = false;
            return 0;
        }
        // Just begun: the first strike at once.
        _owed = _on ? _owed + elapsedMs * rate / 1000 : 1;
        _on = true;
        int due = (int)Math.Floor(_owed);
        _owed -= due;
        return Math.Min(due, MostPerFrame);
    }

    /// <summary>
    /// A strike's pitch and volume: the pitch scale up to <paramref name="pitch"/> either way of 1, the volume up
    /// to <paramref name="volumeDb"/> either way of its cue's, each drawn evenly from <paramref name="random"/>.
    /// </summary>
    public static (float Pitch, float OffsetDb) Vary(Random random, float pitch, float volumeDb) =>
        ((float)(1 + (random.NextDouble() * 2 - 1) * pitch), (float)((random.NextDouble() * 2 - 1) * volumeDb));
}

/// <summary>
/// A loop's volume envelope: on, it starts and rises to full in <c>attack</c> seconds; off, it falls to nothing in
/// <c>release</c> seconds and then stops (the jetpack's thrust, the charging station's hum). Linear in amplitude, as
/// the audition's envelopes were.
/// </summary>
public struct LoopEnvelope
{
    /// <summary>How far up the envelope is, 0 to 1.</summary>
    public double Share { get; private set; }

    /// <summary>Whether the loop plays: from the frame it is turned on till its release has run out.</summary>
    public bool Playing { get; private set; }

    /// <summary>The envelope moved on <paramref name="elapsedS"/> seconds, <paramref name="on"/> or not.</summary>
    public void Step(bool on, double elapsedS, double attack, double release)
    {
        Share = Hearing.Ease(Share, on, elapsedS, attack, release);
        Playing = on || Share > 0;
    }

    /// <summary>The envelope's gain in decibels on top of the cue's: none at full, very quiet near nothing.</summary>
    public readonly float GainDb => (float)(20 * Math.Log10(Math.Max(Share, Hearing.Faint)));
}
