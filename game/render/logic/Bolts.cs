using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>Where a bolt runs now, in pixels of the world, and how far its first fold bows out, a share of its length.</summary>
public delegate (double X0, double Y0, double X1, double Y1, double Bulge) BoltEnds();

/// <summary>
/// An arc's colours (arcs-are-hairlines): its white core, the haze either side of it, the light it gives, and the deeper colour
/// its last shapes fade in behind it.
/// </summary>
public sealed record BoltTint((int R, int G, int B) Core, (int R, int G, int B) Rim, (int R, int G, int B) Glow, (int R, int G, int B) Ghost)
{
    /// <summary>Every arc's till the arc module: a white core in a cyan haze, its afterimage a deep blue (0075).</summary>
    public static readonly BoltTint Cyan = new((244, 254, 255), (85, 214, 255), (70, 180, 255), (60, 120, 255));

    /// <summary>The arc module's (0086): a white core in a violet haze, a violet-white light, its afterimage indigo.</summary>
    public static readonly BoltTint Violet = new((252, 246, 255), (178, 132, 255), (170, 130, 255), (110, 70, 230));

    /// <summary>A tick winding up (the sentry page's): red arcs jumping between its legs, a white-pink core, a red light.</summary>
    public static readonly BoltTint Red = new((255, 225, 219), (255, 120, 100), (255, 90, 70), (190, 40, 40));
}

/// <summary>
/// One electric arc (0075): a crooked line from end to end, its shape held for <see cref="Bolts.Hold"/> 60 Hz frames and
/// rolled anew, alive <see cref="Life"/> of them. Its shape is kept along and across the line between its ends, a share
/// of its length, so a held arc stays on ends that move: the drill swinging, a lamp's socket.
/// </summary>
public sealed class Bolt(BoltEnds ends, int life, double intensity, double width, bool branchy, bool fine = false, BoltTint? tint = null)
{
    public BoltEnds Ends { get; } = ends;
    /// <summary>Its life and age, in 60 Hz frames.</summary>
    public int Life { get; } = life;
    public int Age { get; internal set; }
    /// <summary>How bright it is, 1 a full arc's: its strokes' opacity, its glow's and its light's.</summary>
    public double Intensity { get; } = intensity;
    /// <summary>How thick, 1 an arc's whole width (<see cref="Bolts.Haze"/>, <see cref="Bolts.Core"/>, in pixels of the screen).</summary>
    public double Width { get; } = width;
    public bool Branchy { get; } = branchy;
    /// <summary>
    /// A fine arc (the arc drill's coil, slice 3): a hair of white-blue light at the screen's finest under a faint blue haze,
    /// its shape rolled anew every frame, no glow along it and no light of its own, for what throws it lights the room itself.
    /// </summary>
    public bool Fine { get; } = fine;
    /// <summary>Its colours: the cyan of every arc but the arc module's violet (0086).</summary>
    public BoltTint Tint { get; } = tint ?? BoltTint.Cyan;
    /// <summary>Its line, then each branch's, as (along, across) shares of its length; <see cref="Runs"/> says where each starts.</summary>
    internal readonly List<(double U, double V)> Shape = [];
    internal readonly List<int> Runs = [];

    /// <summary>Its line's last shapes, newest first, at most <see cref="Bolts.Ghosts"/>: its afterimage, fading behind it.</summary>
    internal readonly List<(double U, double V)[]> Past = [];

    /// <summary>How many lines it is drawn as: its own, then its branches.</summary>
    public int Lines => Runs.Count;

    /// <summary>How many of its last shapes it still shows behind it.</summary>
    public int Ghosts => Past.Count;

    /// <summary>The points of its shape <paramref name="back"/> rolls ago (1 the last), its own line alone, between the ends given.</summary>
    public int GhostOf(int back, double x0, double y0, double x1, double y1, Span<(double X, double Y)> into)
    {
        var shape = Past[back - 1];
        double dx = x1 - x0, dy = y1 - y0;
        int n = Math.Min(shape.Length, into.Length);
        for (int i = 0; i < n; i++)
        {
            var (u, v) = shape[i];
            into[i] = (x0 + dx * u - dy * v, y0 + dy * u + dx * v);
        }
        return n;
    }

    /// <summary>The points of line <paramref name="line"/> (0 its own, then its branches) between ends (<paramref name="x0"/>, <paramref name="y0"/>) and (<paramref name="x1"/>, <paramref name="y1"/>).</summary>
    public int PointsOf(int line, double x0, double y0, double x1, double y1, Span<(double X, double Y)> into)
    {
        int from = Runs[line], to = line + 1 < Runs.Count ? Runs[line + 1] : Shape.Count, n = 0;
        double dx = x1 - x0, dy = y1 - y0;
        for (int i = from; i < to && n < into.Length; i++)
        {
            var (u, v) = Shape[i];
            into[n++] = (x0 + dx * u - dy * v, y0 + dy * u + dx * v);
        }
        return n;
    }
}

/// <summary>
/// The electric arcs in the world (0075, slice 2; arcs-are-hairlines): a crooked line by midpoint displacement, now and then
/// branching, each shape held two 60 Hz frames and rolled anew, drawn as a hair of white light in a thin haze at the screen's
/// pixels whatever the zoom, its last shapes fading behind it, and a light while it lives; or fine (<see cref="Bolt.Fine"/>).
/// A lamp short of power catching, an electric smelter's contacts, the arc drill's coil (fine), the relic, the sentry make
/// them; the effects layer draws them, the light feature lights them.
/// Drawn only, each peer its own. <paramref name="random"/> draws from 0 to 1; tests pass their own.
/// </summary>
public sealed class Bolts(Func<double>? random = null)
{
    /// <summary>At most this many at once.</summary>
    public const int Limit = 48;
    /// <summary>How many 60 Hz frames a shape is held before it is rolled anew.</summary>
    public const int Hold = 2;
    /// <summary>
    /// A whole arc's widths, its haze's and its core's, in pixels of the screen at any zoom (arcs-are-hairlines); how far its
    /// folds stray, a share of each.
    /// </summary>
    public const double Haze = 4, Core = 1.2, Stray = 0.35;
    /// <summary>How many of its last shapes an arc shows behind it, each fainter: two 60 Hz frames each, a tenth of a second.</summary>
    public const int Ghosts = 4;
    /// <summary>The chance a fork grows at a point of a branchy arc, before the share each point gets.</summary>
    private const double Branching = 0.35;
    /// <summary>A fine arc's core and the haze either side of it (<see cref="Bolt.Fine"/>).</summary>
    public static readonly (int R, int G, int B) FineCore = (222, 244, 255), FineHaze = (90, 170, 255);

    private readonly Func<double> _random = random ?? Random.Shared.NextDouble;
    private readonly List<Bolt> _live = [];
    private double _owedMs;

    public IReadOnlyList<Bolt> Live => _live;

    /// <summary>A new arc along <paramref name="ends"/> for <paramref name="life"/> 60 Hz frames, its shape rolled now; null past the limit.</summary>
    public Bolt? Add(BoltEnds ends, int life, double intensity = 1, double width = 1, bool branchy = false, bool fine = false, BoltTint? tint = null)
    {
        if (_live.Count >= Limit) return null;
        var bolt = new Bolt(ends, Math.Max(1, life), intensity, width, branchy, fine, tint);
        Roll(bolt);
        _live.Add(bolt);
        return bolt;
    }

    /// <summary>Ages every arc by <paramref name="elapsedMs"/> in whole 60 Hz frames, rolling a held shape anew and letting go of those past their life.</summary>
    public void Step(double elapsedMs)
    {
        // A stall moves on a few frames, never a stall's worth of rolls.
        _owedMs += elapsedMs;
        int frames = Math.Min(6, (int)Math.Floor(_owedMs / Glim.SampleMs + 1e-9));
        _owedMs = frames == 6 ? 0 : _owedMs - frames * Glim.SampleMs;
        for (; frames > 0; frames--)
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var b = _live[i];
                b.Age++;
                if (b.Age >= b.Life)
                {
                    _live[i] = _live[^1];
                    _live.RemoveAt(_live.Count - 1);
                }
                else if (b.Fine || b.Age % Hold == 0) Roll(b);
            }
    }

    public void Clear() => _live.Clear();

    private double Between(double from, double to) => from + (to - from) * _random();

    /// <summary>A new shape for <paramref name="b"/>: its line folded more the longer it is, and, branchy, a few forks.</summary>
    private void Roll(Bolt b)
    {
        var (x0, y0, x1, y1, bulge) = b.Ends();
        double length = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
        if (b.Shape.Count > 0)
        {
            b.Past.Insert(0, b.Shape.GetRange(0, b.Runs.Count > 1 ? b.Runs[1] : b.Shape.Count).ToArray());
            if (b.Past.Count > Ghosts) b.Past.RemoveAt(Ghosts);
        }
        b.Shape.Clear();
        b.Runs.Clear();
        b.Runs.Add(0);
        Fold(b.Shape, (0, 0), (1, 0), length, bulge, b.Fine);
        if (!b.Branchy || length <= 5) return;
        int last = b.Shape.Count - 1;
        for (int j = 1; j < last && b.Runs.Count < 4; j++)
        {
            if (_random() >= Branching * 0.16) continue;
            var at = b.Shape[j];
            double turn = (_random() < 0.5 ? -1 : 1) * Between(0.35, 1), reach = Between(0.22, 0.57);
            b.Runs.Add(b.Shape.Count);
            Fold(b.Shape, at, (at.U + Math.Cos(turn) * reach, at.V + Math.Sin(turn) * reach), length * reach, 0, false);
        }
    }

    /// <summary>
    /// The line from <paramref name="a"/> to <paramref name="b"/> (shares of the arc's length) folded by midpoint displacement into
    /// <paramref name="into"/>: each fold a midpoint pushed aside up to <see cref="Stray"/> of its segment, the first by
    /// <paramref name="bulge"/> more; one fold for each doubling of its <paramref name="length"/> in pixels past 1.6, six at most.
    /// A <paramref name="fine"/> arc is folded twice at least, four segments however short, and strays more than twice as far: jagged.
    /// </summary>
    private void Fold(List<(double U, double V)> into, (double U, double V) a, (double U, double V) b, double length, double bulge, bool fine)
    {
        int levels = Math.Clamp((int)Math.Ceiling(Math.Log2(Math.Max(length, 2) / 1.6)), fine ? 2 : 1, 6);
        Span<(double U, double V)> points = stackalloc (double, double)[65];
        Span<(double U, double V)> next = stackalloc (double, double)[65];
        points[0] = a;
        points[1] = b;
        int n = 2;
        double span = Math.Sqrt((b.U - a.U) * (b.U - a.U) + (b.V - a.V) * (b.V - a.V));
        for (int level = 0; level < levels; level++)
        {
            int m = 0;
            next[m++] = points[0];
            for (int i = 0; i + 1 < n; i++)
            {
                var (p, q) = (points[i], points[i + 1]);
                double du = q.U - p.U, dv = q.V - p.V, segment = Math.Sqrt(du * du + dv * dv);
                if (segment < 1e-9) segment = 1e-9;
                double off = (_random() * 2 - 1) * Stray * segment * (fine ? 1.4 : 0.55) + (level == 0 ? bulge * span : 0);
                next[m++] = ((p.U + q.U) / 2 - dv / segment * off, (p.V + q.V) / 2 + du / segment * off);
                next[m++] = q;
            }
            next[..m].CopyTo(points);
            n = m;
        }
        for (int i = 0; i < n; i++) into.Add(points[i]);
    }
}
