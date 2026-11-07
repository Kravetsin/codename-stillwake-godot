namespace Stillwake.Game.Features.Flora;

/// <summary>
/// Which open faces of a planet's vein cells glint in a helmet's beam (2026-11-07-ore-pockets): one cell in
/// <see cref="CaveLooks.VeinGlintEvery"/>, by a hash of the planet's seed and the cell, the same on every frame, chunk and peer.
/// </summary>
public static class VeinGlints
{
    /// <summary>Whether vein cell <paramref name="cell"/> of the planet grown from <paramref name="seed"/> glints, one in <paramref name="every"/>.</summary>
    public static bool Picks(uint seed, int cell, int every = CaveLooks.VeinGlintEvery) => every > 0 && Hash(seed, cell) % (uint)every == 0;

    /// <summary>A hash of <paramref name="seed"/> and <paramref name="cell"/>: what picks a glinting cell, its high bits where on its face.</summary>
    public static uint Hash(uint seed, int cell)
    {
        unchecked
        {
            uint h = seed * 0x9E3779B1u ^ (uint)cell * 0x85EBCA77u;
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            return h ^ (h >> 15);
        }
    }
}
