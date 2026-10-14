using System;
using Stillwake.Sim.Crew;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The suit colours (features/crew/suits.ts), one for each of <c>Content.Crew.Colours</c>: far apart, and
/// none red, which the screen keeps for danger and refusals. Colours are 0xRRGGBB.
/// </summary>
public static class Suits
{
    public static readonly int[] All = [0xe8b04a, 0x5fc8c0, 0xa08cf0, 0x8cd06a];

    /// <summary>The suit <c>art/hero_*.png</c> are drawn in, which every suit is recoloured from: an older first suit.</summary>
    public const int Drawn = 0xe0a030;

    /// <summary>The suit colour hero <paramref name="id"/> wears, as the sim says; the first while the sim does not know it.</summary>
    public static int Of(ICrewQueries crew, int id) =>
        crew.HeroOf(id)?.Colour is int i && i >= 0 && i < All.Length ? All[i] : All[0];

    /// <summary><paramref name="colour"/> with each channel times <paramref name="by"/>, or moved toward white by −<paramref name="by"/> below 0 (<c>shade</c> of render/pixel-art.ts).</summary>
    public static int Shade(int colour, double by)
    {
        int Channel(int shift)
        {
            int v = (colour >> shift) & 0xff;
            return (int)Math.Round(by >= 0 ? v * by : v + (255 - v) * -by, MidpointRounding.AwayFromZero);
        }
        return (Channel(16) << 16) | (Channel(8) << 8) | Channel(0);
    }

    /// <summary>A suit's own shades of <paramref name="colour"/>, as the pictures use them: light, its own, shade and outline (<c>suitOf</c> of art.ts).</summary>
    public static int[] ShadesOf(int colour) => [Shade(colour, -0.35), colour, Shade(colour, 0.72), Shade(colour, 0.42)];
}
