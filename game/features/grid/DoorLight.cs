using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// A doorway's slide in light (0075), one doorway at a time, from its leading cell: while the panel moves, a thin line of
/// light along its leading edge, as if it caught the lamp, brightest mid-slide; opened, a few fine motes shaken loose
/// from the slot drift down; shut, a faint flat ring along the floor, a small glint at the seal, a little dust sideways
/// and a hiss of vapour from the seal, a pressure door's. A door's panel rises into its top, a hatch's slides into its
/// left end. In pixels of the world; <c>corner</c> is the doorway's top-left, <c>length</c> its cells.
/// </summary>
public static class DoorLight
{
    private const int Cell = ShipCamera.Cell;
    private static readonly (int, int, int) Edge = (255, 236, 205), Warm = (255, 200, 140), Seal = (255, 230, 190), Floor = (240, 236, 226);

    /// <summary>The ease a panel slides by: slow off, quick through the middle, slow in.</summary>
    public static double Ease(double t) => t * t * (3 - 2 * t);

    /// <summary>
    /// The panel part way, <paramref name="open"/> of the way open (eased), <paramref name="through"/> of its slide done:
    /// its leading edge lit across the doorway, brightest halfway.
    /// </summary>
    public static void Sliding(Vector2 corner, int length, bool hatch, double open, double through)
    {
        double lit = Math.Sin(Math.PI * Math.Clamp(through, 0, 1));
        if (lit < 0.02) return;
        float reach = (float)(length * Cell * (1 - open));
        // Across the doorway at its leading edge, from the middle out each way, clear at the ends.
        var (middle, along) = hatch
            ? (corner + new Vector2(reach - 0.4f, Cell / 2f), new Vector2(0, Cell / 2f))
            : (corner + new Vector2(Cell / 2f, reach - 0.4f), new Vector2(Cell / 2f, 0));
        foreach (int side in (ReadOnlySpan<int>)[-1, 1])
        {
            var end = middle + along * side;
            Flares.Shared.Trail(middle.X, middle.Y, end.X, end.Y, Edge, 0.3, 0.75 * lit, 0.27);
        }
        Flares.Shared.Bloom(middle.X, middle.Y, 10, Warm, 0.25 * lit);
    }

    /// <summary>The panel come to rest, <paramref name="open"/> or shut: the slot's motes, or the seal's ring, glint, dust and hiss.</summary>
    public static void Rested(Vector2 corner, int length, bool hatch, bool open)
    {
        var glims = Glims.Shared;
        float span = length * Cell;
        if (open)
        {
            // Shaken loose from the slot the panel went into: a door's top, a hatch's left end.
            for (int i = 0; i < Math.Round(7 * Fx.Count); i++)
            {
                var at = hatch ? corner + new Vector2((float)glims.Between(0.5, 2), (float)glims.Between(2, 14)) : corner + new Vector2((float)glims.Between(2, 14), (float)glims.Between(0.5, 2));
                glims.Add(Luminous.SlotDust, at.X, at.Y, glims.Between(-3, 3), glims.Between(2, 8), hatch ? corner.Y + Cell : corner.Y + span,
                    size: glims.Between(0.4, 0.6));
            }
            var slot = hatch ? corner + new Vector2(0.8f, Cell / 2f) : corner + new Vector2(Cell / 2f, 0.8f);
            Flares.Shared.Glint(slot.X, slot.Y, 5, Seal);
            return;
        }
        // Shut: where the panel meets the frame, a door's floor, a hatch's right end.
        var seal = hatch ? corner + new Vector2(span - 0.4f, Cell / 2f) : corner + new Vector2(Cell / 2f, span - 0.4f);
        Flares.Shared.Glint(seal.X, seal.Y, 5, Seal);
        if (!hatch) Flares.Shared.Shock(seal.X, seal.Y + 0.1, 4, 4 + 18 * Fx.Length, 11 * Glim.SampleMs, Floor, 0.32 * Fx.Glow, 0.26, 0.12);
        foreach (int side in (ReadOnlySpan<int>)[-1, 1])
        {
            for (int i = 0; i < Math.Round(4 * Fx.Count); i++)
            {
                if (hatch) glims.Add(Luminous.SealDust, seal.X, seal.Y + side * glims.Between(5, 8), glims.Between(-6, 6), side * glims.Between(4, 12), size: glims.Between(0.4, 0.6));
                else glims.Add(Luminous.SealDust, seal.X + side * glims.Between(5, 8), seal.Y - 0.2, side * glims.Between(8, 24), -glims.Between(1, 6), seal.Y + 0.4,
                    size: glims.Between(0.4, 0.6));
            }
            for (int i = 0; i < 3; i++)
            {
                if (hatch) glims.Add(Luminous.SealHiss, seal.X + glims.Between(-2, 0), seal.Y + side * glims.Between(6, 9), glims.Between(-7, 2), side * glims.Between(14, 32));
                else glims.Add(Luminous.SealHiss, seal.X + side * glims.Between(6, 9), seal.Y - glims.Between(0, 2), side * glims.Between(14, 32), -glims.Between(2, 7));
            }
        }
    }
}
