using System;
using System.Linq;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A cape (2026-10-27-the-cape-is-worn), as drawn: a <see cref="Cloth"/> pinned along the top of the neck and the shoulders,
/// down to the floor, flaring toward the hem. Its outside is drawn over the hero; its inside is the same sheet drawn
/// behind it, its front edge carried forward as the cape wraps round the far side of the body (<see cref="Inside"/>),
/// so the two always meet at the back edge and the hem. It keeps its cut: no point of it comes more than
/// <see cref="Give"/> forward of where it hangs as cut, so walking backward it stays round the back rather than wrapping
/// the hero, and it swings back from that freely; never under the feet but afloat. What the hero wears on its back does not shape it. A picture is laid over the grid (<see cref="Uv"/>); a triangle turned
/// over shows the lining (<see cref="Flipped"/>). Afloat it drifts: no gravity, no air, its cut opening out behind as a slow
/// wave runs down it (<see cref="AfloatRest"/>). Its hood, down on the back under a fur collar, is a small cloth of its
/// own (<see cref="Hood"/>), so it bobs and swings as the cape does. Drawn only, never simulated: every peer moves its own.
/// </summary>
public sealed class Cape
{
    /// <summary>Points across, from its front edge at the neck to its back edge, and down, from the shoulders to the hem.</summary>
    public const int Cols = 4, Rows = 9;
    /// <summary>
    /// How it moves a step: the air drags it back a twentieth of its speed a step against a light pull down, so on a run
    /// it trails about 30° and lifts on a jump; its diagonals hold only a little, so it swings back from the shoulders;
    /// its threads give a third when squeezed, so it bunches and straightens again.
    /// </summary>
    private static readonly ClothStyle Style = new(Keep: 0.95, Gravity: 0.3, Shear: 0.08, Squash: 0.35, Passes: 10);
    /// <summary>
    /// How it moves afloat: no gravity and no air, so it keeps nearly all its speed, trailing a push off and carrying on
    /// past a stop; only the wool's own stiffness calms it.
    /// </summary>
    private static readonly ClothStyle AfloatStyle = Style with { Keep = 0.985, Gravity = 0 };
    /// <summary>
    /// Afloat, the wool's memory of its cut opens it out behind the hero: each point is drawn toward its cut swung back
    /// from its pin by up to <see cref="Spread"/> radians at the hem (<see cref="AfloatRest"/>), <see cref="Hold"/> of the
    /// way a step a step, so it settles in about a second and a half; a slow wave runs down it every
    /// <see cref="WaveSteps"/> steps, <see cref="Wave"/> pixels a step a step at the hem, <see cref="WaveLag"/> of a turn
    /// behind at the hem than at the shoulders, so it ripples as if in water and never quite comes to rest.
    /// </summary>
    private const double Spread = 0.7, Hold = 0.004, Wave = 0.012, WaveSteps = 100, WaveLag = 0.75;
    /// <summary>
    /// The air on it beyond the plain drag, growing with the square of the hero's speed, so a run lifts it far more than a
    /// walk: pushed against the way it goes <see cref="Push"/> and lifted by a run across <see cref="Lift"/> pixels a step a step at a sprint's speed
    /// (<see cref="Sprint"/> pixels a step), the lift fluttering by <see cref="Flutter"/> of itself every
    /// <see cref="FlutterSteps"/> steps; the hero's speed read from its shoulders, eased over <see cref="Ease"/> of a step.
    /// </summary>
    private const double Push = 0.1, Lift = 0.055, Sprint = 211.0 / 60, Flutter = 0.5, FlutterSteps = 9, Ease = 0.2;
    /// <summary>The farthest forward of its cut any point of it goes, in pixels.</summary>
    private const double Give = 1;
    /// <summary>How far its inside's front edge is carried forward of the outside's, at the shoulders and at the hem: the cape round the far side.</summary>
    private const double WrapAtShoulders = 2, WrapAtHem = 4;
    /// <summary>
    /// Where the top is pinned, front to back, from the feet: clasped at the throat under the chin (<see cref="Clasp"/>), round
    /// the neck and over the shoulder to the back, under its collar.
    /// </summary>
    private static readonly (double X, double Y)[] Pins =
        [(3.5, -(Rig.Neck - 0.6)), (0.5, -(Rig.Neck + 0.2)), (-3.5, -Rig.Neck), (-7, -(Rig.Neck - 1.5))];

    /// <summary>The hood's points across, front to back, and down, from its opening under the collar to its tip.</summary>
    public const int HoodCols = 3, HoodRows = 3;
    /// <summary>The hood, down on the back: a bag of the same wool, its diagonals firmer than the cape's so it keeps its fullness, its threads down giving
    /// almost freely when squeezed, so it tosses up as the hero stops rising and drops back.</summary>
    private static readonly ClothStyle HoodStyle = new(Keep: 0.9, Gravity: 0.3, Shear: 0.4, Squash: 0.1, Passes: 10);
    /// <summary>The hood afloat: no gravity, more of its speed kept, as the cape's.</summary>
    private static readonly ClothStyle HoodAfloatStyle = HoodStyle with { Keep = 0.97, Gravity = 0 };
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
    /// <summary>The hood's share of the air on the cape: it lies on the cape, out of most of it.</summary>
    private const double HoodWind = 0.4;

    /// <summary>Where its clasp is, from the feet: its front pin, at the throat.</summary>
    public static (double X, double Y) Clasp => Pins[0];

    private Cloth? _cloth, _hood;
    private (double X, double Y)? _shoulders;
    private double _speedX, _speedY, _steps;
    private int _facing;
    private readonly (double X, double Y)[] _points = new (double, double)[Cols * Rows], _inside = new (double, double)[Cols * Rows],
        _hoodPoints = new (double, double)[HoodCols * HoodRows];
    private static readonly int[] Top = [0, 1, 2, 3], HoodTop = [0, 1, 2];

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

    /// <summary>Its inside as last drawn: the same sheet, its front edge carried forward round the far side of the body.</summary>
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

    /// <summary>Where point <paramref name="i"/> drifts to afloat, in the figure's frame: its cut swung back and up from its pin, the more the lower.</summary>
    public static (double X, double Y) AfloatRest(int i)
    {
        var (top, (x, y)) = (Pins[i % Cols], Rest(i));
        double angle = Spread * Uv(i).V, dx = x - top.X, dy = y - top.Y;
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

    /// <summary>Whether it is thrown up over the shoulders: turned over in its top row, under the collar, as a fall throws it.</summary>
    public bool TurnedUp
    {
        get
        {
            for (int t = 0; t < (Cols - 1) * 2; t++)
                if (Flipped(t)) return true;
            return false;
        }
    }

    /// <summary>
    /// Moves it on by <paramref name="elapsedMs"/>. <paramref name="toWorld"/> and <paramref name="toFrame"/> turn a point
    /// of the figure's frame into the world's pixels and back: it hangs in the world and is pinned in the frame.
    /// <paramref name="afloat"/>, the hero is weightless, out in space or gliding in fog: it drifts rather than hangs.
    /// </summary>
    public void Step(double elapsedMs, Func<(double X, double Y), (double X, double Y)> toWorld, Func<(double X, double Y), (double X, double Y)> toFrame, bool afloat = false)
    {
        if (_cloth == null)
        {
            var start = new (double X, double Y)[Cols * Rows];
            for (int i = 0; i < start.Length; i++) start[i] = toWorld(Rest(i));
            _cloth = new Cloth(Style, Cols, Rows, start);
            _hood = new Cloth(HoodStyle, HoodCols, HoodRows, [.. HoodCut.Select(toWorld)]);
        }
        // Turned about, the cape turns with the hero: left where it was, it would fold through the body and catch.
        int facing = Math.Sign(toWorld((1, 0)).X - toWorld((0, 0)).X);
        if (_facing != 0 && facing != _facing)
            foreach (var cloth in new[] { _cloth, _hood! })
                cloth.Carry(p => { var (x, y) = toFrame(p); return toWorld((-x, y)); });
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
        _steps += steps;
        (_cloth.Style, _hood!.Style) = afloat ? (AfloatStyle, HoodAfloatStyle) : (Style, HoodStyle);
        Func<int, double, double, (double X, double Y)> cape, hood;
        if (afloat)
        {
            // In the frame, turned into the world's: what moves a point of the frame by (x, y).
            var origin = toWorld((0, 0));
            (double X, double Y) World((double X, double Y) f) { var w = toWorld(f); return (w.X - origin.X, w.Y - origin.Y); }
            (double X, double Y) Drift((double X, double Y) cut, double v, double x, double y)
            {
                var (fx, fy) = toFrame((x, y));
                double wave = Math.Sin((_steps / WaveSteps - WaveLag * v) * 2 * Math.PI) * Wave * v;
                return World(((cut.X - fx) * Hold + wave, (cut.Y - fy) * Hold + wave / 2));
            }
            cape = (i, x, y) => Drift(AfloatRest(i), Uv(i).V, x, y);
            hood = (i, x, y) => Drift(HoodCut[i], HoodUv(i).V * HoodWind, x, y);
        }
        else
        {
            // Against the way the hero moves, as the air it goes through; lifted by a run across, not by a fall.
            double speed = Math.Sqrt(_speedX * _speedX + _speedY * _speedY), push = Push * (speed / Sprint) * (speed / Sprint);
            double across = _speedX / Sprint, flutter = 1 + Flutter * Math.Sin(_steps / FlutterSteps * 2 * Math.PI);
            (double X, double Y) wind = speed == 0 ? default : (-_speedX / speed * push, -_speedY / speed * push - Lift * across * across * flutter);
            cape = (_, _, _) => wind;
            hood = (_, _, _) => (wind.X * HoodWind, wind.Y * HoodWind);
        }
        // Afloat there is no floor to keep it off.
        double floor = afloat ? double.MaxValue : -0.3;
        _cloth.Update(Top, i => toWorld(Pins[i]), elapsedMs, (i, x, y) =>
        {
            var (fx, fy) = toFrame((x, y));
            return toWorld((Math.Min(fx, Rest(i).X + Give), Math.Min(fy, floor)));
        }, cape);
        _hood.Update(HoodTop, i => toWorld(HoodCut[i]), elapsedMs, (i, x, y) =>
        {
            var (fx, fy) = toFrame((x, y));
            return toWorld((Math.Min(fx, HoodCut[i].X + Give), fy));
        }, hood);
        for (int i = 0; i < _hoodPoints.Length; i++) _hoodPoints[i] = toFrame(_hood.DrawnAt(i));
        for (int i = 0; i < _points.Length; i++)
        {
            _points[i] = toFrame(_cloth.DrawnAt(i));
            var (u, v) = Uv(i);
            _inside[i] = (_points[i].X + (1 - u) * (WrapAtShoulders + (WrapAtHem - WrapAtShoulders) * v), _points[i].Y);
        }
    }

    /// <summary>Forgets where it hung, so it falls from the shoulders afresh: a respawn, a cape put on.</summary>
    public void Reset() => (_cloth, _hood, _shoulders, _speedX, _speedY, _facing) = (null, null, null, 0, 0, 0);

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
