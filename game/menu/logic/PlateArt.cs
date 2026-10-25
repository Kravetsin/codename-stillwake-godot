using System;

namespace Stillwake.Game.Menu;

/// <summary>What a pixel of the title's plate is: its shades, a rivet, a seam, the frame inside it, a hanging wire.</summary>
public enum PlatePixel : byte { None, Shadow, Body, Lit, Rivet, Seam, Rib, Frame, Wire }

/// <summary>
/// The title's plate drawn pixel by pixel (2026-10-25-title-scene): a chunk of hull torn off a ship, a few strides long —
/// the outer skin on top, lit, with seams and rivets; under it the frame, a truss of ribs and struts in the dark; the inner
/// skin under that, torn away in stretches, wires hanging out of the tears. Bent down at its left, where it was wrenched
/// off, its ends ragged, a hole punched through it; a broken rib standing up out of it. Its right end is whole, level and
/// square: the hero sits there. Drawn from a seed, the same each time.
/// </summary>
/// <param name="Width">Its width in pixels.</param>
/// <param name="Height">Its height in pixels.</param>
/// <param name="Pixels">Each pixel, row by row.</param>
/// <param name="Seat">The top of its right end, where the hero's hips rest, the edge just to its right.</param>
/// <param name="Lamp">Where the emergency lamp sits on its top.</param>
/// <param name="Anchor">Where the hero's tether is made fast, on its top.</param>
public sealed record PlateArt(int Width, int Height, PlatePixel[] Pixels, (int X, int Y) Seat, (int X, int Y) Lamp, (int X, int Y) Anchor)
{
    /// <summary>The plate's length and thickness, the rib's height, how far the bend drops its left end, in pixels.</summary>
    public const int Long = 140, Thick = 15, RibHigh = 24, Bend = 12;

    /// <summary>The pixel at <paramref name="x"/>, <paramref name="y"/>; none outside.</summary>
    public PlatePixel At(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height ? PlatePixel.None : Pixels[y * Width + x];

    /// <summary>The plate of <paramref name="seed"/>.</summary>
    public static PlateArt Of(int seed)
    {
        var rng = new Random(seed);
        int width = Long, height = RibHigh + Bend + Thick + 12;
        var pixels = new PlatePixel[width * height];
        void Set(int x, int y, PlatePixel p)
        {
            if (x >= 0 && y >= 0 && x < width && y < height) pixels[y * width + x] = p;
        }
        PlatePixel Get(int x, int y) => x < 0 || y < 0 || x >= width || y >= height ? PlatePixel.None : pixels[y * width + x];
        int baseY = RibHigh + 2;
        // The top along the plate: level at the right, bent down towards the torn left end.
        int TopAt(int x) => baseY + (x < 44 ? (int)Math.Round(Math.Pow((44 - x) / 44.0, 2) * Bend) : 0);
        // The ragged left end, each row stopping at its own column; the right end square.
        var leftEnd = new int[Thick];
        for (int r = 0; r < Thick; r++) leftEnd[r] = rng.Next(0, 5) + r / 3 + (r % 4 == 1 ? 3 : 0);
        // The inner skin torn away in stretches: where it is, its depth below the frame.
        var torn = new bool[width];
        for (int x = 0; x < width - 12;)
        {
            int run = 6 + rng.Next(0, 14);
            bool gone = rng.NextDouble() < 0.45;
            for (int i = x; i < Math.Min(width - 12, x + run); i++) torn[i] = gone;
            x += run;
        }
        int holeX = 58 + rng.Next(0, 8), holeW = 6 + rng.Next(0, 3);
        for (int x = 0; x < width; x++)
        {
            int top = TopAt(x);
            for (int r = 0; r < Thick; r++)
            {
                if (x < leftEnd[r]) continue;
                bool hole = x >= holeX && x < holeX + holeW && r < 4 && Math.Abs(2 * (x - holeX) - holeW + 1) < holeW - (r == 0 ? 2 : 0);
                if (hole) continue;
                // The outer skin: lit top, body, a dark line where it meets the frame.
                PlatePixel p = r switch
                {
                    0 => PlatePixel.Lit,
                    1 or 2 => PlatePixel.Body,
                    3 => PlatePixel.Seam,
                    >= 4 and <= 10 => Truss(x, r - 4),
                    11 or 12 => torn[x] ? PlatePixel.None : PlatePixel.Body,
                    _ => torn[x] ? PlatePixel.None : PlatePixel.Shadow,
                };
                if (p != PlatePixel.None) Set(x, top + r, p);
            }
            if (x % 34 == 22) for (int r = 1; r < 3; r++) Set(x, top + r, PlatePixel.Seam);
            if (x % 5 == 2 && x > leftEnd[1] + 1) Set(x, top + 1, PlatePixel.Rivet);
        }
        // Wires hanging out of the tears in the inner skin, a pixel wide, of their own lengths.
        for (int x = 4; x < width - 14; x += 3 + rng.Next(0, 6))
        {
            if (!torn[x] || rng.NextDouble() < 0.4) continue;
            int from = TopAt(x) + 11, length = 2 + rng.Next(0, 8);
            for (int i = 0; i < length; i++) Set(x + (i > 4 && rng.NextDouble() < 0.3 ? 1 : 0), from + i, PlatePixel.Wire);
        }
        // The rib: a girder standing out of the plate's left third, leaning, broken off at its top, holed.
        int ribX = 30, ribTop = 2 + rng.Next(0, 3);
        for (int y = ribTop; y < TopAt(ribX); y++)
        {
            int lean = (TopAt(ribX) - y) / 3;
            for (int w = 0; w < 4; w++)
            {
                if (y - ribTop < 3 && (w + y) % 3 == 0) continue;
                bool holed = w is 1 or 2 && (y - ribTop) % 7 is 3 or 4;
                if (!holed && Get(ribX - lean + w, y) == PlatePixel.None) Set(ribX - lean + w, y, w == 0 ? PlatePixel.Lit : PlatePixel.Rib);
            }
        }
        int seatX = width - 4;
        return new PlateArt(width, height, pixels, (seatX, TopAt(seatX)), (86, TopAt(86)), (110, TopAt(110)));
    }

    /// <summary>The frame between the skins, <paramref name="row"/> down it: ribs across every ten pixels, struts slanting between them.</summary>
    private static PlatePixel Truss(int x, int row)
    {
        int along = x % 10;
        if (along is 0 or 1) return PlatePixel.Rib;
        // A strut from one rib's foot to the next one's head, the next bay the other way.
        int slant = (x / 10) % 2 == 0 ? along - 1 : 9 - along;
        return Math.Abs(slant * 6 / 8 - row) <= 0 ? PlatePixel.Rib : PlatePixel.Frame;
    }
}
