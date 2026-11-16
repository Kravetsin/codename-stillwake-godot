using System;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A cape (2026-10-27-the-cape-is-worn), as drawn: a sheet of points pinned along the top of the neck and the shoulders,
/// down to the floor, flaring toward the hem. Its outside is drawn over the hero; its inside is the same sheet drawn
/// behind it, its front edge carried forward as the cape wraps round the far side of the body (<see cref="Inside"/>),
/// so the two always meet at the back edge and the hem. It is posed, not simulated (2026-10-28-the-cape-flows): its cut
/// swung back about the nape (<see cref="Flow"/>) by an angle set by how the hero moves, little standing, far at a sprint
/// whichever way, down rising from a jump and up falling, each row easing toward it the slower the lower, so a change runs
/// down it as a whip, and a breeze always running down it. No point of it comes more than <see cref="Give"/> forward of
/// where it hangs as cut, so it never wraps the hero; never under the feet but afloat. What the hero wears on its back
/// does not shape it. A picture is laid over the grid (<see cref="Uv"/>). Afloat it opens out behind on a slow wave
/// (<see cref="AfloatRest"/>). Its hood, down on the back under a fur collar, swings with the cape's top rows
/// (<see cref="Hood"/>). Drawn only: every peer poses its own.
/// </summary>
public sealed class Cape
{
    /// <summary>Points across, from its front edge at the neck to its back edge, and down, from the shoulders to the hem.</summary>
    public const int Cols = 4, Rows = 9;
    /// <summary>
    /// How far it swings, in radians at the hem (<see cref="Flow"/>): <see cref="Calm"/> standing, up to <see cref="Sweep"/>
    /// at a sprint; <see cref="Sink"/> less rising from a jump at <see cref="Plunge"/> pixels a step, so it trails down, and
    /// <see cref="Soar"/> more falling as fast, so it streams up; never past <see cref="Most"/>, where the sheet would
    /// begin to fold, nor below none.
    /// </summary>
    private const double Calm = 0.1, Sweep = 1.0, Sink = 0.5, Soar = 0.7, Most = 1.6, Plunge = 5;
    /// <summary>
    /// How each row eases toward the swing, as a spring: <see cref="Spring"/> of the gap a step a step at the shoulders,
    /// <see cref="Lag"/> of that less at the hem, <see cref="Damp"/> of its speed kept a step, so it overshoots a little,
    /// the hem last: a change runs down it as a whip. Turned about, every row is thrown back to <see cref="Flung"/> of its
    /// swing, so it swings out behind afresh.
    /// </summary>
    private const double Spring = 0.02, Lag = 0.75, Damp = 0.9, Flung = 0.3;
    /// <summary>
    /// The hero's breeze: a wave runs across it from the shoulders to the hem, <see cref="WaveLag"/> of a turn behind at the
    /// hem, <see cref="Ripple"/> pixels at the hem standing and <see cref="RippleRun"/> at a sprint, once every
    /// <see cref="WaveSteps"/> steps standing and <see cref="RunWaveSteps"/> at a sprint.
    /// </summary>
    private const double Ripple = 0.8, RippleRun = 1.6, WaveSteps = 100, RunWaveSteps = 18, WaveLag = 0.75;
    /// <summary>Afloat it opens out to <see cref="Spread"/> and a sprint's sweep at most, on a slow wave of <see cref="Wave"/> pixels.</summary>
    private const double Spread = 0.7, Wave = 1.2;
    /// <summary>
    /// A sprint's speed, <see cref="Sprint"/> pixels a step: the hero's speed, read from its shoulders and eased over
    /// <see cref="Ease"/> of a step, against it.
    /// </summary>
    private const double Sprint = 211.0 / 60, Ease = 0.2;
    /// <summary>The farthest forward of its cut any point of it goes, in pixels.</summary>
    private const double Give = 1;
    /// <summary>How far its inside's front edge is carried forward of the outside's, at the shoulders and at the hem, hanging: the cape round the far side; less as it swings away, none swung level.</summary>
    private const double WrapAtShoulders = 2, WrapAtHem = 4;
    /// <summary>How fast the wrap goes as it leaves hanging straight down: as the power of how nearly straight down it runs, so at 45° it is all but gone.</summary>
    private const double Tuck = 8;
    /// <summary>Where it swings about, just behind its back pin: every point in front of or below it, so swung far it turns as one and never folds.</summary>
    private static readonly (double X, double Y) Nape = (-10, -(Rig.Neck - 1.5));
    /// <summary>How the swing grows down it, as a power of how far down: under 1 the sheet turns more nearly whole, so swung far its back edge, inside the curl, is not squeezed.</summary>
    private const double Curl = 0.7;
    /// <summary>
    /// Where the top is pinned, front to back, from the feet: clasped at the throat under the chin (<see cref="Clasp"/>), round
    /// the neck and over the shoulder to the back, under its collar.
    /// </summary>
    private static readonly (double X, double Y)[] Pins =
        [(3.5, -(Rig.Neck - 0.6)), (0.5, -(Rig.Neck + 0.2)), (-3.5, -Rig.Neck), (-7, -(Rig.Neck - 1.5))];

    /// <summary>The hood's points across, front to back, and down, from its opening under the collar to its tip.</summary>
    public const int HoodCols = 3, HoodRows = 3;
    /// <summary>
    /// Where the hood hangs as cut, row by row from its opening, pinned under the collar: full at its middle, its tip lowest
    /// at the back.
    /// </summary>
    private static readonly (double X, double Y)[] HoodCut =
    [
        (-1.5, -(Rig.Neck - 1)), (-5, -(Rig.Neck - 0.5)), (-8.5, -(Rig.Neck - 1.5)),
        (-2.5, -(Rig.Neck - 5)), (-6, -(Rig.Neck - 5.5)), (-10.5, -(Rig.Neck - 5)),
        (-4, -(Rig.Neck - 8.5)), (-7.5, -(Rig.Neck - 10.5)), (-10, -(Rig.Neck - 8.5)),
    ];
    /// <summary>The hood's share of the swing and the breeze of the cape's row under it: it lies on the cape, out of most of it.</summary>
    private const double HoodWind = 0.4;
    /// <summary>The cape's row the hood lies on, whose swing it takes.</summary>
    private const int HoodOn = 2;

    /// <summary>Where its clasp is, from the feet: its front pin, at the throat.</summary>
    public static (double X, double Y) Clasp => Pins[0];

    private (double X, double Y)? _shoulders;
    private double _speedX, _speedY, _phase;
    private int _facing;
    private readonly double[] _swing = new double[Rows], _swingSpeed = new double[Rows];
    private readonly (double X, double Y)[] _points = new (double, double)[Cols * Rows], _inside = new (double, double)[Cols * Rows],
        _hoodPoints = new (double, double)[HoodCols * HoodRows];

    public Cape() => Reset();

    /// <summary>The triangles of its sheet, three points each, turned the same way as the grid at rest.</summary>
    public static readonly int[] Triangles = MakeTriangles(Cols, Rows);

    /// <summary>The triangles of its hood, as <see cref="Triangles"/>.</summary>
    public static readonly int[] HoodTriangles = MakeTriangles(HoodCols, HoodRows);

    /// <summary>Its hood's points as last drawn, as <see cref="Points"/>.</summary>
    public ReadOnlySpan<(double X, double Y)> Hood => _hoodPoints;

    /// <summary>Where the hood's point <paramref name="i"/> hangs as cut, in the figure's frame.</summary>
    public static (double X, double Y) HoodRest(int i) => HoodCut[i];

    /// <summary>Where the hood's point <paramref name="i"/> lies on its picture, as <see cref="Uv"/>.</summary>
    public static (double U, double V) HoodUv(int i) => ((double)(i % HoodCols) / (HoodCols - 1), (double)(i / HoodCols) / (HoodRows - 1));

    /// <summary>Its points as last drawn, row by row from the top, in the figure's frame (+x the way it faces, from the feet).</summary>
    public ReadOnlySpan<(double X, double Y)> Points => _points;

    /// <summary>Its inside as last drawn: the same sheet, its front edge carried forward round the far side of the body as far as it hangs.</summary>
    public ReadOnlySpan<(double X, double Y)> Inside => _inside;

    /// <summary>Where point <paramref name="i"/> hangs as cut, in the figure's frame: from its pin down to the hem, flaring back from the shoulders.</summary>
    public static (double X, double Y) Rest(int i)
    {
        int c = i % Cols;
        double u = (double)c / (Cols - 1), v = (double)(i / Cols) / (Rows - 1);
        var top = Pins[c];
        double hemX = -1 - u * 20, hemY = -1.5;
        // Full from the shoulders down, as it trails walking: it stands off the back rather than clinging to it.
        return (top.X + (hemX - top.X) * Math.Pow(v, 1.2), top.Y + (hemY - top.Y) * v);
    }

    /// <summary>
    /// Where point <paramref name="i"/> is posed with the cape swung by <paramref name="swing"/> radians at the hem, in the
    /// figure's frame: its cut turned back and up about its <see cref="Nape"/>, less toward the shoulders, so it curves.
    /// </summary>
    public static (double X, double Y) Flow(int i, double swing) => Swung(Nape, Rest(i), swing * Math.Pow(Uv(i).V, Curl));

    /// <summary>Where point <paramref name="i"/> opens to afloat and still: its <see cref="Flow"/> swung by <see cref="Spread"/>.</summary>
    public static (double X, double Y) AfloatRest(int i) => Flow(i, Spread);

    private static (double X, double Y) Swung((double X, double Y) top, (double X, double Y) point, double angle)
    {
        double dx = point.X - top.X, dy = point.Y - top.Y;
        return (top.X + dx * Math.Cos(angle) - dy * Math.Sin(angle), top.Y + dx * Math.Sin(angle) + dy * Math.Cos(angle));
    }

    /// <summary>Where point <paramref name="i"/> lies on the cape's picture, 0 to 1 across (front edge to back) and down.</summary>
    public static (double U, double V) Uv(int i) => ((double)(i % Cols) / (Cols - 1), (double)(i / Cols) / (Rows - 1));

    /// <summary>Whether triangle <paramref name="t"/> (of <see cref="Triangles"/>) is turned over, its other side toward the eye.</summary>
    public bool Flipped(int t)
    {
        var (a, b, c) = (_points[Triangles[t * 3]], _points[Triangles[t * 3 + 1]], _points[Triangles[t * 3 + 2]]);
        return Turn(a, b, c) * Turn(Rest(Triangles[t * 3]), Rest(Triangles[t * 3 + 1]), Rest(Triangles[t * 3 + 2])) < 0;
    }

    /// <summary>
    /// Poses it on by <paramref name="elapsedMs"/>. <paramref name="toWorld"/> turns a point of the figure's frame into the
    /// world's pixels: the hero's speed is read from how its shoulders move there. <paramref name="afloat"/>, the hero is
    /// weightless, out in space or gliding in fog: it opens out rather than hangs.
    /// </summary>
    public void Step(double elapsedMs, Func<(double X, double Y), (double X, double Y)> toWorld, bool afloat = false)
    {
        // Turned about, it is flung back toward the body and swings out behind afresh.
        int facing = Math.Sign(toWorld((1, 0)).X - toWorld((0, 0)).X);
        if (_facing != 0 && facing != _facing)
            for (int r = 0; r < Rows; r++) (_swing[r], _swingSpeed[r]) = (_swing[r] * Flung, 0);
        _facing = facing;
        // The hero's speed, from how its shoulders moved, in pixels a step.
        var at = toWorld(Pins[0]);
        double steps = elapsedMs / Rope.StepMs;
        if (_shoulders is var (sx, sy) && steps > 0)
        {
            _speedX += ((at.X - sx) / steps - _speedX) * Math.Min(1, Ease * steps);
            _speedY += ((at.Y - sy) / steps - _speedY) * Math.Min(1, Ease * steps);
        }
        _shoulders = at;
        double run = Math.Pow(Math.Min(1, Math.Abs(_speedX) / Sprint), 1.5);
        double rise = afloat ? 0 : Math.Clamp(-_speedY / Plunge, 0, 1), fall = afloat ? 0 : Math.Clamp(_speedY / Plunge, 0, 1);
        double calm = afloat ? Spread : Calm, goal = Math.Clamp(calm + (Math.Max(calm, Sweep) - calm) * run - Sink * rise + Soar * fall, 0, Most);
        for (int r = 1; r < Rows; r++)
        {
            double spring = Spring * (1 - Lag * (r - 1) / (Rows - 2));
            for (double left = steps; left > 0; left -= 1)
            {
                double k = Math.Min(1, left);
                _swingSpeed[r] = (_swingSpeed[r] + (goal - _swing[r]) * spring * k) * Math.Pow(Damp, k);
                _swing[r] = Math.Clamp(_swing[r] + _swingSpeed[r] * k, 0, Most);
            }
        }
        double wave = afloat ? Wave : Ripple + (RippleRun - Ripple) * run;
        _phase += steps / (afloat ? WaveSteps : WaveSteps + (RunWaveSteps - WaveSteps) * run);
        // Afloat there is no floor to keep it off.
        double floor = afloat ? double.MaxValue : -0.3;
        for (int i = 0; i < _points.Length; i++)
        {
            int r = i / Cols;
            double v = Uv(i).V;
            var (x, y) = r == 0 ? Pins[i] : Flow(i, _swing[r]);
            var (dx, dy) = Breeze(v, _swing[r] * Math.Pow(v, Curl), wave);
            _points[i] = (Math.Min(x + dx, Rest(i).X + Give), Math.Min(y + dy, floor));
        }
        for (int i = 0; i < _hoodPoints.Length; i++)
        {
            double v = HoodUv(i).V, swing = _swing[HoodOn] * HoodWind * v;
            var (x, y) = Swung(HoodCut[i % HoodCols], HoodCut[i], swing);
            var (dx, dy) = Breeze(v * HoodWind, swing, wave);
            _hoodPoints[i] = (Math.Min(x + dx, HoodCut[i].X + Give), y + dy);
        }
        for (int i = 0; i < _points.Length; i++)
        {
            var (u, v) = Uv(i);
            // Swung away from the body it no longer wraps round it: its far side lies behind its near side, not past its edge.
            // How far it hangs: how nearly straight down it runs from the row above (the top row, to the row below).
            int below = i < Cols ? i + Cols : i;
            double dx = _points[below].X - _points[below - Cols].X, dy = _points[below].Y - _points[below - Cols].Y, d = Math.Sqrt(dx * dx + dy * dy);
            double hangs = d == 0 ? 1 : Math.Pow(Math.Max(0, dy / d), Tuck);
            _inside[i] = (_points[i].X + (1 - u) * (WrapAtShoulders + (WrapAtHem - WrapAtShoulders) * v) * hangs, _points[i].Y);
        }
    }

    /// <summary>Where the breeze carries a point <paramref name="v"/> of the way down, across a sheet turned by <paramref name="turn"/>.</summary>
    private (double X, double Y) Breeze(double v, double turn, double wave)
    {
        double across = Math.Sin((_phase - WaveLag * v) * 2 * Math.PI) * wave * v;
        return (across * Math.Cos(turn), across * Math.Sin(turn));
    }

    /// <summary>Forgets how it moved, so it hangs from the shoulders afresh: a respawn, a cape put on.</summary>
    public void Reset()
    {
        (_shoulders, _speedX, _speedY, _facing) = (null, 0, 0, 0);
        Array.Fill(_swing, Calm);
        Array.Clear(_swingSpeed);
    }

    private static int[] MakeTriangles(int cols, int rows)
    {
        var t = new int[(cols - 1) * (rows - 1) * 6];
        int k = 0;
        for (int r = 0; r + 1 < rows; r++)
            for (int c = 0; c + 1 < cols; c++)
            {
                int i = r * cols + c;
                (t[k], t[k + 1], t[k + 2], t[k + 3], t[k + 4], t[k + 5]) = (i, i + 1, i + cols, i + 1, i + cols + 1, i + cols);
                k += 6;
            }
        return t;
    }

    private static double Turn((double X, double Y) a, (double X, double Y) b, (double X, double Y) c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
}
