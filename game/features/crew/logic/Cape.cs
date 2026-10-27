using System;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A cape (a prototype for cloth that moves): a <see cref="Cloth"/> hung across the shoulders, from the neck to the
/// back of the air pack, down to the floor, flaring toward the hem, so it covers the back half of the body and the
/// legs. Drawn as one sheet with a picture laid over its grid (<see cref="Uv"/>); a triangle turned over shows the
/// lining (<see cref="Flipped"/>). It never swings in front of the body's middle nor under the feet. Drawn only, never
/// simulated: every peer moves its own.
/// </summary>
public sealed class Cape
{
    /// <summary>Points across, from its front edge at the neck to its back edge, and down, from the shoulders to the hem.</summary>
    public const int Cols = 4, Rows = 9;
    /// <summary>
    /// How it moves a step: the air drags it back a twentieth of its speed a step against a light pull down, so on a run
    /// it trails about 30° and lifts on a jump; its diagonals hold it only a little, so it swings back from the shoulders.
    /// </summary>
    private static readonly ClothStyle Style = new(Keep: 0.95, Gravity: 0.3, Shear: 0.08, Passes: 10);
    /// <summary>The farthest forward it goes, from the middle, below the shoulders: it hangs round the back, not through the chest.</summary>
    private const double Front = 1;

    private Cloth? _cloth;
    private readonly (double X, double Y)[] _points = new (double, double)[Cols * Rows];
    private static readonly int[] Top = [0, 1, 2, 3];

    /// <summary>The triangles of its sheet, three points each, turned the same way as the grid at rest.</summary>
    public static readonly int[] Triangles = MakeTriangles();

    /// <summary>Its points as last drawn, row by row from the top, in the figure's frame (+x the way it faces, from the feet).</summary>
    public ReadOnlySpan<(double X, double Y)> Points => _points;

    /// <summary>Where point <paramref name="i"/> hangs at rest, in the figure's frame: across the shoulders, sloping back over them, flaring down to the floor.</summary>
    public static (double X, double Y) Rest(int i)
    {
        double u = (double)(i % Cols) / (Cols - 1), v = (double)(i / Cols) / (Rows - 1);
        double top = -(Rig.Neck - 1.5) + u * 2, hem = -1.5;
        double topX = 0.5 - u * 11, hemX = -1.5 - u * 13.5;
        return (topX + (hemX - topX) * v, top + (hem - top) * v);
    }

    /// <summary>Where point <paramref name="i"/> lies on the cape's picture, 0 to 1 across (front edge to back) and down.</summary>
    public static (double U, double V) Uv(int i) => ((double)(i % Cols) / (Cols - 1), (double)(i / Cols) / (Rows - 1));

    /// <summary>Whether triangle <paramref name="t"/> (of <see cref="Triangles"/>) is turned over, its lining toward the eye.</summary>
    public bool Flipped(int t)
    {
        var (a, b, c) = (_points[Triangles[t * 3]], _points[Triangles[t * 3 + 1]], _points[Triangles[t * 3 + 2]]);
        return Turn(a, b, c) * RestTurn(t) < 0;
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
        double shoulders = Rest(Cols).Y;
        _cloth.Update(Top, i => toWorld(Rest(i)), elapsedMs, (x, y) =>
        {
            var (fx, fy) = toFrame((x, y));
            if (fy > shoulders) fx = Math.Min(fx, Front);
            fy = Math.Min(fy, -0.3);
            return toWorld((fx, fy));
        });
        for (int i = 0; i < _points.Length; i++) _points[i] = toFrame(_cloth.DrawnAt(i));
    }

    /// <summary>Forgets where it hung, so it falls from the shoulders afresh: a respawn, a cape put on.</summary>
    public void Reset() => _cloth = null;

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

    private static double RestTurn(int t) => Turn(Rest(Triangles[t * 3]), Rest(Triangles[t * 3 + 1]), Rest(Triangles[t * 3 + 2]));
}
