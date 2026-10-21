using System;
using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>
/// Each module technology's icon (0086), by its id (<c>Content.Modules</c>' <c>Tech</c>), as the user picked it on the
/// arc icon page (variant C, "Hexagon"): a thin hexagon frame every technology shares, and inside it the technology's own
/// glyph in its own colours, drawn in thin lines: a white-hot core, a coloured rim, a soft glow. The glyph's strokes
/// crackle, held on twos: redrawn twelve times a second, each time jagged afresh (<see cref="Strokes"/>). It marks what
/// takes a module, and what fits, in place of words: the socket's ghost, the slot mark of a module that fits the machine
/// open, the module's card. In shares of the icon's side, (0, 0) its top-left.
/// </summary>
public static class TechLooks
{
    /// <summary>A line through <paramref name="Points"/>; whether it crackles; how thick, against the icon's base width.</summary>
    public sealed record Stroke(IReadOnlyList<(float U, float V)> Points, bool Crackles = true, float Weight = 1);

    /// <summary>A technology's glyph inside the frame, and its colours, 0xRRGGBB: core, rim and glow.</summary>
    public sealed record Look(IReadOnlyList<Stroke> Glyph, uint Core, uint Rim, uint Glow);

    /// <summary>One line to draw: its points in shares of the side, the frame's or the glyph's, and its weight.</summary>
    public sealed record Line(IReadOnlyList<(float U, float V)> Points, bool Frame, float Weight);

    /// <summary>The frame every technology shares: a hexagon on its point, closed.</summary>
    public static readonly IReadOnlyList<(float U, float V)> Frame =
        [(.5f, .03f), (.9f, .265f), (.9f, .735f), (.5f, .97f), (.1f, .735f), (.1f, .265f), (.5f, .03f)];

    /// <summary>A stroke's base width, a share of the side, and never under <see cref="LeastWidth"/> pixels; the frame's, a share of that.</summary>
    public const float WidthShare = 0.08f, LeastWidth = 1.25f, FrameWeight = 0.55f;

    /// <summary>How often a crackle is redrawn, a second: on twos at 24 frames.</summary>
    public const int CracklesPerSecond = 12;

    /// <summary>How far a crackle's kink strays from its line, a share of the segment; a segment longer than <see cref="LongSegment"/> of the side kinks twice.</summary>
    public const float Stray = 0.15f, LongSegment = 0.22f;

    /// <summary>The chance a crackle sends off a branch, and the branch's length, at least and at most, a share of the side.</summary>
    public const float BranchChance = 0.25f, BranchShortest = 0.14f, BranchLongest = 0.24f;

    /// <summary>A crackle's brightness, at least and at most, against a still icon's 1.</summary>
    public const float Dimmest = 0.85f, Brightest = 1.15f;

    /// <summary>The arcs': a three-stroke bolt from top right to bottom left, violet-rimmed and white-hot.</summary>
    public static readonly Look Arc = new(
        [new Stroke(Fit((.3f, .2f, .4f, .6f), (.68f, 0f), (.2f, .56f), (.66f, .48f), (.3f, 1f)), Weight: 0.92f)],
        Core: 0xfcf6ff, Rim: 0xb284ff, Glow: 0xaa82ff);

    public static readonly IReadOnlyDictionary<string, Look> All = new Dictionary<string, Look> { ["arc"] = Arc };

    /// <summary>Technology <paramref name="tech"/>'s icon; throws for one with none.</summary>
    public static Look Of(string tech) => All[tech];

    /// <summary>
    /// The icon's lines, the frame first: still for <paramref name="seed"/> 0, else the glyph's crackling strokes jagged by
    /// the seed, each segment kinked once or twice off its line, and now and then a short branch off one kink.
    /// </summary>
    public static List<Line> Strokes(Look look, int seed)
    {
        var lines = new List<Line> { new(Frame, true, 1) };
        var random = seed == 0 ? null : new Random(seed);
        foreach (var stroke in look.Glyph)
        {
            if (random == null || !stroke.Crackles)
            {
                lines.Add(new Line(stroke.Points, false, stroke.Weight));
                continue;
            }
            var points = Jag(stroke.Points, random);
            lines.Add(new Line(points, false, stroke.Weight));
            if (random.NextDouble() < BranchChance) lines.Add(new Line(Branch(points, random), false, stroke.Weight * 0.6f));
        }
        return lines;
    }

    private static List<(float U, float V)> Jag(IReadOnlyList<(float U, float V)> points, Random random)
    {
        var jagged = new List<(float U, float V)> { points[0] };
        for (int i = 1; i < points.Count; i++)
        {
            var ((u0, v0), (u1, v1)) = (points[i - 1], points[i]);
            float du = u1 - u0, dv = v1 - v0, length = MathF.Max(MathF.Sqrt(du * du + dv * dv), 1e-6f);
            float nu = -dv / length, nv = du / length;
            int kinks = length > LongSegment ? 2 : 1;
            for (int j = 1; j <= kinks; j++)
            {
                float t = j / (kinks + 1f), off = (float)(random.NextDouble() * 2 - 1) * Stray * length;
                jagged.Add((u0 + du * t + nu * off, v0 + dv * t + nv * off));
            }
            jagged.Add((u1, v1));
        }
        return jagged;
    }

    /// <summary>A two-segment branch off one of <paramref name="points"/>' inner kinks, angled off the way on.</summary>
    private static List<(float U, float V)> Branch(List<(float U, float V)> points, Random random)
    {
        int i = 1 + (int)(random.NextDouble() * (points.Count - 2));
        var (u, v) = points[i];
        var (u1, v1) = points[i + 1];
        double angle = Math.Atan2(v1 - v, u1 - u) + (random.NextDouble() < 0.5 ? -1 : 1) * (0.6 + random.NextDouble() * 0.5);
        float length = BranchShortest + (float)random.NextDouble() * (BranchLongest - BranchShortest);
        float cu = (float)Math.Cos(angle), cv = (float)Math.Sin(angle);
        var mid = (u + cu * length * 0.5f + (float)(random.NextDouble() - 0.5) * length * 0.3f,
            v + cv * length * 0.5f + (float)(random.NextDouble() - 0.5) * length * 0.3f);
        return [(u, v), mid, (u + cu * length, v + cv * length)];
    }

    /// <summary>Points given in shares of <paramref name="box"/> (left, top, width, height), in shares of the side.</summary>
    private static (float U, float V)[] Fit((float L, float T, float W, float H) box, params (float U, float V)[] points) =>
        Array.ConvertAll(points, p => (box.L + p.U * box.W, box.T + p.V * box.H));
}
