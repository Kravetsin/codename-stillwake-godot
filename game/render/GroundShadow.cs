using System;

namespace Stillwake.Game.Render;

/// <summary>
/// The soft shadow walls cast on a natural back wall along their open edges (0088), a rule of drawing: the walls' pixels round a
/// cell blurred twice by a box (<see cref="GroundLooks.ShadowReach"/>), strengthened (<see cref="GroundLooks.ShadowGain"/>) and
/// stepped into <see cref="GroundLooks.ShadowSteps"/> tones. It reaches less than a cell, so a cell's shadow hangs on the walls of
/// its own cell and its eight neighbours only. Plain C#, so it is tested.
/// </summary>
public static class GroundShadow
{
    private const int Cell = Camera.Cell, Side = 3 * Cell;

    /// <summary>
    /// The shadow over a cell's pixels, row by row, from 0 (none) to <see cref="GroundLooks.ShadowSteps"/> (darkest);
    /// <paramref name="walls"/> holds whether a wall stands in each cell of the three by three round it, row by row. Null where
    /// no pixel is shadowed, or the cell's own wall covers it.
    /// </summary>
    public static byte[]? Of(ReadOnlySpan<bool> walls)
    {
        if (walls[4]) return null;
        var solid = new float[Side * Side];
        bool any = false;
        for (int c = 0; c < 9; c++)
        {
            if (!walls[c]) continue;
            any = true;
            int x0 = c % 3 * Cell, y0 = c / 3 * Cell;
            for (int py = 0; py < Cell; py++)
                for (int px = 0; px < Cell; px++)
                    solid[(y0 + py) * Side + x0 + px] = 1;
        }
        if (!any) return null;
        var across = new float[Side * Side];
        for (int pass = 0; pass < 2; pass++)
        {
            Blur(solid, across, 1, Side);
            Blur(across, solid, Side, 1);
        }
        var shade = new byte[Cell * Cell];
        bool shadowed = false;
        for (int py = 0; py < Cell; py++)
            for (int px = 0; px < Cell; px++)
            {
                double v = Math.Min(1, solid[(Cell + py) * Side + Cell + px] * GroundLooks.ShadowGain);
                var step = (byte)Math.Floor(v * GroundLooks.ShadowSteps + 0.5);
                shade[py * Cell + px] = step;
                shadowed |= step > 0;
            }
        return shadowed ? shade : null;
    }

    /// <summary>A box blur of <paramref name="from"/> into <paramref name="to"/> along one way (<paramref name="step"/> apart), its ends held.</summary>
    private static void Blur(float[] from, float[] to, int step, int line)
    {
        int reach = GroundLooks.ShadowReach;
        for (int l = 0; l < Side; l++)
            for (int i = 0; i < Side; i++)
            {
                float sum = 0;
                for (int d = -reach; d <= reach; d++) sum += from[l * line + Math.Clamp(i + d, 0, Side - 1) * step];
                to[l * line + i * step] = sum / (2 * reach + 1);
            }
    }
}
