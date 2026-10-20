using System;

namespace Stillwake.Game.Render;

/// <summary>
/// The pictures laid over a planet's ground (0081), a rule of drawing, never the sim's: a pebble, a root or a crack in about
/// <see cref="SinglePercent"/> of its whole cells, and now and then a boulder or a vein over several. A feature lies inside a
/// block of <see cref="Block"/> cells square, so what a cell shows hangs on its own block only, and only where every cell it
/// covers is a whole wall of one natural material. A hash of where it is: the same on every machine and run.
/// </summary>
public static class GroundDetails
{
    /// <summary>The pictures one cell each, the first row of <c>art/planet/detail_{material}</c>.</summary>
    public const int Singles = 8;

    /// <summary>The features over several cells, their sides in cells, along the sheet's second row: two of 2×2, one of 3×3.</summary>
    public static readonly int[] Features = [2, 2, 3];

    /// <summary>The blocks a feature lies in, in cells square: at most one in each.</summary>
    public const int Block = 4;

    /// <summary>The share of whole cells with a picture of their own, and of blocks with a feature of 3×3, then of 2×2.</summary>
    public const int SinglePercent = 40, BigPercent = 7, SmallPercent = 16;

    /// <summary>What a cell shows: <see cref="Picture"/> -1 for nothing, below <see cref="Singles"/> a single, else feature
    /// <c>Picture - Singles</c>, of which the piece (<see cref="Dx"/>, <see cref="Dy"/>) from its corner.</summary>
    public readonly record struct Detail(int Picture, int Dx, int Dy)
    {
        public static readonly Detail None = new(-1, 0, 0);
    }

    /// <summary>
    /// What cell (<paramref name="x"/>, <paramref name="y"/>) shows, <paramref name="ground"/> answering the material of a whole
    /// wall of natural ground at a cell, -1 for any other cell or one past the grid's edge.
    /// </summary>
    public static Detail At(int x, int y, Func<int, int, int> ground)
    {
        int material = ground(x, y);
        if (material < 0) return Detail.None;
        int bx = x / Block, by = y / Block;
        uint h = Hash(bx, by, 0x51u);
        int roll = (int)(h % 100), feature = roll < BigPercent ? 2 : roll < BigPercent + SmallPercent ? (int)((h >> 7) & 1) : -1;
        if (feature >= 0)
        {
            int size = Features[feature], room = Block - size + 1;
            int fx = bx * Block + (int)((h >> 9) % (uint)room), fy = by * Block + (int)((h >> 13) % (uint)room);
            if (x >= fx && x < fx + size && y >= fy && y < fy + size && Fits(fx, fy, size, material, ground))
                return new Detail(Singles + feature, x - fx, y - fy);
        }
        uint s = Hash(x, y, 0xD7u);
        return s % 100 < SinglePercent ? new Detail((int)((s >> 8) % Singles), 0, 0) : Detail.None;
    }

    private static bool Fits(int fx, int fy, int size, int material, Func<int, int, int> ground)
    {
        for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
                if (ground(fx + i, fy + j) != material) return false;
        return true;
    }

    private static uint Hash(int x, int y, uint salt)
    {
        unchecked
        {
            uint h = (uint)x * 0x85EBCA77u ^ (uint)y * 0xC2B2AE3Du ^ salt * 0x27D4EB2Fu;
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            return h ^ (h >> 15);
        }
    }
}
