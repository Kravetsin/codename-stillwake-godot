namespace Stillwake.Game.Features.Flora;

/// <summary>
/// Which open faces of a planet's vein cells glint in a helmet's beam (2026-11-07-ore-pockets): one cell in
/// <see cref="CaveLooks.VeinGlintEvery"/>, by a hash of the planet's seed and the cell, the same on every frame, chunk and peer;
/// and where on an open face of a block of stone a glint sits, a vein's cell or a rock growth's block alike.
/// </summary>
public static class VeinGlints
{
    /// <summary>Whether vein cell <paramref name="cell"/> of the planet grown from <paramref name="seed"/> glints.</summary>
    public static bool Picks(uint seed, int cell) => Hash(seed, cell) % CaveLooks.VeinGlintEvery == 0;

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

    /// <summary>
    /// A point a pixel or two inside one of the <paramref name="open"/> faces of the stone block <paramref name="across"/> by
    /// <paramref name="tall"/> cells of <paramref name="size"/> pixels at (<paramref name="x"/>, <paramref name="y"/>), in pixels of
    /// the world, the face and the place along it by <paramref name="hash"/>: two pixels or more from either end of the face, at a
    /// pixel's middle. None with no face open.
    /// </summary>
    public static (float X, float Y)? FacePoint(int x, int y, int across, int tall, Planting.Faces open, uint hash, int size)
    {
        System.Span<int> faces = stackalloc int[4];
        int n = 0;
        if ((open & Planting.Faces.Up) != 0) faces[n++] = 0;
        if ((open & Planting.Faces.Down) != 0) faces[n++] = 1;
        if ((open & Planting.Faces.Left) != 0) faces[n++] = 2;
        if ((open & Planting.Faces.Right) != 0) faces[n++] = 3;
        if (n == 0) return null;
        // Along a face, two pixels from either end: a growth's block 16 pixels, a vein's cell 8.
        int face = faces[(int)(hash % (uint)n)], length = (face < 2 ? across : tall) * size;
        int along = 2 + (int)(hash / 4 % (uint)(length - 4)), inward = 1 + (int)(hash / 64 % 2);
        var (px, py) = face switch
        {
            0 => (x * size + along, y * size + inward),
            1 => (x * size + along, (y + tall) * size - 1 - inward),
            2 => (x * size + inward, y * size + along),
            _ => ((x + across) * size - 1 - inward, y * size + along),
        };
        return (px + 0.5f, py + 0.5f);
    }
}
