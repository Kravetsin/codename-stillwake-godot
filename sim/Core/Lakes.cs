using Stillwake.Content;

namespace Stillwake.Sim;

public static partial class Terrain
{
    /// <summary>A lake's lattice point across and down, whether it holds one, and its cap: hashed apart from the ground's own.</summary>
    private const uint LakeAcross = 200, LakeDown = 201, LakeRoll = 202, LakeSize = 203;

    /// <summary>
    /// The lakes (docs/plans/liquids.md), grown last: a point every <see cref="PlanetType.LakeWave"/> cells across and down, moved
    /// within its square by the seed, visited row by row from the top, each row from the left. One that lies open in a
    /// <see cref="PlanetType.LakeBands"/> band and passes <see cref="PlanetType.LakeChance"/> drops straight down to the floor
    /// under it, and the lake rises from there a row at a time: at level L it is every open cell joined to the floor through
    /// rows at L or under, and it rises while the next row's region holds at most its cap, all in the zone, none of it sky, a
    /// ruin's box or an earlier lake. Every cell of it is full, so it lies still: a full cell's open neighbours below and
    /// beside are in it too.
    /// </summary>
    /// <returns>A byte a cell (<see cref="Liquids.Of"/>), or null where none grew.</returns>
    private static byte[]? Lakes(
        PlanetType t, uint seed, char[][] rows, Box zone, List<(int X, int Y)> spires, List<(int X, int Y, Ruins.Home Home)> homes)
    {
        int wave = t.LakeWave;
        if (wave <= 0 || t.LakeChance <= 0) return null;
        int w = t.Ground.Width, h = t.Ground.Height;
        var ruins = new List<Box>();
        // A cell round each ruin: its walls stay dry on both faces.
        foreach (var (x, y) in spires) ruins.Add(new Box(x - 1, y - 1, x + Ruins.SpireWide, y + Ruins.Spire.Count));
        foreach (var (x, y, home) in homes) ruins.Add(new Box(x - 1, y - 1, x + home.Wide, y + home.Tall));
        byte[]? water = null;
        var seen = new HashSet<int>();
        var lake = new List<int>();
        for (int j = 0; j * wave < h; j++)
            for (int i = 0; i * wave < w; i++)
            {
                int x = i * wave + (int)((long)Lattice(seed, LakeAcross, i, j) * wave / Whole);
                int y = j * wave + (int)((long)Lattice(seed, LakeDown, i, j) * wave / Whole);
                if (x >= w || y >= h || rows[y][x] != '.' || !t.LakeBands.Contains(BandOf(t, y))) continue;
                if (Lattice(seed, LakeRoll, i, j) >= (long)t.LakeChance * Whole / 1000) continue;
                while (y + 1 < h && rows[y + 1][x] == '.') y++;
                if (y + 1 >= h || rows[y + 1][x] != '#' || water != null && water[y * w + x] != Liquids.None) continue;
                int cap = Lattice(seed, LakeSize, i, j) < (long)t.BigChance * Whole / 1000 ? t.LakeBig : t.LakeMedium;
                seen.Clear();
                lake.Clear();
                bool Takes(int cell) =>
                    rows[cell / w][cell % w] == '.' && zone.Contains(cell % w, cell / w) && (water == null || water[cell] == Liquids.None)
                    && !ruins.Any(b => b.Contains(cell % w, cell / w));
                // Joins to the lake every open cell reached from its cells from `from` on through rows at `level` or under;
                // false once it is past the cap or meets a cell no lake may take.
                bool Spread(int from, int level)
                {
                    for (int k = from; k < lake.Count; k++)
                    {
                        int cell = lake[k], cx = cell % w, cy = cell / w;
                        foreach (int n in (ReadOnlySpan<int>)[cx > 0 ? cell - 1 : -1, cx + 1 < w ? cell + 1 : -1, cy + 1 < h ? cell + w : -1, cy > level ? cell - w : -1])
                        {
                            if (n < 0 || rows[n / w][n % w] == '#' || !seen.Add(n)) continue;
                            if (!Takes(n)) return false;
                            lake.Add(n);
                            if (lake.Count > cap) return false;
                        }
                    }
                    return true;
                }
                int floor = y * w + x;
                if (!Takes(floor)) continue;
                seen.Add(floor);
                lake.Add(floor);
                if (!Spread(0, y)) continue;
                for (int level = y; level > 0; level--)
                {
                    int kept = lake.Count;
                    // The row above joins where it lies over the lake's top row.
                    for (int k = 0; k < kept; k++)
                    {
                        int up = lake[k] - w;
                        if (lake[k] / w != level || rows[level - 1][up % w] == '#' || !seen.Add(up)) continue;
                        lake.Add(up);
                    }
                    bool ok = lake.Count <= cap;
                    for (int k = kept; ok && k < lake.Count; k++) ok = Takes(lake[k]);
                    if (ok && Spread(kept, level - 1)) continue;
                    for (int k = kept; k < lake.Count; k++) seen.Remove(lake[k]);
                    lake.RemoveRange(kept, lake.Count - kept);
                    break;
                }
                water ??= new byte[w * h];
                foreach (int cell in lake) water[cell] = Liquids.Of(Liquids.Water, Liquids.Full);
            }
        return water;
    }
}
