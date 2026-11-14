using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// One picture of a limb bent at its elbow or knee as a whole piece (bent-limbs): a grid over the picture, rigid with its bone away
/// from the joint and, within <see cref="Band"/> of it, each row turned about the joint by a smooth share of the bend. Turning rows
/// about the joint, not blending their places, keeps the limb's width through a sharp bend.
/// <para>
/// The two pictures meet at a seam round the joint (<see cref="SeamOf"/>), where both are full width, so neither's rounded end
/// shows and nothing marks where the elbow or knee is: the upper is drawn down to the seam's end, fading out across it over the
/// lower, drawn from the seam's start.
/// </para>
/// </summary>
public sealed class BentLimb
{
    /// <summary>How long the stretch round the joint that bends is, in pixels; the rest stays rigid, so buckles and quilting keep their shape.</summary>
    public const double Band = 4;

    /// <summary>How many rows cross the band, its edges included: a row every quarter pixel.</summary>
    public const int BandRows = 17;

    /// <summary>
    /// Points across each row. A row turns whole, so two would place it exactly, but a quad turned into a wedge round the joint's
    /// outside is drawn as two triangles mapped flat, which kinks the picture along their diagonal into radial streaks (the knee's
    /// fan); narrow quads keep the picture where it belongs (<see cref="Warp"/>).
    /// </summary>
    public const int Columns = 9;

    /// <summary>How far round the joint the parts' pictures are looked at for their full width, pixels, and how much of it is full.</summary>
    private const double SeamReach = 3, Full = 0.85;

    /// <summary>The seam where no picture's widths are known: a pixel's fade across the joint.</summary>
    public static readonly (double From, double To) Plain = (-0.5, 0.5);

    /// <summary>No seam: a lower picture drawn whole from its top, as a soft helmet's are (helmets-fit-the-suit).</summary>
    public static readonly (double From, double To) Seamless = (double.NegativeInfinity, double.PositiveInfinity);

    private readonly double _joint, _band;
    private readonly bool _lower;
    private readonly Vec[] _rest;

    /// <summary>The grid's points as bent last, in the picture's bone frame, row by row down, left to right.</summary>
    public Vec[] Points { get; }

    /// <summary>Each point's place in the picture, in its pixels from its top-left corner.</summary>
    public Vec[] Uvs { get; }

    /// <summary>How opaque the picture is at each point: the upper fading out across the seam, all else whole.</summary>
    public double[] Alphas { get; }

    /// <summary>The grid's triangles, three points each, by index.</summary>
    public int[] Triangles { get; }

    /// <summary>The bend last drawn; none before the first.</summary>
    public double? Bent { get; private set; }

    /// <param name="left">The picture's left edge in its bone's frame, pixels, as it hangs straight down.</param>
    /// <param name="top">Its top edge.</param>
    /// <param name="width">Its width, pixels.</param>
    /// <param name="height">Its height, pixels; a shin cut at the ankle only its cut.</param>
    /// <param name="joint">Where the elbow or knee is down the frame: the upper bone's length for an upper picture, 0 for a lower one.</param>
    /// <param name="lower">Whether the picture hangs from the lower bone (forearm, shin) rather than the upper.</param>
    /// <param name="seam">Where the pictures meet, from and to, pixels down from the joint (<see cref="SeamOf"/>).</param>
    /// <param name="band">How tall the band it bends in is, pixels: <see cref="Band"/> for a limb, a helmet's own for its seal (helmets-fit-the-suit).</param>
    public BentLimb(double left, double top, double width, double height, double joint, bool lower, (double From, double To) seam, double band = Band)
    {
        (_joint, _lower, _band) = (joint, lower, band);
        double from = lower ? Math.Max(top, joint + seam.From) : top, to = lower ? top + height : Math.Min(top + height, joint + seam.To);
        if (to <= from) (from, to) = (top, top);
        var rows = new List<double> { from, to };
        foreach (double y in (double[])[joint + seam.From, joint + seam.To])
            if (y > from && y < to && !rows.Contains(y)) rows.Add(y);
        for (int i = 0; i < BandRows; i++)
        {
            double y = joint - band / 2 + band * i / (BandRows - 1);
            if (y > from && y < to && !rows.Contains(y)) rows.Add(y);
        }
        rows.Sort();
        _rest = new Vec[rows.Count * Columns];
        Uvs = new Vec[_rest.Length];
        Alphas = new double[_rest.Length];
        for (int r = 0; r < rows.Count; r++)
        {
            double y = rows[r];
            double fade = lower || seam.To <= seam.From ? 1 : Math.Clamp((joint + seam.To - y) / (seam.To - seam.From), 0, 1);
            for (int c = 0; c < Columns; c++)
            {
                double x = width * c / (Columns - 1);
                (_rest[r * Columns + c], Uvs[r * Columns + c], Alphas[r * Columns + c]) = (new Vec(left + x, y), new Vec(x, y - top), fade);
            }
        }
        Points = (Vec[])_rest.Clone();
        Triangles = new int[(rows.Count - 1) * (Columns - 1) * 6];
        int t = 0;
        for (int r = 0; r < rows.Count - 1; r++)
            for (int c = 0; c < Columns - 1; c++)
            {
                int a = r * Columns + c, b = a + Columns;
                foreach (int i in (ReadOnlySpan<int>)[a, a + 1, b + 1, a, b + 1, b]) Triangles[t++] = i;
            }
    }

    /// <summary>
    /// Bends the grid by <paramref name="bend"/>, the limb's <see cref="Bend.Lower"/>; false when it is bent so already, nothing
    /// to draw again.
    /// </summary>
    public bool Bend(double bend)
    {
        if (Bent == bend) return false;
        Bent = bend;
        for (int i = 0; i < _rest.Length; i++) Points[i] = Warp(_rest[i], _joint, bend, _lower, _band);
        return true;
    }

    /// <summary>How much of the bend a row <paramref name="along"/> pixels below the joint takes: none above the <paramref name="band"/>, all below it, smoothly between.</summary>
    public static double Share(double along, double band = Band)
    {
        double t = Math.Clamp(along / band + 0.5, 0, 1);
        return t * t * (3 - 2 * t);
    }

    /// <summary>
    /// Where <paramref name="rest"/>, a point of a picture hanging straight, is with the limb bent <paramref name="bend"/> at
    /// <paramref name="joint"/> down its frame: turned about the joint as the figure's nodes turn, clockwise; in the upper bone's
    /// frame by its <see cref="Share"/> of the bend, in the lower one's back by the rest of it.
    /// </summary>
    public static Vec Warp(Vec rest, double joint, double bend, bool lower, double band = Band)
    {
        double share = Share(rest.Y - joint, band);
        double turn = lower ? (1 - share) * bend : -share * bend;
        double c = Math.Cos(turn), s = Math.Sin(turn), y = rest.Y - joint;
        return new Vec(rest.X * c - y * s, joint + rest.X * s + y * c);
    }

    /// <summary>
    /// Where the two pictures of a limb meet, pixels down from the joint: from where the lower is full width, past its rounded top, to
    /// where the upper still is, before its rounded end; a gap between them closed at its middle. Each picture by its opaque width
    /// row by row, <paramref name="rowHeight"/> pixels a row, its first row <paramref name="upperTop"/> or <paramref name="lowerTop"/>
    /// pixels down from the joint; <see cref="Plain"/> without them.
    /// </summary>
    public static (double From, double To) SeamOf(IReadOnlyList<float>? upper, double upperTop, IReadOnlyList<float>? lower, double lowerTop, double rowHeight)
    {
        if (upper == null || lower == null) return Plain;
        // The upper from above the joint down, while full; the lower from below it up.
        double? to = Edge(upper, upperTop, rowHeight, down: true), from = Edge(lower, lowerTop, rowHeight, down: false);
        if (to is not double t || from is not double f) return Plain;
        return f <= t ? (f, t) : ((f + t) / 2, (f + t) / 2);
    }

    /// <summary>
    /// How far down from the joint a picture stays full width toward its rounded end, down for the upper, up for the lower: from the
    /// joint's row, or the nearest full one on the bone's side, within <see cref="SeamReach"/> of the joint.
    /// </summary>
    private static double? Edge(IReadOnlyList<float> widths, double top, double rowHeight, bool down)
    {
        int Row(double along) => (int)Math.Floor((along - top) / rowHeight);
        int first = Math.Max(0, Row(-SeamReach)), last = Math.Min(widths.Count - 1, Row(SeamReach));
        if (first > last) return null;
        float most = 0;
        for (int r = first; r <= last; r++) most = Math.Max(most, widths[r]);
        if (most <= 0) return null;
        bool Whole(int r) => r >= first && r <= last && widths[r] >= most * Full;
        int step = down ? 1 : -1, at = Math.Clamp(Row(0), first, last);
        while (!Whole(at) && at - step >= first && at - step <= last) at -= step;
        if (!Whole(at)) return null;
        while (Whole(at + step)) at += step;
        return top + (down ? at + 1 : at) * rowHeight;
    }
}
