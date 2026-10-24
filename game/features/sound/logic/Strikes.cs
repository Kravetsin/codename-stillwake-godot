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

/// <summary>
/// A one-shot struck in step with a picture (0059): the Former's stroke as its press head comes down. Its pictures go by in
/// steps, picture <c>step % frames</c>; a stroke is due each time the steps since the last frame pass picture <c>down</c>,
/// however many a slow frame skipped (one at most), and on the first frame it is shown on that picture.
/// </summary>
public struct FrameBeat
{
    private long _last;
    private bool _on;

    /// <summary>Whether the picture came to <paramref name="down"/> of <paramref name="frames"/> by step <paramref name="step"/>, while <paramref name="on"/>.</summary>
    public bool Struck(bool on, long step, int frames, int down)
    {
        if (!on)
        {
            _on = false;
            return false;
        }
        long from = _on ? _last + 1 : step;
        (_last, _on) = (step, true);
        if (step < from) return false;
        long next = from + (((down - from) % frames) + frames) % frames;
        return next <= step;
    }
}

/// <summary>
/// One-shots at random moments while something works, <c>rate</c> a second on average (0059): the smelter's melt bubbling. The
/// gaps between them are drawn from <c>random</c> as a Poisson process's are, the first one too, so nothing plays the moment
/// it starts; a stall owes at most <see cref="StrikeTrain.MostPerFrame"/>.
/// </summary>
public struct RandomTrain
{
    private double _wait;
    private bool _on;

    /// <summary>How many are due this frame of <paramref name="elapsedS"/> seconds, while <paramref name="on"/>.</summary>
    public int Due(double elapsedS, bool on, double rate, Random random)
    {
        if (!on)
        {
            _on = false;
            return 0;
        }
        if (!_on) (_wait, _on) = (Gap(rate, random), true);
        _wait -= elapsedS;
        int due = 0;
        while (_wait <= 0 && due < StrikeTrain.MostPerFrame)
        {
            due++;
            _wait += Gap(rate, random);
        }
        // Whatever is still owed after a stall is dropped, not played later.
        if (_wait <= 0) _wait = Gap(rate, random);
        return due;
    }

    /// <summary>A gap before the next, in seconds: exponential, <paramref name="rate"/> a second on average.</summary>
    public static double Gap(double rate, Random random) => -Math.Log(1 - random.NextDouble()) / rate;
}
