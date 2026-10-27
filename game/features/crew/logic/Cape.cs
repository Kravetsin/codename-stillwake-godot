using System;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A cape (a prototype for cloth that moves): a <see cref="Cloth"/> pinned along the top of the neck and the shoulders,
/// down to the floor, flaring toward the hem. Its outside is drawn over the hero; its inside is the same sheet drawn
/// behind it, its front edge carried forward as the cape wraps round the far side of the body (<see cref="Inside"/>),
/// so the two always meet at the back edge and the hem. It keeps its cut: no point of it comes more than
/// <see cref="Give"/> forward of where it hangs as cut, so walking backward it stays round the back rather than wrapping
/// the hero, and it swings back from that freely; never under the feet. What the hero wears on its back does not shape it. A picture is laid over the grid (<see cref="Uv"/>); a triangle turned
/// over shows the lining (<see cref="Flipped"/>). Drawn only, never simulated: every peer moves its own.
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
    /// <summary>Where the top is pinned, front to back, from the feet: round the top of the neck and over the shoulder.</summary>
    private static readonly (double X, double Y)[] Pins =
        [(1.5, -(Rig.Neck - 1)), (-1.5, -(Rig.Neck - 0.6)), (-4.4, -(Rig.Neck - 1)), (-6.4, -(Rig.Neck - 2.2))];

    private Cloth? _cloth;
    private (double X, double Y)? _shoulders;
    private double _speedX, _speedY, _steps;
    private readonly (double X, double Y)[] _points = new (double, double)[Cols * Rows], _inside = new (double, double)[Cols * Rows];
    private static readonly int[] Top = [0, 1, 2, 3];

    /// <summary>The triangles of its sheet, three points each, turned the same way as the grid at rest.</summary>
    public static readonly int[] Triangles = MakeTriangles();

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
        double hemX = -2.5 - u * 16, hemY = -1.5;
        // Full from the shoulders down, as it trails walking: it stands off the back rather than clinging to it.
        return (top.X + (hemX - top.X) * Math.Pow(v, 1.2), top.Y + (hemY - top.Y) * v);
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
    /// Moves it on by <paramref name="elapsedMs"/>. <paramref name="toWorld"/> and <paramref name="toFrame"/> turn a point
    /// of the figure's frame into the world's pixels and back: it hangs in the world and is pinned in the frame.
    /// </summary>
    public void Step(double elapsedMs, Func<(double X, double Y), (double X, double Y)> toWorld, Func<(double X, double Y), (double X, double Y)> toFrame)
    {
        if (_cloth == null)
        {
            var start = new (double X, double Y)[Cols * Rows];
            for (int i = 0; i < start.Length; i++) start[i] = toWorld(Rest(i));
            _cloth = new Cloth(Style, Cols, Rows, start);
        }
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
        // Against the way the hero moves, as the air it goes through; lifted by a run across, not by a fall.
        double speed = Math.Sqrt(_speedX * _speedX + _speedY * _speedY), push = Push * (speed / Sprint) * (speed / Sprint);
        double across = _speedX / Sprint, flutter = 1 + Flutter * Math.Sin(_steps / FlutterSteps * 2 * Math.PI);
        var wind = speed == 0 ? default : (-_speedX / speed * push, -_speedY / speed * push - Lift * across * across * flutter);
        _cloth.Update(Top, i => toWorld(Pins[i]), elapsedMs, (i, x, y) =>
        {
            var (fx, fy) = toFrame((x, y));
            return toWorld((Math.Min(fx, Rest(i).X + Give), Math.Min(fy, -0.3)));
        }, wind);
        for (int i = 0; i < _points.Length; i++)
        {
            _points[i] = toFrame(_cloth.DrawnAt(i));
            var (u, v) = Uv(i);
            _inside[i] = (_points[i].X + (1 - u) * (WrapAtShoulders + (WrapAtHem - WrapAtShoulders) * v), _points[i].Y);
        }
    }

    /// <summary>Forgets where it hung, so it falls from the shoulders afresh: a respawn, a cape put on.</summary>
    public void Reset() => (_cloth, _shoulders, _speedX, _speedY) = (null, null, 0, 0);

    private static int[] MakeTriangles()
    {
        var t = new int[(Cols - 1) * (Rows - 1) * 6];
        int k = 0;
        for (int r = 0; r + 1 < Rows; r++)
            for (int c = 0; c + 1 < Cols; c++)
            {
                int i = r * Cols + c;
                (t[k], t[k + 1], t[k + 2], t[k + 3], t[k + 4], t[k + 5]) = (i, i + 1, i + Cols, i + 1, i + Cols + 1, i + Cols);
                k += 6;
            }
        return t;
    }

    private static double Turn((double X, double Y) a, (double X, double Y) b, (double X, double Y) c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
}
