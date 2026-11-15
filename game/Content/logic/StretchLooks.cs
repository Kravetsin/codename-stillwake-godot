using System;
using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>
/// How a stretch of space (docs/plans/stretches.md) is seen coming, drawn only, without a word: the space behind the ship washed in
/// its colour, the radar's band in its own, frost on the hull before the ice belt. All of it from the sky's <c>SkyNow</c>, so a
/// peer joining mid-warning sees what the others do.
/// </summary>
public static class StretchLooks
{
    /// <summary>The ice belt's block and the contraband drift's crate: the wrecks the stretches bring, drawn their own way.</summary>
    public const string IceBlock = "iceBlock", ContrabandCrate = "contrabandCrate";

    /// <summary>
    /// A stretch's look: <paramref name="Wash"/> laid over the black of space, at most <see cref="WashMost"/> of it; the radar's band in
    /// <paramref name="Band"/>; whether frost creeps over the hull before it (<paramref name="Frost"/>).
    /// </summary>
    public sealed record Look(string Id, (float R, float G, float B) Wash, (float R, float G, float B) Band, bool Frost);

    /// <summary>The ice belt blue-grey and frosting the hull; the contraband drift amber-red.</summary>
    public static readonly IReadOnlyList<Look> All =
    [
        new(Content.Stretches.IceBelt.Id, (0.46f, 0.6f, 0.74f), (0.64f, 0.8f, 0.92f), Frost: true),
        new(Content.Stretches.ContrabandDrift.Id, (0.82f, 0.36f, 0.16f), (0.98f, 0.58f, 0.26f), Frost: false),
    ];

    /// <summary>How much of space the wash covers at its full, over the stars' black.</summary>
    public const float WashMost = 0.3f;

    /// <summary>Ticks a stretch's look takes to ease back once it has gone: the frost melts, the wash and the band fade (a minute).</summary>
    public const int Thaw = 60 * 20;

    /// <summary>How long the hull stays wet after the frost, in <see cref="Thaw"/>s from its end: the pick's 3.2 s to its 2.2.</summary>
    public const double Dry = 1.45;

    /// <summary>The look of stretch <paramref name="id"/>; none for none or one unknown.</summary>
    public static Look? Of(string? id)
    {
        if (id == null) return null;
        foreach (var look in All)
            if (look.Id == id) return look;
        return null;
    }
}

/// <summary>
/// One view's reading of the stretch shown: which, how far its warning has run (<see cref="Grow"/>), and once it has gone how far
/// its look has eased back (<see cref="After"/>). Read from what the sky says now, every frame or on a panel's timer; only the
/// easing back is counted here, from <paramref name="clock"/> in ticks, so a peer that joins after a stretch ended sees none.
/// </summary>
public sealed class StretchShown
{
    private double? _ended;

    /// <summary>The stretch shown, coming, running or easing back; none.</summary>
    public StretchLooks.Look? Look { get; private set; }

    /// <summary>The share of its warning run, 0 at its start to 1 as it comes; 1 while it runs and after.</summary>
    public float Grow { get; private set; }

    /// <summary>0 till it has gone; then the ticks since, in <see cref="StretchLooks.Thaw"/>s, to <see cref="StretchLooks.Dry"/>.</summary>
    public double After { get; private set; }

    /// <summary>How far it has eased back, 0 to 1 over <see cref="StretchLooks.Thaw"/>.</summary>
    public float Melt => (float)Math.Min(1, After);

    /// <summary>The share of its wash and band shown: its warning's run, eased back after.</summary>
    public float Shown => Grow * (1 - Melt);

    /// <summary>
    /// The sky now: <paramref name="running"/>, the stretch running, else <paramref name="coming"/> with <paramref name="comingIn"/> ticks
    /// of its warning left (<c>SkyNow</c>); <paramref name="clock"/> the world's ticks, between them too.
    /// </summary>
    public void See(string? running, string? coming, int comingIn, double clock)
    {
        if ((running ?? coming) is { } id)
        {
            Look = StretchLooks.Of(id);
            Grow = running != null ? 1 : Math.Clamp(1 - comingIn / (float)Content.Stretches.Warning, 0, 1);
            (After, _ended) = (0, null);
            return;
        }
        if (Look == null) return;
        _ended ??= clock;
        After = Math.Max(0, (clock - _ended.Value) / StretchLooks.Thaw);
        if (After < StretchLooks.Dry) return;
        (Look, Grow, After, _ended) = (null, 0, 0, null);
    }
}
