using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game;

/// <summary>
/// The glowing line icons (0086, 0097): a thin frame and inside it a glyph, drawn in thin lines with a white-hot core, a
/// coloured rim and a soft glow by <see cref="Render.IconCanvas"/>. The frame says what kind of sign it is: a hexagon is
/// a module's technology (<see cref="TechLooks"/>), a ring what a station does (<see cref="MarkLooks"/>). Lit, a glyph
/// moves its own way, held on twos: redrawn twelve times a second (<see cref="Lines"/>). In shares of the icon's side,
/// (0, 0) its top-left.
/// </summary>
public static class IconLooks
{
    /// <summary>A line through <paramref name="Points"/>; whether its look's motion moves it; how thick, against the icon's base width; for a filling glyph, the step it lights at.</summary>
    public sealed record Stroke(IReadOnlyList<(float U, float V)> Points, bool Moves = true, float Weight = 1, int Step = 0);

    /// <summary>
    /// An icon: its frame, its glyph inside it and its colours, 0xRRGGBB: core, rim and glow; how its glyph moves while
    /// lit, about <paramref name="Pivot"/> for a glyph that sways or flickers from its foot.
    /// </summary>
    public sealed record Look(
        IReadOnlyList<(float U, float V)> Frame, IReadOnlyList<Stroke> Glyph, uint Core, uint Rim, uint Glow,
        IconMotion Motion, (float U, float V) Pivot = default);

    /// <summary>One line to draw: its points in shares of the side, the frame's or the glyph's, and its weight.</summary>
    public sealed record Line(IReadOnlyList<(float U, float V)> Points, bool Frame, float Weight);

    /// <summary>One drawing of an icon: its lines, the frame first, and how bright, against a still icon's 1.</summary>
    public sealed record Pose(List<Line> Lines, float Intensity);

    /// <summary>A module technology's frame: a hexagon on its point, closed.</summary>
    public static readonly IReadOnlyList<(float U, float V)> Hexagon =
        [(.5f, .03f), (.9f, .265f), (.9f, .735f), (.5f, .97f), (.1f, .735f), (.1f, .265f), (.5f, .03f)];

    /// <summary>A station's mark's frame: a ring as wide as the hexagon, of <see cref="RingPoints"/> points, closed.</summary>
    public static readonly IReadOnlyList<(float U, float V)> Ring = Circle(.5f, .5f, .43f, RingPoints);

    /// <summary>How many points the ring is drawn through: smooth at 32 px.</summary>
    public const int RingPoints = 28;

    /// <summary>A stroke's base width, a share of the side, and never under <see cref="LeastWidth"/> pixels; the frame's, a share of that.</summary>
    public const float WidthShare = 0.08f, LeastWidth = 1.25f, FrameWeight = 0.55f;

    /// <summary>How often a moving glyph is redrawn, a second: on twos at 24 frames.</summary>
    public const int FramesPerSecond = 12;

    /// <summary>A crackle's: how far its kink strays from its line, a share of the segment; a segment longer than <see cref="LongSegment"/> of the side kinks twice.</summary>
    public const float Stray = 0.15f, LongSegment = 0.22f;

    /// <summary>The chance a crackle sends off a branch, and the branch's length, at least and at most, a share of the side.</summary>
    public const float BranchChance = 0.25f, BranchShortest = 0.14f, BranchLongest = 0.24f;

    /// <summary>A crackle's or a flicker's brightness, at least and at most, against a still icon's 1.</summary>
    public const float Dimmest = 0.85f, Brightest = 1.15f;

    /// <summary>How high over its pivot a glyph that flickers or sways moves in full, a share of the side: about a glyph's height.</summary>
    public const float Reach = 0.5f;

    /// <summary>How many fresh frames a flicker has before it comes round again: two seconds, too long to be seen repeating.</summary>
    public const int FlickerFrames = 24;

    /// <summary>A flicker's: how far a point strays sideways and up, a share of the side, at <see cref="Reach"/>; its foot stays put.</summary>
    public const float FlickerSway = 0.05f, FlickerLift = 0.04f;

    /// <summary>A sway's: its swing at <see cref="Reach"/>, a share of the side, and one swing there and back, in frames.</summary>
    public const float SwayReach = 0.06f;
    public const int SwayFrames = 30;

    /// <summary>A simmer's: how far a wisp's top sways, a share of the side, its brightness breathing by <see cref="SimmerBreath"/>; one round, in frames.</summary>
    public const float SimmerBob = 0.035f, SimmerBreath = 0.12f;
    public const int SimmerFrames = 16;

    /// <summary>A fill's: how many frames each step holds, the last held twice as long.</summary>
    public const int FillFrames = 5;

    /// <summary>A spin's: how far its moving strokes, rotor blades, turn each frame about their middles, in degrees.</summary>
    public const int SpinStep = 60;

    /// <summary>
    /// The icon's lines, the frame first: still for <paramref name="frame"/> 0 (a ghost, a test); else its glyph moved as
    /// its <see cref="Look.Motion"/> moves it at that frame, a frame a twelfth of a second. A random motion is seeded by the
    /// frame, so each frame is its own and the same frame draws alike.
    /// </summary>
    public static Pose Lines(Look look, int frame)
    {
        var lines = new List<Line> { new(look.Frame, true, 1) };
        var random = frame == 0 ? null : new Random(frame);
        if (frame == 0 || look.Motion == IconMotion.Still)
        {
            foreach (var s in look.Glyph) lines.Add(new Line(s.Points, false, s.Weight));
            return new Pose(lines, 1);
        }
        float intensity = 1;
        switch (look.Motion)
        {
            case IconMotion.Crackle:
                foreach (var s in look.Glyph)
                {
                    if (!s.Moves)
                    {
                        lines.Add(new Line(s.Points, false, s.Weight));
                        continue;
                    }
                    var points = Jag(s.Points, random!);
                    lines.Add(new Line(points, false, s.Weight));
                    if (random!.NextDouble() < BranchChance) lines.Add(new Line(Branch(points, random), false, s.Weight * 0.6f));
                }
                intensity = Between(random!, Dimmest, Brightest);
                break;
            case IconMotion.Flicker:
                // One gust for the whole flame, each point taking it by how high it stands, and a little of its own.
                float gust = Between(random!, -1, 1), lift = Between(random!, -1, 1);
                foreach (var s in look.Glyph)
                    lines.Add(new Line(s.Moves ? Moved(s.Points, p =>
                    {
                        float up = Height(look, p);
                        return (p.U + (gust * 0.8f + Between(random!, -0.2f, 0.2f)) * FlickerSway * up, p.V - lift * FlickerLift * up);
                    }) : s.Points, false, s.Weight));
                intensity = Between(random!, Dimmest, Brightest);
                break;
            case IconMotion.Sway:
                float swing = SwayReach * Wave(frame, SwayFrames);
                foreach (var s in look.Glyph)
                    lines.Add(new Line(s.Moves ? Moved(s.Points, p =>
                    {
                        // A bend, not a tilt: the higher a point, the farther it goes, more than in proportion.
                        float up = Height(look, p);
                        return (p.U + swing * up * up, p.V + MathF.Abs(swing) * up * up * 0.3f);
                    }) : s.Points, false, s.Weight));
                break;
            case IconMotion.Simmer:
                float phase = (float)(frame % SimmerFrames) / SimmerFrames;
                foreach (var s in look.Glyph)
                    lines.Add(new Line(s.Moves ? Rippled(s.Points, phase) : s.Points, false, s.Weight));
                intensity = 1 + SimmerBreath * Wave(frame + SimmerFrames / 4, SimmerFrames);
                break;
            case IconMotion.Fill:
                int steps = 0;
                foreach (var s in look.Glyph) steps = Math.Max(steps, s.Step);
                // Each step lit in turn, the full one held twice as long, then empty again.
                int at = frame % ((steps + 2) * FillFrames) / FillFrames;
                foreach (var s in look.Glyph)
                    if (s.Step <= Math.Min(at, steps)) lines.Add(new Line(s.Points, false, s.Weight));
                break;
            case IconMotion.Spin:
                // Each blade a third of the way round each frame, every rotor its own way, so the four never turn as one.
                for (int n = 0; n < look.Glyph.Count; n++)
                {
                    var s = look.Glyph[n];
                    float turn = MathF.PI / 180 * SpinStep * (frame % (360 / SpinStep)) * (n % 2 == 0 ? 1 : -1);
                    lines.Add(new Line(s.Moves ? Turned(s.Points, turn) : s.Points, false, s.Weight));
                }
                break;
        }
        return new Pose(lines, intensity);
    }

    /// <summary>
    /// How many frames <paramref name="look"/>'s motion takes to come round, frames 1 to it drawing it all, so they can be
    /// painted once and shared by every icon of it; null for a crackle, jagged afresh each frame for good.
    /// </summary>
    public static int? Cycle(Look look) => look.Motion switch
    {
        IconMotion.Still => 1,
        IconMotion.Crackle => null,
        IconMotion.Flicker => FlickerFrames,
        IconMotion.Sway => SwayFrames,
        IconMotion.Simmer => SimmerFrames,
        IconMotion.Fill => (look.Glyph.Max(s => s.Step) + 2) * FillFrames,
        IconMotion.Spin => 360 / SpinStep,
        _ => throw new ArgumentOutOfRangeException(nameof(look)),
    };

    /// <summary>How high a point stands over the look's pivot, in shares of <see cref="Reach"/>: 0 at its foot, 1 that high and over.</summary>
    private static float Height(Look look, (float U, float V) p) => Math.Clamp((look.Pivot.V - p.V) / Reach, 0, 1);

    /// <summary>A smooth swing from -1 to 1 and back over <paramref name="period"/> frames.</summary>
    private static float Wave(int frame, int period) => MathF.Sin(MathF.Tau * (frame % period) / period);

    private static float Between(Random random, float least, float most) => least + (float)random.NextDouble() * (most - least);

    private static (float U, float V)[] Moved(IReadOnlyList<(float U, float V)> points, Func<(float U, float V), (float U, float V)> move)
    {
        var moved = new (float U, float V)[points.Count];
        for (int i = 0; i < points.Count; i++) moved[i] = move(points[i]);
        return moved;
    }

    /// <summary>A line whose points past its foot sway sideways in a wave running up it at <paramref name="phase"/> of its round: heat rising.</summary>
    private static (float U, float V)[] Rippled(IReadOnlyList<(float U, float V)> points, float phase)
    {
        var moved = new (float U, float V)[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            float t = points.Count > 1 ? i / (points.Count - 1f) : 0;
            moved[i] = (points[i].U + SimmerBob * t * MathF.Sin(MathF.Tau * (t - phase)), points[i].V);
        }
        return moved;
    }

    /// <summary>A line turned by <paramref name="angle"/> radians about its points' middle: a rotor's blade.</summary>
    private static (float U, float V)[] Turned(IReadOnlyList<(float U, float V)> points, float angle)
    {
        float cu = 0, cv = 0;
        foreach (var (u, v) in points) (cu, cv) = (cu + u, cv + v);
        (cu, cv) = (cu / points.Count, cv / points.Count);
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return Moved(points, p => (cu + (p.U - cu) * c - (p.V - cv) * s, cv + (p.U - cu) * s + (p.V - cv) * c));
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
    public static (float U, float V)[] Fit((float L, float T, float W, float H) box, params (float U, float V)[] points) =>
        Array.ConvertAll(points, p => (box.L + p.U * box.W, box.T + p.V * box.H));

    /// <summary>A closed circle round (<paramref name="u"/>, <paramref name="v"/>) through <paramref name="count"/> points, from its top clockwise.</summary>
    public static (float U, float V)[] Circle(float u, float v, float radius, int count)
    {
        var points = new (float U, float V)[count + 1];
        for (int i = 0; i < count; i++)
        {
            float a = MathF.Tau * i / count;
            points[i] = (u + radius * MathF.Sin(a), v - radius * MathF.Cos(a));
        }
        points[count] = points[0];
        return points;
    }
}

/// <summary>How a lit icon's glyph moves (<see cref="IconLooks.Lines"/>), on twos; a ghost never does.</summary>
public enum IconMotion
{
    /// <summary>Not at all: the "?".</summary>
    Still,
    /// <summary>Jagged afresh each frame, now and then branching, flickering bright: the arc's bolt.</summary>
    Crackle,
    /// <summary>Its tip licking side to side and up, its foot still, flickering bright: a flame.</summary>
    Flicker,
    /// <summary>Bending from its foot one way and back, slow: a sprout.</summary>
    Sway,
    /// <summary>Its moving strokes swaying in a wave running up them, its brightness breathing: heat off a hot ingot.</summary>
    Simmer,
    /// <summary>Its strokes lit step by step, then empty again: a battery charging.</summary>
    Fill,
    /// <summary>Its moving strokes turning about their middles, every other one the other way: a drone's rotors.</summary>
    Spin,
}
