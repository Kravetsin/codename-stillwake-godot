using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>
/// One door, every shape (0067): the pictures a door cell is drawn with by the sides it joins its doorway on, as
/// the sim decides them (<see cref="Doorways.LinksOf"/>): a door's pieces by its joins up and down, a hatch's by
/// its joins either side. Between shut and open, the doorway's panel slides as one: a door's up into its top,
/// a hatch's along into its left end, each cell drawing the part of the panel that has slid over it.
/// </summary>
public static class DoorShape
{
    /// <summary>How far a panel slides between two pictures, in pixels.</summary>
    public const int Stride = 2;

    /// <summary>The picture of a door cell joined on <paramref name="links"/> (<see cref="Doorways.LinksOf"/>), shut or slid open.</summary>
    public static string Picture(bool closed, int links) => (closed ? Closed : Open)[links & 15];

    /// <summary>
    /// The picture of cell <paramref name="index"/> of a doorway <paramref name="length"/> cells long, counted from its top or
    /// left end, a <paramref name="hatch"/> or a door, while its panel has slid <paramref name="shift"/> pixels open: shut at
    /// 0, open from <c>length</c> cells on. <paramref name="shift"/> is rounded down to a <see cref="Stride"/>.
    /// </summary>
    public static string Picture(bool hatch, int index, int length, int shift)
    {
        int piece = PieceAt(hatch, index, length);
        shift = shift / Stride * Stride;
        if (shift <= 0) return Name(hatch, "closed", piece);
        int over = index + shift / Cell;
        if (over >= length) return Name(hatch, "open", piece);
        return SlidName(hatch, piece, PieceAt(hatch, over, length), over + 1 < length ? PieceAt(hatch, over + 1, length) : None,
            shift % Cell);
    }

    /// <summary>Whether a doorway is heard, and toggled by a script, at this cell of it: joined neither above nor to its left.</summary>
    public static bool Leads(int links) => (links & (Doorways.Up | Doorways.Left)) == 0;

    /// <summary>Every hatch picture there is, each once: what the ship's atlas packs beside the doors'.</summary>
    public static IEnumerable<string> HatchPictures =>
        Enumerable.Range(0, 16).Where(Doorways.IsHatch).SelectMany(l => new[] { Picture(true, l), Picture(false, l) }).Distinct();

    /// <summary>
    /// Every picture of a cell part way through a slide, each once: its name, the cell's own shut and open pictures, those
    /// of the cell whose panel slides over it and of the one after (null past the doorway's end), how far into the first
    /// the panel has slid, and whether it is a hatch's. What the ship's atlas makes with <see cref="Slid"/>; doorways
    /// longer than five cells only repeat their middles.
    /// </summary>
    public static IEnumerable<SlidPicture> SlidPictures =>
        new[] { false, true }.SelectMany(hatch => Enumerable.Range(1, 5).SelectMany(length => Enumerable.Range(0, length).SelectMany(index =>
            Enumerable.Range(1, length * Cell / Stride - 1).Select(step => Spec(hatch, index, length, step * Stride)))))
        .OfType<SlidPicture>().DistinctBy(p => p.Name);

    /// <summary>A picture part way through a slide, as <see cref="SlidPictures"/> lists it.</summary>
    public sealed record SlidPicture(string Name, string Closed, string Open, string OverClosed, string OverOpen,
        string? NextClosed, string? NextOpen, int Shift, bool Hatch);

    /// <summary>
    /// A cell part way through a slide (RGBA, <paramref name="width"/> pixels a row): its open picture, but where its shut and
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

    private const int Cell = Camera.Cell, None = -1;

    /// <summary>The piece cell <paramref name="index"/> of a doorway <paramref name="length"/> long is, by its joins: a door cell alone is a hatch.</summary>
    private static int PieceAt(bool hatch, int index, int length)
    {
        var (back, ahead) = hatch ? (Doorways.Left, Doorways.Right) : (Doorways.Up, Doorways.Down);
        return (index > 0 ? back : 0) | (index < length - 1 ? ahead : 0);
    }

    private static SlidPicture? Spec(bool hatch, int index, int length, int shift)
    {
        int over = index + shift / Cell;
        if (over >= length || (!hatch && length == 1)) return null;
        int piece = PieceAt(hatch, index, length), overPiece = PieceAt(hatch, over, length);
        int next = over + 1 < length ? PieceAt(hatch, over + 1, length) : None;
        return new SlidPicture(SlidName(hatch, piece, overPiece, next, shift % Cell), Name(hatch, "closed", piece), Name(hatch, "open", piece),
            Name(hatch, "closed", overPiece), Name(hatch, "open", overPiece),
            next == None ? null : Name(hatch, "closed", next), next == None ? null : Name(hatch, "open", next), shift % Cell, hatch);
    }

    private static string Name(bool hatch, string look, int piece) => $"{(hatch ? "hatch" : "door")}_{look}_{piece}";

    private static string SlidName(bool hatch, int piece, int over, int next, int shift) =>
        $"{(hatch ? "hatch" : "door")}_slid_{piece}_{over}_{(next == None ? "x" : next)}_{shift}";

    // Made once, not a string a cell: a big ship draws half a million cells as it loads.
    private static readonly string[] Closed = Names("closed"), Open = Names("open");

    private static string[] Names(string look) => Enumerable.Range(0, 16)
        .Select(l => Doorways.IsHatch(l) ? $"hatch_{look}_{l & (Doorways.Left | Doorways.Right)}" : $"door_{look}_{l & (Doorways.Up | Doorways.Down)}")
        .ToArray();
}
