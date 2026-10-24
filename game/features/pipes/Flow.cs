using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Pipes;

/// <summary>
/// A span oxygen is drawn flowing along (0109): from (<c>X</c>, <c>Y</c>), in cells, <c>Length</c> cells along the unit
/// way (<c>Dx</c>, <c>Dy</c>); <c>Steps</c> is how far along the pipes from a source its start is.
/// </summary>
public readonly record struct Run(double X, double Y, double Dx, double Dy, double Length, double Steps);

/// <summary>Oxygen as the pipes overlay shows it flowing (features/pipes/flow.ts, oxygen.ts), along spans.</summary>
public static class Flow
{
    /// <summary>Networks take these in turn in the overlay, so neighbours rarely share one.</summary>
    public static readonly uint[] Palette = [0x4fc3f7, 0xffb74d, 0xba68c8, 0x81c784, 0xf06292, 0xfff176, 0x4db6ac, 0xa1887f];

    /// <summary>The overlay colour of the <paramref name="index"/>-th network, as 0xRRGGBB.</summary>
    public static uint ColourOf(int index) => Palette[index % Palette.Length];

    private static (double X, double Y) MiddleOf(int cell, int width) => (cell % width + 0.5, cell / width + 0.5);

    /// <summary>
    /// The <paramref name="spans"/> (node cells, a grid <paramref name="width"/> wide) oxygen reaches from
    /// <paramref name="sources"/> through nodes that <paramref name="carry"/>, each run from its end nearer the sources, by
    /// the shortest way along the pipes, to the farther. Spans no source reaches are left out.
    /// </summary>
    public static List<Run> RunsOf(IReadOnlyList<(int A, int B)> spans, int width, IReadOnlyList<int> sources, System.Func<int, bool> carry)
    {
        double LengthOf(int a, int b)
        {
            var (ax, ay) = MiddleOf(a, width);
            var (bx, by) = MiddleOf(b, width);
            return System.Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
        }
        var far = new Dictionary<int, double>();
        var front = new List<int>();
        foreach (int s in sources)
            if (carry(s) && far.TryAdd(s, 0)) front.Add(s);
        // Few spans: a node is settled again whenever a shorter way to it turns up.
        while (front.Count > 0)
        {
            var next = new List<int>();
            foreach (int node in front)
                foreach (var (a, b) in spans)
                {
                    if (a != node && b != node) continue;
                    int other = a == node ? b : a;
                    if (!carry(other)) continue;
                    double d = far[node] + LengthOf(node, other);
                    if (far.TryGetValue(other, out double was) && was <= d) continue;
                    far[other] = d;
                    next.Add(other);
                }
            front = next;
        }
        var runs = new List<Run>();
        foreach (var (a, b) in spans)
        {
            if (!far.TryGetValue(a, out double da) || !far.TryGetValue(b, out double db)) continue;
            var (from, to, steps) = da <= db ? (a, b, da) : (b, a, db);
            var (fx, fy) = MiddleOf(from, width);
            var (tx, ty) = MiddleOf(to, width);
            double length = LengthOf(from, to);
            if (length == 0) continue;
            runs.Add(new Run(fx, fy, (tx - fx) / length, (ty - fy) / length, length, steps));
        }
        return runs;
    }

    /// <summary>
    /// Where dots flowing along <paramref name="runs"/> are when the flow has gone <paramref name="along"/> cells: one every
    /// <paramref name="every"/> cells of the way from the sources, each moving on a cell as <paramref name="along"/> grows by one.
    /// </summary>
    public static List<(double X, double Y)> DotsOf(IReadOnlyList<Run> runs, double along, int every)
    {
        var dots = new List<(double X, double Y)>();
        foreach (var run in runs)
            for (double u = ((along - run.Steps) % every + every) % every; u < run.Length; u += every)
                dots.Add((run.X + run.Dx * u, run.Y + run.Dy * u));
        return dots;
    }

    /// <summary>The spans of <paramref name="network"/>'s nodes, as pairs: those with an end in it.</summary>
    public static List<(int A, int B)> SpansIn(IEnumerable<(int A, int B)> spans, IReadOnlyCollection<int> network) =>
        spans.Where(s => network.Contains(s.A) || network.Contains(s.B)).ToList();
}
