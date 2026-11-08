using System;
using Stillwake.Content;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// Which depth band's stone a planet's cell is drawn in (2026-11-08-planet-layers): the sim's band of its row (<see cref="Terrain.BandOf"/>),
/// the boundary wobbling a few cells along the columns so it reads as rock, not a ruled line; plain stone above
/// <paramref name="from"/> rows under the surface's middle, so the land under the sky keeps today's grey.
/// </summary>
public sealed class Strata(PlanetType type, int from)
{
    /// <summary>Drawn in plain stone: above the bands' own stone.</summary>
    public const int Plain = -1;

    /// <summary>How many cells a boundary wobbles either way at most.</summary>
    public const int Reach = 3;

    /// <summary>How many bands the type has: one more than its boundaries.</summary>
    public int Bands => type.Bands.Count + 1;

    /// <summary>The band cell (<paramref name="x"/>, <paramref name="y"/>) is drawn in, or <see cref="Plain"/>.</summary>
    public int BandOf(int x, int y)
    {
        int row = y + Wobble(x);
        return row < type.Ground.Surface + from ? Plain : Terrain.BandOf(type, row);
    }

    /// <summary>The boundaries' shift at column <paramref name="x"/>, within <see cref="Reach"/>: two waves, so it never repeats in view.</summary>
    public static int Wobble(int x) => Math.Clamp((int)Math.Round(1.8 * Math.Sin(x * 0.13) + 1.1 * Math.Sin(x * 0.41 + 1.3)), -Reach, Reach);

    /// <summary>Whether <paramref name="material"/> is drawn by band: stone and the veins run through it.</summary>
    public static bool Banded(Material material) => material.Id == Stone || material.In == Stone;

    private const string Stone = "stone";
}
