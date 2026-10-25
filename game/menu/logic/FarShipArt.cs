using System;

namespace Stillwake.Game.Menu;

/// <summary>What a pixel of the far ship is: its hull, its top edge, its underside lit by the planet, a window, the dark inside it.</summary>
public enum ShipPixel : byte { None, Hull, Edge, Under, Window, Dark }

/// <summary>
/// The ship the title's hero came from, far off and broken in two (2026-10-25-title-scene), drawn pixel by pixel: the fore
/// half with its bridge, a mast and a row of windows, a few still lit; the aft half with the engine block and its nozzles;
/// each torn end ragged, the dark inside showing. Two pictures, drawn apart; the same for a seed every time.
/// </summary>
/// <param name="Fore">The fore half, row by row, <see cref="ForeWide"/> wide and <see cref="High"/> high.</param>
/// <param name="Aft">The aft half, row by row, <see cref="AftWide"/> wide and <see cref="High"/> high.</param>
public sealed record FarShipArt(ShipPixel[] Fore, ShipPixel[] Aft)
{
    public const int ForeWide = 70, AftWide = 56, High = 23;

    /// <summary>The fore half's pixel at <paramref name="x"/>, <paramref name="y"/>; none outside.</summary>
    public ShipPixel ForeAt(int x, int y) => x < 0 || y < 0 || x >= ForeWide || y >= High ? ShipPixel.None : Fore[y * ForeWide + x];

    /// <summary>The aft half's pixel at <paramref name="x"/>, <paramref name="y"/>; none outside.</summary>
    public ShipPixel AftAt(int x, int y) => x < 0 || y < 0 || x >= AftWide || y >= High ? ShipPixel.None : Aft[y * AftWide + x];

    public static FarShipArt Of(int seed)
    {
        var rng = new Random(seed);
        // The hull's band, its top lit, its underside catching the planet's light.
        const int top = 11, bottom = 21;
        var fore = new ShipPixel[ForeWide * High];
        void Fore(int x, int y, ShipPixel p)
        {
            if (x >= 0 && y >= 0 && x < ForeWide && y < High) fore[y * ForeWide + x] = p;
        }
        var torn = new int[High];
        for (int y = 0; y < High; y++) torn[y] = rng.Next(0, 4);
        for (int x = 0; x < ForeWide; x++)
            for (int y = top; y <= bottom; y++)
            {
                // The bow tapers; the torn end at the right is ragged, the dark inside showing a pixel in.
                if (x < (bottom - y) / 2 || x < (y - top) - 4) continue;
                int end = ForeWide - 1 - torn[y];
                if (x > end) continue;
                Fore(x, y, x == end ? ShipPixel.Dark : y == top ? ShipPixel.Edge : y == bottom ? ShipPixel.Under : ShipPixel.Hull);
            }
        // The bridge on the fore half, its windows lit, and the mast over it.
        for (int x = 10; x < 26; x++)
            for (int y = 5; y < top; y++) Fore(x, y, y == 5 ? ShipPixel.Edge : ShipPixel.Hull);
        for (int x = 12; x < 24; x += 3) Fore(x, 7, ShipPixel.Window);
        for (int y = 0; y < 5; y++) Fore(16, y, ShipPixel.Hull);
        for (int y = 2; y < 5; y++) Fore(21, y, ShipPixel.Hull);
        Fore(15, 1, ShipPixel.Hull);
        Fore(17, 1, ShipPixel.Hull);
        // A row of windows along it, a few still lit.
        foreach (int row in new[] { 14, 17 })
            for (int x = 6 + row % 2; x < ForeWide - 6; x += 3)
                if (rng.NextDouble() < 0.35) Fore(x, row, ShipPixel.Window);

        var aft = new ShipPixel[AftWide * High];
        void Aft(int x, int y, ShipPixel p)
        {
            if (x >= 0 && y >= 0 && x < AftWide && y < High) aft[y * AftWide + x] = p;
        }
        for (int y = 0; y < High; y++) torn[y] = rng.Next(0, 4);
        for (int x = 0; x < AftWide - 6; x++)
            for (int y = top; y <= bottom; y++)
            {
                if (x < torn[y]) continue;
                Aft(x, y, x == torn[y] ? ShipPixel.Dark : y == top ? ShipPixel.Edge : y == bottom ? ShipPixel.Under : ShipPixel.Hull);
            }
        foreach (int row in new[] { 14, 17 })
            for (int x = 6 + row % 2; x < AftWide - 10; x += 4)
                if (rng.NextDouble() < 0.2) Aft(x, row, ShipPixel.Window);
        // The engine block at the stern, taller than the hull, and its nozzles, cold.
        for (int x = AftWide - 9; x < AftWide - 2; x++)
            for (int y = top - 3; y <= bottom + 1; y++) Aft(x, y, y == top - 3 ? ShipPixel.Edge : y == bottom + 1 ? ShipPixel.Under : ShipPixel.Hull);
        foreach (int y in new[] { top, top + 4, bottom - 2 })
            for (int i = 0; i < 2; i++)
                for (int h = 0; h < 2; h++) Aft(AftWide - 2 + i, y + h, ShipPixel.Dark);
        return new FarShipArt(fore, aft);
    }
}
