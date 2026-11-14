using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The spear's own effects, the user's picks (spear-effects): plain, a lens of squeezed air before the point on the thrust that
/// tears off as two eddies (<see cref="HazeAt"/>); with the arc fitted, the arc running down its cable to the point on the
/// thrust (<see cref="JabAt"/>), a violet wake and motes behind the moving blade (<see cref="SpearWake"/>) and charges rolling
/// down the cable all the while, faster on a run (<see cref="ChargeAt"/>). Times are ms into a thrust (<see cref="Strokes.Spear"/>).
/// </summary>
public static class SpearFx
{
    /// <summary>
    /// The haze at one moment, in pixels of the world from the point, x along the spear toward it, y across as its picture's:
    /// a lens of <c>Lens</c> strength centred at <c>LensX</c>, and two eddies of <c>Eddy</c> strength at <c>EddyX</c>,
    /// <c>EddyApart</c> either side of the shaft's line, spinning opposite ways, each its <c>Radius</c> across.
    /// </summary>
    public readonly record struct Haze(double Lens, double LensX, double LensRadius, double Eddy, double EddyX, double EddyApart, double EddyRadius);

    /// <summary>The lens's middle ahead of the point and its radius; how long it holds at the hit and dies after.</summary>
    public const double LensAhead = 1.8, LensRadius = 2.6, LensHoldMs = 15, LensFadeMs = 40;
    /// <summary>How long the eddies roll back, and how far; their radius at the start and what it grows by.</summary>
    public const double EddyMs = 260, EddyBack = 12, EddyRadius = 1.9, EddyGrows = 2.1;

    /// <summary>The haze <paramref name="ms"/> into a thrust; none while there is none.</summary>
    public static Haze? HazeAt(double ms)
    {
        double cock = Strokes.JabCockMs, hit = Strokes.JabHitMs;
        double lens = ms < cock || ms > hit + LensHoldMs + LensFadeMs ? 0
            : ms < hit ? Smooth((ms - cock) / (hit - cock)) : ms < hit + LensHoldMs ? 1 : 1 - (ms - hit - LensHoldMs) / LensFadeMs;
        double age = (ms - hit) / EddyMs, grown = Math.Clamp(age, 0, 1);
        double eddy = age < 0 || age > 1 ? 0 : Math.Min(1, age * 7) * Math.Pow(1 - age, 1.4);
        if (lens <= 0.01 && eddy <= 0.01) return null;
        double back = EddyBack * (1 - (1 - grown) * (1 - grown));
        return new(Math.Max(0, lens), LensAhead, LensRadius, eddy, -1.2 - back, 2 + 1.3 * grown, EddyRadius + EddyGrows * grown);
    }

    /// <summary>
    /// The arc on a thrust at one moment: the coil's glow <c>Charge</c>, 0 to 1, and its flick while <c>Flicks</c>; the arc's head
    /// <c>Head</c> of the way down the cable, the cable behind it lit at <c>Lit</c> (0 for none) and the arc itself at its head
    /// while <c>Running</c>; short arcs off the point at <c>Spits</c>.
    /// </summary>
    public readonly record struct Jab(double Charge, bool Flicks, double Head, double Lit, bool Running, double Spits);

    /// <summary>How long the coil's glow dies after the hit; the cable stays lit; the point spits arcs.</summary>
    public const double ChargeFadeMs = 50, LitFadeMs = 70, SpitMs = 60;
    /// <summary>The share of the cable the running arc covers behind its head.</summary>
    public const double RunLength = 0.16;

    /// <summary>The arc <paramref name="ms"/> into a thrust; none once it is over.</summary>
    public static Jab? JabAt(double ms)
    {
        double cock = Strokes.JabCockMs, hit = Strokes.JabHitMs;
        if (ms < 0 || ms >= hit + LitFadeMs) return null;
        double charge = ms < hit ? Smooth(Math.Clamp(ms / cock, 0, 1)) : 1 - Math.Clamp((ms - hit) / ChargeFadeMs, 0, 1);
        double head = ms < cock ? 0 : ms < hit ? Math.Pow((ms - cock) / (hit - cock), 1.3) : 1;
        double lit = ms < cock ? 0 : ms < hit ? 1 : 1 - (ms - hit) / LitFadeMs;
        double spits = ms >= hit - 5 && ms < hit + SpitMs ? 1 - Math.Max(0, ms - hit) / SpitMs : 0;
        return new(charge, ms < hit && charge >= 0.3, head, lit, ms >= cock && ms < hit, spits);
    }

    /// <summary>How long a charge takes down the cable standing, and how much a full run cuts off; how much of the cable it lights.</summary>
    public const double PassMs = 1100, PassRun = 550, ChargeLength = 0.12;
    /// <summary>How many charges roll down the cable at once, each a share of a pass behind the last; a pass's chance of none.</summary>
    public const int ChargeLanes = 2;
    public const double Skipped = 0.2;
    /// <summary>How often a charge lays a piece of arc over its stretch, ms, and how many 60 Hz frames one fades over.</summary>
    public const double ChargeEveryMs = 90;
    public const int ChargeLife = 8;

    /// <summary>
    /// Pieces of arc torn off the blade a second as it moves fully, from the share of the cable the blade begins at on; how long
    /// one is, pixels of the world, and how many 60 Hz frames it fades over.
    /// </summary>
    public const double TearsPerSecond = 40, TearFrom = 0.66;
    public static readonly (double From, double To) TearPx = (2.5, 5), TearLife = (9, 16);

    /// <summary>How many passes down the cable <paramref name="elapsedMs"/> makes, the hero running by <paramref name="run"/> (0 to 1).</summary>
    public static double PassesIn(double elapsedMs, double run) => elapsedMs / (PassMs - PassRun * Math.Clamp(run, 0, 1));

    /// <summary>
    /// The charge rolling down the cable in <paramref name="lane"/> once <paramref name="passes"/> have gone by (<see cref="PassesIn"/>,
    /// summed, so a change of pace never jumps it): its head's share of the way, past 1 off the point, and how bright; none while
    /// its pass is skipped. Each pass rolls its own brightness from its number, the same however often it is asked.
    /// </summary>
    public static (double Head, double Alpha)? ChargeAt(int lane, double passes)
    {
        double t = passes + (double)lane / ChargeLanes;
        long pass = (long)Math.Floor(t);
        var roll = new Random(unchecked((int)(pass * 73856093) ^ (lane + 1) * 19349663));
        if (roll.NextDouble() < Skipped) return null;
        double k = t - pass;
        return (k * (1 + ChargeLength), (0.6 + 0.4 * roll.NextDouble()) * Math.Sqrt(Math.Sin(Math.PI * k)));
    }

    /// <summary>
    /// The arc picture's cable, from the coil's tube to the point, in pixels of the spear's picture (hero_spear_arc.png): what the
    /// thrust's arc and the charges run down.
    /// </summary>
    public static readonly (double X, double Y)[] Cable =
        [(28.9, 3.1), (31, 2.6), (32.5, 2.9), (33.3, 3.6), (36.8, 4.15), (44, 4.15), (44.6, 4.5), (51.5, 4.8), (57, 4.6), (63.1, 3.9)];
    /// <summary>The coil's tube, either end, and its box's middle, in pixels of the picture.</summary>
    public static readonly (double X, double Y) TubeFrom = (27.2, 3.2), TubeTo = (30.6, 3.0), Box = (29, 4.7);

    /// <summary>The point <paramref name="share"/> of the way along <see cref="Cable"/> by its length, in pixels of the picture.</summary>
    public static (double X, double Y) Along(double share)
    {
        double total = 0;
        for (int i = 1; i < Cable.Length; i++) total += Length(Cable[i - 1], Cable[i]);
        double left = Math.Clamp(share, 0, 1) * total;
        for (int i = 1; i < Cable.Length; i++)
        {
            double l = Length(Cable[i - 1], Cable[i]);
            if (left <= l || i == Cable.Length - 1)
            {
                double t = l > 0 ? Math.Min(1, left / l) : 0;
                return (Cable[i - 1].X + (Cable[i].X - Cable[i - 1].X) * t, Cable[i - 1].Y + (Cable[i].Y - Cable[i - 1].Y) * t);
            }
            left -= l;
        }
        return Cable[^1];
    }

    /// <summary>The cable from <paramref name="from"/> to <paramref name="to"/> of its length, into <paramref name="into"/>, a point every <paramref name="step"/> pixels.</summary>
    public static void Slice(double from, double to, double step, List<(double X, double Y)> into)
    {
        into.Clear();
        double total = 0;
        for (int i = 1; i < Cable.Length; i++) total += Length(Cable[i - 1], Cable[i]);
        int n = Math.Max(1, (int)Math.Ceiling((to - from) * total / step));
        for (int i = 0; i <= n; i++) into.Add(Along(from + (to - from) * i / n));
    }

    private static double Length((double X, double Y) a, (double X, double Y) b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

    private static double Smooth(double t) => t * t * (3 - 2 * t);
}
