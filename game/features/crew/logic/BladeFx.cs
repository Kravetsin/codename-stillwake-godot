using System;
using System.Collections.Generic;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A weapon effect skin at work (skins): it replaces the haze as the drawer of the blade's trail (<see cref="BladeTrail"/>),
/// and lives on the blade at rest, on its lip (<see cref="EdgeLip"/>), all the while it is held. Its loose bits (embers,
/// cinders) are its own, in the world, stepped with it; it draws into three inks, under, dark and over, as the view lays them.
/// </summary>
public abstract class BladeFx
{
    /// <summary>The most loose bits at once; the oldest go first.</summary>
    public const int MostMotes = 360;

    /// <summary>
    /// The blade as held now: <c>At</c> takes a point of its picture into the world; <c>Lip</c> its lip on the picture;
    /// <c>Edge</c> its edge's points in the world from one end to the other (<see cref="HeldLook.EdgePoints"/> of them).
    /// </summary>
    public sealed record Held(Func<(double X, double Y), (double X, double Y)> At, IReadOnlyList<EdgeLip.Point> Lip, IReadOnlyList<(double X, double Y)> Edge);

    /// <summary>A loose bit in the world: what it is, where, its speed and pull (px a second, a second²), drag a 60 Hz frame, age and life (ms), size, phase.</summary>
    protected struct Mote
    {
        public int Kind;
        public double X, Y, Vx, Vy, Ax, Ay, Drag, Age, Life, Size, Phase;
    }

    protected readonly Random Random = new();
    protected readonly List<Mote> Motes = [];
    protected Held? Now { get; private set; }

    /// <summary>The trail its swing is drawn from.</summary>
    public BladeTrail Trail { get; }

    /// <summary>Its clock, ms: what its beats and breaths run on.</summary>
    public double ClockMs { get; private set; }

    /// <summary>The look it was made from.</summary>
    public EffectLook Look { get; }

    protected BladeFx(EffectLook look) => (Look, Trail) = (look, new BladeTrail(look.LifeMs));

    /// <summary>The effect of <paramref name="look"/>.</summary>
    public static BladeFx Of(EffectLook look) => look switch
    {
        FireLook fire => new FireFx(fire),
        RiftLook rift => new RiftFx(rift),
        _ => throw new ArgumentException($"No effect for {look}"),
    };

    /// <summary>Whether anything of it is drawn now: the blade held, a trail or a loose bit left.</summary>
    public bool Live => Now != null || Trail.Live || Motes.Count > 0;

    /// <summary>
    /// <paramref name="elapsedMs"/> on: the blade as <paramref name="held"/>, none while none is; <paramref name="edge"/> what
    /// its trail follows, none while it leaves none (the spear's thrust).
    /// </summary>
    public void Step(double elapsedMs, Held? held, IReadOnlyList<(double X, double Y)>? edge)
    {
        ClockMs += elapsedMs;
        Now = held;
        Trail.Step(elapsedMs, edge);
        double dt = elapsedMs / 1000, frames = elapsedMs / (1000.0 / 60);
        for (int i = Motes.Count - 1; i >= 0; i--)
        {
            var m = Motes[i];
            m.Age += elapsedMs;
            if (m.Age >= m.Life)
            {
                Motes.RemoveAt(i);
                continue;
            }
            (m.Vx, m.Vy) = (m.Vx + m.Ax * dt, m.Vy + m.Ay * dt);
            double drag = Math.Pow(m.Drag, frames);
            (m.Vx, m.Vy) = (m.Vx * drag, m.Vy * drag);
            (m.X, m.Y) = (m.X + m.Vx * dt, m.Y + m.Vy * dt);
            Motes[i] = m;
        }
        if (elapsedMs > 0) Throw(elapsedMs);
        if (Motes.Count > MostMotes) Motes.RemoveRange(0, Motes.Count - MostMotes);
    }

    /// <summary>What it throws this step: loose bits at rest off the lip, and off the stretch just swept.</summary>
    protected virtual void Throw(double elapsedMs)
    {
    }

    /// <summary>
    /// All of it as it is now: <paramref name="under"/> and <paramref name="over"/> are added light, <paramref name="dark"/>
    /// painted between them; points of the world turned into the drawer's own by <paramref name="local"/>.
    /// </summary>
    public abstract void Draw(Ink under, Ink dark, Ink over, Func<(double X, double Y), (double X, double Y)> local);

    /// <summary>The stretch swept by the last step, its edge as it was and as it is and how much of it shows; none if it leaves nothing.</summary>
    protected bool Swept(out (double X, double Y)[] was, out (double X, double Y)[] now, out double shown)
    {
        var s = Trail.Samples;
        (was, now, shown) = (s.Count >= 2 ? s[^2].Edge : [], s.Count >= 1 ? s[^1].Edge : [], s.Count >= 2 ? s[^1].Shown : 0);
        return shown > 0;
    }

    /// <summary>A stretch's mean sweep, px.</summary>
    protected static double SweptPx((double X, double Y)[] was, (double X, double Y)[] now)
    {
        double d = 0;
        for (int i = 0; i < now.Length; i++) d += Distance(was[i], now[i]);
        return d / Math.Max(1, now.Length);
    }

    /// <summary>A point anywhere in a swept stretch, and the edge's speed there, px a second.</summary>
    protected ((double X, double Y) At, (double X, double Y) Speed) InBand((double X, double Y)[] was, (double X, double Y)[] now, double elapsedMs)
    {
        int j = Math.Min(now.Length - 2, (int)(Random.NextDouble() * (now.Length - 1)));
        double a = Random.NextDouble(), b = Random.NextDouble();
        var w = Lerp(was[j], was[j + 1], a);
        var n = Lerp(now[j], now[j + 1], a);
        return (Lerp(w, n, b), ((n.X - w.X) * 1000 / elapsedMs, (n.Y - w.Y) * 1000 / elapsedMs));
    }

    /// <summary>A lip point in the world and the unit way out of the blade there, turned with it.</summary>
    protected static ((double X, double Y) At, (double X, double Y) Out) Outward(Held held, EdgeLip.Point p)
    {
        var at = held.At((p.X, p.Y));
        var o = held.At((p.X + p.Nx * 0.5, p.Y + p.Ny * 0.5));
        return (at, Unit(o.X - at.X, o.Y - at.Y));
    }

    /// <summary>How many of something due at <paramref name="rate"/> this step: its whole part, and one more by chance of the rest.</summary>
    protected int HowMany(double rate) => (int)Math.Floor(rate + Random.NextDouble());

    protected double Between((double From, double To) range) => range.From + (range.To - range.From) * Random.NextDouble();

    /// <summary>A soft round spot <paramref name="radius"/> across from its heart, faded to nothing at its rim.</summary>
    protected static void Soft(Ink ink, (double X, double Y) at, double radius, Ink.Tone tone)
    {
        if (tone.A <= 0.004 || radius <= 0.05) return;
        float was = ink.Feather;
        ink.Feather = (float)radius;
        Disc(ink, at, radius * 0.05, tone);
        ink.Feather = was;
    }

    /// <summary>A round dot of <paramref name="radius"/>, its rim feathered.</summary>
    protected static void Disc(Ink ink, (double X, double Y) at, double radius, Ink.Tone tone)
    {
        if (tone.A <= 0.004) return;
        var shape = new (double X, double Y)[8];
        for (int k = 0; k < 8; k++) shape[k] = (at.X + Math.Cos(k * Math.PI / 4) * radius, at.Y + Math.Sin(k * Math.PI / 4) * radius);
        ink.Fill(shape, at, tone);
    }

    protected static Ink.Tone Tone((int R, int G, int B) c, double a) => Ink.Tone.Of(c.R, c.G, c.B, (float)Math.Clamp(a, 0, 1));

    protected static (int R, int G, int B) Mix((int R, int G, int B) a, (int R, int G, int B) b, double t) =>
        ((int)Math.Round(a.R + (b.R - a.R) * t), (int)Math.Round(a.G + (b.G - a.G) * t), (int)Math.Round(a.B + (b.B - a.B) * t));

    protected static (double X, double Y) Lerp((double X, double Y) a, (double X, double Y) b, double t) => (a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    protected static double Distance((double X, double Y) a, (double X, double Y) b) => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

    protected static (double X, double Y) Unit(double x, double y)
    {
        double l = Math.Sqrt(x * x + y * y);
        return l > 1e-9 ? (x / l, y / l) : (1, 0);
    }

    protected static double Smooth(double t) => t * t * (3 - 2 * t);
}
