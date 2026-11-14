using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// One picture of a limb bent at its elbow or knee as a whole piece (exp/bent-limbs), instead of turning rigid at the joint: a strip
/// of rows across the picture, rigid with its bone away from the joint and, within <see cref="Band"/> of it, each row turned about the
/// joint by a smooth share of the bend. Turning rows about the joint, not blending their places, keeps the limb's width through a
/// sharp bend. A row turns whole, so its two ends map it exactly: two columns, rows dense only in the band.
/// <para>
/// The two pictures meet at a seam round the joint (<see cref="SeamOf"/>) instead of the upper's rounded end lying over the lower's:
/// bent, those ends would fan out round the joint. The upper is drawn down to the seam's end, fading out across it over the lower,
/// drawn from the seam's start.
/// </para>
/// </summary>
public sealed class BentLimb
{
    /// <summary>How long the stretch round the joint that bends is, in pixels; the rest stays rigid, so buckles and quilting keep their shape.</summary>
    public const double Band = 4;

    /// <summary>How many rows cross the band, its edges included: a row every quarter pixel.</summary>
    public const int BandRows = 17;

    /// <summary>How far round the joint the parts' pictures are looked at for their full width, pixels, and how much of it is full.</summary>
    private const double SeamReach = 3, Full = 0.85;

    /// <summary>The seam where no picture's widths are known: a pixel's fade across the joint.</summary>
    public static readonly (double From, double To) Plain = (-0.5, 0.5);

    private readonly double _joint;
    private readonly bool _lower;
    private readonly Vec[] _rest;

    /// <summary>The strip's corners as bent last, in the picture's bone frame: a row's left then its right, row by row down.</summary>
    public Vec[] Points { get; }

    /// <summary>Each point's place in the picture, in its pixels from its top-left corner.</summary>
    public Vec[] Uvs { get; }

    /// <summary>How opaque the picture is at each point: the upper fading out across the seam, all else whole.</summary>
    public double[] Alphas { get; }

    /// <summary>The strip's triangles, three points each, by index.</summary>
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
    public BentLimb(double left, double top, double width, double height, double joint, bool lower, (double From, double To) seam)
    {
        (_joint, _lower) = (joint, lower);
        double from = lower ? Math.Max(top, joint + seam.From) : top, to = lower ? top + height : Math.Min(top + height, joint + seam.To);
        if (to <= from) (from, to) = (top, top);
        var rows = new List<double> { from, to };
        foreach (double y in (double[])[joint + seam.From, joint + seam.To])
            if (y > from && y < to && !rows.Contains(y)) rows.Add(y);
        for (int i = 0; i < BandRows; i++)
        {
            double y = joint - Band / 2 + Band * i / (BandRows - 1);
            if (y > from && y < to && !rows.Contains(y)) rows.Add(y);
        }
        rows.Sort();
        _rest = new Vec[rows.Count * 2];
        Uvs = new Vec[_rest.Length];
        Alphas = new double[_rest.Length];
        for (int r = 0; r < rows.Count; r++)
        {
            double y = rows[r];
            (_rest[2 * r], _rest[2 * r + 1]) = (new Vec(left, y), new Vec(left + width, y));
            (Uvs[2 * r], Uvs[2 * r + 1]) = (new Vec(0, y - top), new Vec(width, y - top));
            double fade = lower || seam.To <= seam.From ? 1 : Math.Clamp((joint + seam.To - y) / (seam.To - seam.From), 0, 1);
            Alphas[2 * r] = Alphas[2 * r + 1] = fade;
        }
        Points = (Vec[])_rest.Clone();
        Triangles = new int[(rows.Count - 1) * 6];
        for (int r = 0, a = 0; r < rows.Count - 1; r++, a += 2)
            (Triangles[r * 6], Triangles[r * 6 + 1], Triangles[r * 6 + 2], Triangles[r * 6 + 3], Triangles[r * 6 + 4], Triangles[r * 6 + 5]) =
                (a, a + 1, a + 3, a, a + 3, a + 2);
    }

    /// <summary>
    /// Bends the strip by <paramref name="bend"/>, the limb's <see cref="Bend.Lower"/>; false when it is bent so already, nothing
    /// to draw again.
    /// </summary>
    public bool Bend(double bend)
    {
        if (Bent == bend) return false;
        Bent = bend;
        for (int i = 0; i < _rest.Length; i++) Points[i] = Warp(_rest[i], _joint, bend, _lower);
        return true;
    }

    /// <summary>How much of the bend a row <paramref name="along"/> pixels below the joint takes: none above the band, all below it, smoothly between.</summary>
    public static double Share(double along)
    {
        double t = Math.Clamp(along / Band + 0.5, 0, 1);
        return t * t * (3 - 2 * t);
    }

    /// <summary>
    /// Where <paramref name="rest"/>, a point of a picture hanging straight, is with the limb bent <paramref name="bend"/> at
    /// <paramref name="joint"/> down its frame: turned about the joint as the figure's nodes turn, clockwise; in the upper bone's
    /// frame by its <see cref="Share"/> of the bend, in the lower one's back by the rest of it.
    /// </summary>
    public static Vec Warp(Vec rest, double joint, double bend, bool lower)
    {
        double share = Share(rest.Y - joint);
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
