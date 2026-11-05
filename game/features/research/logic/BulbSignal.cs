using System;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The bulb over this player's hero ([bulb-signal](../../../../docs/decisions/2026-11-02-bulb-signal.md)): when a note starts waiting on the board it is drawn on,
/// heats up, overheats, bursts and is gone. There is one at a time: a find while it burns makes it burn on, and a find after it burst adds nothing, for the
/// key's lamp is lit. Times are seconds since the first find.
/// </summary>
public sealed class BulbSignal
{
    /// <summary>How long the line takes to draw on, and when the filament is whole.</summary>
    public const double DrawnBy = 0.22, HotBy = 0.55;

    /// <summary>When it bursts with one find; how long it still burns after a later one.</summary>
    public const double Burns = 1.45, BurnsOn = 0.8;

    /// <summary>How long before the burst it stutters, and how fast.</summary>
    private const double Stutters = 0.22, StutterRate = 36;

    /// <summary>How long a later find's dip lasts.</summary>
    private const double Dips = 0.12;

    /// <summary>The share of heat in the dip and in the stutter.</summary>
    private const double DipHeat = 0.35, StutterHeat = 0.55;

    /// <summary>The flash of the burst, and how long after it the cap has gone.</summary>
    public const double Flash = 0.05, Gone = 0.95;

    /// <summary>How long before the cap has gone it starts to fade, and how long the smoke rises and fades.</summary>
    private const double CapFades = 0.3, SmokeFrom = 0.08, SmokeFadesFrom = 0.36, SmokeUntil = 0.62;

    private const int ShardCount = 11, SparkCount = 4;
    private const double Fall = 170, SparkSpeed = 26;

    /// <summary>How long the find that hatches at the burst takes to fly down into the research key.</summary>
    public const double FlightS = 0.9;

    private object? _seen;
    private int _unread;

    /// <summary>Seconds since the first find while it plays; null while it does not.</summary>
    public double? Time { get; private set; }

    /// <summary>When the last find while it burned came; null with none.</summary>
    public double? Later { get; private set; }

    /// <summary>How many finds the one that hatches carries: every one that came before the burst.</summary>
    public int Carried { get; private set; }

    /// <summary>When it bursts.</summary>
    public double BurstsAt => Later is { } later ? Math.Max(Burns, later + BurnsOn) : Burns;

    /// <summary>How far the find is on its way down, 0 at the burst to 1 on the key; null before the burst and once it has landed.</summary>
    public double? Flight => Time is { } t && t >= BurstsAt && t < BurstsAt + FlightS ? (t - BurstsAt) / FlightS : null;

    /// <summary>The finds not yet on the key: carried until the one that hatched lands. The key's number is the unread less these.</summary>
    public int Flying => Time is { } t && t < BurstsAt + FlightS ? Carried : 0;

    /// <summary>A read of what is new on the board: <paramref name="seen"/> is whose, so another world's starts over with nothing new.</summary>
    public void Read(object seen, int unread, bool open)
    {
        int more = seen == _seen ? unread - _unread : 0;
        (_seen, _unread) = (seen, unread);
        if (open) Out();
        else if (more > 0) Find(more);
    }

    private void Find(int more)
    {
        if (Time is not { } time) (Time, Later, Carried) = (0, null, more);
        else if (time < BurstsAt) (Later, Carried) = (time, Carried + more);
    }

    private void Out() => (Time, Later, Carried) = (null, null, 0);

    /// <summary>Moves it <paramref name="seconds"/> on.</summary>
    public void Step(double seconds)
    {
        if (Time is not { } time) return;
        time += seconds;
        if (time >= BurstsAt + Math.Max(Gone, FlightS)) Out();
        else Time = time;
    }

    /// <summary>How hot the filament is at <paramref name="t"/>, 0 to 1; 0 once it has burst.</summary>
    public double Heat(double t)
    {
        double burst = BurstsAt;
        if (t >= burst) return 0;
        if (t > burst - Stutters && (int)Math.Floor(t * StutterRate) % 2 != 0) return StutterHeat;
        if (t < DrawnBy) return 0;
        if (t < HotBy) return (t - DrawnBy) / (HotBy - DrawnBy);
        return Later is { } later && t >= later && t - later < Dips ? DipHeat : 1;
    }

    /// <summary>Which picture of the filament shows at <paramref name="heat"/>, in hard steps; null while it is dark.</summary>
    public static int? FireOf(double heat) => heat <= 0.02 ? null : heat < 0.2 ? 0 : heat < 0.45 ? 1 : heat < 0.7 ? 2 : heat < 0.95 ? 3 : 4;

    /// <summary>Which picture of the line being drawn shows at <paramref name="t"/>; null once it is whole.</summary>
    public static int? DrawingOf(double t) => t >= DrawnBy ? null : (int)(t / DrawnBy * 3);

    /// <summary>How much of the cap shows, <paramref name="age"/> seconds after the burst.</summary>
    public static double CapAlpha(double age) => Steps(age, Gone - CapFades, Gone);

    /// <summary>How much of the smoke shows, <paramref name="age"/> seconds after the burst; 0 outside its time.</summary>
    public static double SmokeAlpha(double age) => age <= SmokeFrom ? 0 : Steps(age, SmokeFadesFrom, SmokeUntil);

    /// <summary>A point of the smoke, along it by <paramref name="along"/>, in the world's pixels from the cap.</summary>
    public static (double X, double Y) Smoke(double along, double age) => (Math.Sin(along * 6 + age * 4) * 0.75, -along * (14 + age * 56) / 4);

    /// <summary>
    /// Shard <paramref name="i"/>, <paramref name="age"/> seconds after the burst, in the world's pixels from where it burst, no further down than
    /// <paramref name="floor"/>: null once it has gone. Afloat it drifts straight on.
    /// </summary>
    public static (double X, double Y, double Turn, double Size, double Alpha, bool Bright)? Shard(int i, double age, bool afloat, double floor)
    {
        double a = Noise(70 + i * 3.3), b = Noise(70 + i * 7.9), c = Noise(70 + i * 11.7), d = Noise(70 + i * 5.1), life = 0.45 + 0.42 * c;
        if (age >= life) return null;
        double way = a * Math.Tau, speed = 16 + 40 * b, y = (Math.Sin(way) * speed - 10) * age + 0.5 * (afloat ? 0 : Fall) * age * age;
        return (Math.Cos(way) * speed * age, afloat ? y : Math.Min(y, floor), way + (d - 0.5) * 24 * age, 0.7 + 0.8 * d, Steps(age, life * 0.6, life), i % 3 == 0);
    }

    /// <summary>How many shards fly.</summary>
    public static int Shards => ShardCount;

    /// <summary>
    /// Spark <paramref name="i"/>, <paramref name="age"/> seconds after the burst, in the world's pixels from where it burst, with its way this moment and how far it
    /// has cooled, 0 to 1: null once it has gone.
    /// </summary>
    public static (double X, double Y, double Dx, double Dy, double Cooled)? Spark(int i, double age, bool afloat)
    {
        double a = Noise(41 + i * 3.3), b = Noise(41 + i * 7.9), c = Noise(41 + i * 11.7), life = 0.4 + 0.55 * c;
        if (age >= life) return null;
        double vx = (a - 0.5) * 2 * SparkSpeed, vy = -(0.15 + b * 0.85) * SparkSpeed, fall = afloat ? 0 : Fall;
        return (vx * age, vy * age + 0.5 * fall * age * age, vx, vy + fall * age, age / life);
    }

    /// <summary>How many sparks fly.</summary>
    public static int Sparks => SparkCount;

    /// <summary>Goes out in three hard steps between two times: 1 before, 0 after.</summary>
    private static double Steps(double t, double from, double to) => t < from ? 1 : t >= to ? 0 : (int)((t - from) / (to - from) * 3) switch { 0 => 1, 1 => 0.62, _ => 0.32 };

    private static double Noise(double i)
    {
        double s = Math.Sin(i * 127.1 + 311.7) * 43758.5453;
        return s - Math.Floor(s);
    }
}
