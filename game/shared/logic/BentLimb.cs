using System;
using System.Collections.Generic;

namespace Stillwake.Game.Shared;

/// <summary>
/// One picture of a limb bent at its elbow or knee as a whole piece (bent-limbs, whole-limbs): a grid over the picture, rigid with its
/// bone away from the joint and, within <see cref="Band"/> of it, each row turned about the joint by a smooth share of the bend.
/// Turning rows about the joint, not blending their places, keeps the limb's width through a sharp bend.
/// <para>
/// A hero's arm and leg, a creature's leg, neck or tail are each one picture bent so (creatures-bend): no end of a picture anywhere
/// near the joint. A neck or tail lies along its own axis, not straight down its picture; its rows cross that axis.
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

    private readonly double _joint, _band, _axis;
    private readonly bool _lower;
    private readonly Vec[] _rest;

    /// <summary>The grid's points as bent last, in the picture's frame, row by row down its bone, left to right.</summary>
    public Vec[] Points { get; }

    /// <summary>Each point's place in the picture, in its pixels from its top-left corner.</summary>
    public Vec[] Uvs { get; }

    /// <summary>The grid's triangles, three points each, by index.</summary>
    public int[] Triangles { get; }

    /// <summary>The bend last drawn; none before the first.</summary>
    public double? Bent { get; private set; }

    /// <param name="left">The picture's left edge in its bone's frame, pixels, as it hangs straight down.</param>
    /// <param name="top">Its top edge.</param>
    /// <param name="width">Its width, pixels.</param>
    /// <param name="height">Its height, pixels; a leg cut at the ankle only its cut.</param>
    /// <param name="joint">Where the elbow or knee is down the frame: the upper bone's length for a picture hung from it, 0 for one hung from the lower.</param>
    /// <param name="lower">Whether the picture hangs from the lower bone (a hero's arm and leg) rather than the upper.</param>
    /// <param name="band">How tall the band it bends in is, pixels: <see cref="Band"/> for a limb, a helmet's own for its seal (helmets-fit-the-suit).</param>
    /// <param name="axis">
    /// Which way its bone runs from the frame's origin, radians clockwise from straight down: 0 for a limb, down its picture; a
    /// creature's neck or tail its own, the grid then over the picture's box turned onto the bone.
    /// </param>
    public BentLimb(double left, double top, double width, double height, double joint, bool lower, double band = Band, double axis = 0)
    {
        (_joint, _lower, _band, _axis) = (joint, lower, band, axis);
        var (pictureLeft, pictureTop) = (left, top);
        (left, top, width, height) = OnBone(left, top, width, height, axis);
        double from = top, to = top + height;
        var rows = new List<double> { from, to };
        for (int i = 0; i < BandRows; i++)
        {
            double y = joint - band / 2 + band * i / (BandRows - 1);
            if (y > from && y < to && !rows.Contains(y)) rows.Add(y);
        }
        rows.Sort();
        _rest = new Vec[rows.Count * Columns];
        Uvs = new Vec[_rest.Length];
        for (int r = 0; r < rows.Count; r++)
        {
            double y = rows[r];
            for (int c = 0; c < Columns; c++)
            {
                var rest = new Vec(left + width * c / (Columns - 1), y);
                var inPicture = Turned(rest, axis);
                (_rest[r * Columns + c], Uvs[r * Columns + c]) = (rest, new Vec(inPicture.X - pictureLeft, inPicture.Y - pictureTop));
            }
        }
        Points = Array.ConvertAll(_rest, p => Turned(p, axis));
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
    /// Bends the grid by <paramref name="bend"/>, the limb's bend (the hero's <c>Bend.Lower</c>); false when it is bent so already,
    /// nothing to draw again.
    /// </summary>
    public bool Bend(double bend)
    {
        if (Bent == bend) return false;
        Bent = bend;
        for (int i = 0; i < _rest.Length; i++) Points[i] = Turned(Warp(_rest[i], _joint, bend, _lower, _band), _axis);
        return true;
    }

    /// <summary><paramref name="v"/> turned <paramref name="turn"/> radians clockwise about the origin: from a bone's frame into its picture's.</summary>
    public static Vec Turned(Vec v, double turn)
    {
        if (turn == 0) return v;
        double c = Math.Cos(turn), s = Math.Sin(turn);
        return new Vec(v.X * c - v.Y * s, v.X * s + v.Y * c);
    }

    /// <summary>The box round a picture's box turned onto a bone running <paramref name="axis"/> from straight down: the box itself for a limb.</summary>
    private static (double Left, double Top, double Width, double Height) OnBone(double left, double top, double width, double height, double axis)
    {
        if (axis == 0) return (left, top, width, height);
        double x0 = double.PositiveInfinity, y0 = double.PositiveInfinity, x1 = double.NegativeInfinity, y1 = double.NegativeInfinity;
        foreach (var corner in (ReadOnlySpan<Vec>)[new(left, top), new(left + width, top), new(left, top + height), new(left + width, top + height)])
        {
            var p = Turned(corner, -axis);
            (x0, y0, x1, y1) = (Math.Min(x0, p.X), Math.Min(y0, p.Y), Math.Max(x1, p.X), Math.Max(y1, p.Y));
        }
        return (x0, y0, x1 - x0, y1 - y0);
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
}
