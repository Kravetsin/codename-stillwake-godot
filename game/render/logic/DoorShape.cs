using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>
/// One door, every shape (0067): the pictures a doorway is drawn with by the joins of its cells, as the sim decides them
/// (<see cref="Doorways.LinksOf"/>). Its pictures are the 16 px door's (0104), each over two by two of its cells, a door two
/// cells thick and a hatch two cells tall: a door's pieces by where they lie up and down it, a hatch's by where they lie
/// along it. Between shut and open, the doorway's panel slides as one: a door's up into its top, a hatch's along into its
/// left end, each piece drawing the part of the panel that has slid over it.
/// </summary>
public static class DoorShape
{
    /// <summary>How far a panel slides between two pictures, in pixels.</summary>
    public const int Stride = 2;

    /// <summary>The pixels of a piece, the 16 px door's (0104), and the cells it covers each way.</summary>
    public const int Drawn = Camera.Drawn, Per = Drawn / Camera.Cell;

    /// <summary>
    /// Where a door cell lies in its doorway: in a <paramref name="Hatch"/> or a door, <paramref name="Along"/> cells from the
    /// end its panel slides into (a door's top, a hatch's left) of a run <paramref name="Length"/> cells long, and
    /// <paramref name="Across"/> cells into its thickness from its left (a door's) or top (a hatch's) side.
    /// </summary>
    public readonly record struct Spot(bool Hatch, int Along, int Length, int Across);

    /// <summary>
    /// Where door cell <paramref name="cell"/> of a grid <paramref name="width"/> wide lies in its doorway, a
    /// <paramref name="hatch"/> or a door, by <paramref name="linksOf"/> (<see cref="Doorways.LinksOf"/>).
    /// </summary>
    public static Spot SpotOf(int cell, int width, Func<int, int> linksOf, bool hatch)
    {
        var (back, ahead, step) = hatch ? (Doorways.Left, Doorways.Right, 1) : (Doorways.Up, Doorways.Down, width);
        int along = 0, length = 1, across = 0;
        for (int c = cell; (linksOf(c) & back) != 0; c -= step) along++;
        for (int c = cell; (linksOf(c) & ahead) != 0; c += step) length++;
        if (!hatch) for (int c = cell; (linksOf(c) & Doorways.Left) != 0; c -= 1) across++;
        return new Spot(hatch, along, length + along, across);
    }

    /// <summary>
    /// The 16 px picture a door cell at <paramref name="spot"/> draws a quarter of, while its panel has slid <paramref name="shift"/>
    /// pixels open (0 shut), and which quarter, across and down: its piece's along the run, its own of the piece's two by two.
    /// </summary>
    public static (string Picture, (int X, int Y) Quarter) TileOf(Spot spot, int shift)
    {
        int piece = spot.Along / Per, pieces = (spot.Length + Per - 1) / Per;
        var quarter = spot.Hatch ? (spot.Along % Per, spot.Across % Per) : (spot.Across % Per, spot.Along % Per);
        return (Picture(spot.Hatch, piece, pieces, shift), quarter);
    }

    /// <summary>A door cell's quarter of its picture at rest, shut or open.</summary>
    public static (string Picture, (int X, int Y) Quarter) TileOf(Spot spot, bool closed) => TileOf(spot, closed ? 0 : int.MaxValue / 2);

    /// <summary>
    /// The picture of piece <paramref name="index"/> of a doorway <paramref name="length"/> pieces long, counted from its top or
    /// left end, a <paramref name="hatch"/> or a door, while its panel has slid <paramref name="shift"/> pixels open: shut at
    /// 0, open from <c>length</c> pieces on. <paramref name="shift"/> is rounded down to a <see cref="Stride"/>.
    /// </summary>
    public static string Picture(bool hatch, int index, int length, int shift)
    {
        int piece = PieceAt(hatch, index, length);
        shift = shift / Stride * Stride;
        if (shift <= 0) return Name(hatch, "closed", piece);
        int over = index + shift / Drawn;
        if (over >= length) return Name(hatch, "open", piece);
        return SlidName(hatch, piece, PieceAt(hatch, over, length), over + 1 < length ? PieceAt(hatch, over + 1, length) : None,
            shift % Drawn);
    }

    /// <summary>The pictures of a whole door or <paramref name="hatch"/> <paramref name="length"/> pieces long, shut, from its top or left end: a plan's.</summary>
    public static IReadOnlyList<string> Whole(bool hatch, int length) =>
        Enumerable.Range(0, length).Select(i => Name(hatch, "closed", PieceAt(hatch, i, length))).ToArray();

    /// <summary>Whether a doorway is heard, and toggled by a script, at this cell of it: joined neither above nor to its left.</summary>
    public static bool Leads(int links) => (links & (Doorways.Up | Doorways.Left)) == 0;

    /// <summary>Every hatch picture there is, each once: what the ship's atlas packs beside the doors'.</summary>
    public static IEnumerable<string> HatchPictures =>
        new[] { 0, Doorways.Right, Doorways.Left | Doorways.Right, Doorways.Left }.SelectMany(p => new[] { Name(true, "closed", p), Name(true, "open", p) });

    /// <summary>
    /// Every picture of a piece part way through a slide, each once: its name, the piece's own shut and open pictures, those
    /// of the piece whose panel slides over it and of the one after (null past the doorway's end), how far into the first
    /// the panel has slid, and whether it is a hatch's. What the ship's atlas makes with <see cref="Slid"/>; doorways
    /// longer than five pieces only repeat their middles.
    /// </summary>
    public static IEnumerable<SlidPicture> SlidPictures =>
        new[] { false, true }.SelectMany(hatch => Enumerable.Range(1, 5).SelectMany(length => Enumerable.Range(0, length).SelectMany(index =>
            Enumerable.Range(1, length * Drawn / Stride - 1).Select(step => Spec(hatch, index, length, step * Stride)))))
        .OfType<SlidPicture>().DistinctBy(p => p.Name);

    /// <summary>A picture part way through a slide, as <see cref="SlidPictures"/> lists it.</summary>
    public sealed record SlidPicture(string Name, string Closed, string Open, string OverClosed, string OverOpen,
        string? NextClosed, string? NextOpen, int Shift, bool Hatch);

    /// <summary>
    /// A piece part way through a slide (RGBA, <paramref name="width"/> pixels a row): its open picture, but where its shut and
    /// open pictures differ, the panel <paramref name="shift"/> pixels further down (a door's, rising) or right (a hatch's,
    /// sliding left), read from <paramref name="overClosed"/> and past its edge from <paramref name="nextClosed"/>, while
    /// that pixel is still panel; past the panel's end, open.
    /// </summary>
    public static byte[] Slid(byte[] closed, byte[] open, byte[] overClosed, byte[] overOpen, byte[]? nextClosed, byte[]? nextOpen,
        int width, int shift, bool hatch)
    {
        int height = closed.Length / 4 / width, along = hatch ? width : height;
        var slid = (byte[])open.Clone();
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (!Panel(closed, open, (y * width + x) * 4)) continue;
                int from = (hatch ? x : y) + shift;
                var (shut, opened) = from < along ? (overClosed, overOpen) : (nextClosed, nextOpen);
                if (shut == null || opened == null) continue;
                from %= along;
                int i = (hatch ? y * width + from : from * width + x) * 4;
                if (Panel(shut, opened, i)) System.Array.Copy(shut, i, slid, (y * width + x) * 4, 4);
            }
        return slid;
    }

    private static bool Panel(byte[] closed, byte[] open, int i) =>
        closed[i] != open[i] || closed[i + 1] != open[i + 1] || closed[i + 2] != open[i + 2] || closed[i + 3] != open[i + 3];

    private const int None = -1;

    /// <summary>The piece <paramref name="index"/> of a doorway <paramref name="length"/> pieces long is, by its joins: a door piece alone is a hatch.</summary>
    private static int PieceAt(bool hatch, int index, int length)
    {
        var (back, ahead) = hatch ? (Doorways.Left, Doorways.Right) : (Doorways.Up, Doorways.Down);
        return (index > 0 ? back : 0) | (index < length - 1 ? ahead : 0);
    }

    private static SlidPicture? Spec(bool hatch, int index, int length, int shift)
    {
        int over = index + shift / Drawn;
        if (over >= length || (!hatch && length == 1)) return null;
        int piece = PieceAt(hatch, index, length), overPiece = PieceAt(hatch, over, length);
        int next = over + 1 < length ? PieceAt(hatch, over + 1, length) : None;
        return new SlidPicture(SlidName(hatch, piece, overPiece, next, shift % Drawn), Name(hatch, "closed", piece), Name(hatch, "open", piece),
            Name(hatch, "closed", overPiece), Name(hatch, "open", overPiece),
            next == None ? null : Name(hatch, "closed", next), next == None ? null : Name(hatch, "open", next), shift % Drawn, hatch);
    }

    private static string Name(bool hatch, string look, int piece) => $"{(hatch ? "hatch" : "door")}_{look}_{piece}";

    private static string SlidName(bool hatch, int piece, int over, int next, int shift) =>
        $"{(hatch ? "hatch" : "door")}_slid_{piece}_{over}_{(next == None ? "x" : next)}_{shift}";
}
