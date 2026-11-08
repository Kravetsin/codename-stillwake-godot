using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using static Stillwake.Game.BackdropLooks;

namespace Stillwake.Game.Features.Backdrop;

/// <summary>
/// A planet's land behind its ground, composed from its own plants and ground pictures by its surface's seed
/// (composed-backdrops): the backdrops page's generator (<c>surface.js</c>: <c>composeSurface</c>, <c>layerFrom</c>,
/// <c>compose</c>) one for one. Each layer is a list of draws in order over one tile of <see cref="Recipe.Tile"/> pixels,
/// repeated across: ridges of land filled with a ground picture, and pictures. Client only and cosmetic: integer ridges and a
/// seeded integer rng, so every peer composes the same land; a double may round a pixel differently on another machine.
/// </summary>
public static class BackdropPlan
{
    /// <summary>
    /// A ridge as the game builds ground: <see cref="N"/> columns <see cref="Cx"/> wide across the tile, each <see cref="Up"/>
    /// cells of <see cref="Cw"/> pixels over the layer's ground row, neighbours at most a cell apart, wrapping at the tile.
    /// </summary>
    public sealed class Ridge(IReadOnlyList<int> up, double cw, double cx, double groundRow)
    {
        public IReadOnlyList<int> Up { get; } = up;
        public int N => Up.Count;
        public double Cw { get; } = cw;
        public double Cx { get; } = cx;

        /// <summary>The top of column <paramref name="i"/>, any column, wrapping.</summary>
        public double Top(int i) => groundRow - Up[((i % N) + N) % N] * Cw;

        /// <summary>The top under <paramref name="x"/> pixels across the tile.</summary>
        public double At(double x) => Top((int)Math.Floor(x / Cx));
    }

    /// <summary>One thing a layer draws.</summary>
    public abstract record Draw;

    /// <summary>
    /// A ridge's land, from its columns' tops down past the layer's height, filled with picture <paramref name="Sheet"/> repeated at
    /// the layer's scale; then, where its alpha is above nothing, <paramref name="Wash"/> over it.
    /// </summary>
    public sealed record Fill(Ridge Ridge, string Sheet, (float R, float G, float B, float A) Wash) : Draw;

    /// <summary>A picture, its top left at (<paramref name="X"/>, <paramref name="Y"/>), <paramref name="W"/> × <paramref name="H"/>, mirrored or not; repeated with the tile.</summary>
    public sealed record Picture(string Name, double X, double Y, double W, double H, bool Flip) : Draw;

    /// <summary>A layer: its recipe and what it draws, in order.</summary>
    public sealed record Layer(Depth Depth, IReadOnlyList<Draw> Draws);

    /// <summary>
    /// The land of a surface of <paramref name="type"/> grown from <paramref name="seed"/>: <see cref="Recipe.Depths"/> layers, far
    /// to near. <paramref name="size"/> gives a picture's size in the world's pixels, none where it has no picture;
    /// <paramref name="grass"/> is the grass's own green (<see cref="GrassOf"/>).
    /// </summary>
    public static IReadOnlyList<Layer> Compose(PlanetType type, uint seed, Func<string, (int W, int H)?> size, (int R, int G, int B) grass)
    {
        var recipe = First;
        var r = new Rng(unchecked(seed * 7919u + 17u));
        var roles = RolesOf(type, size, recipe);
        // The page placed its giant and sun by the first five draws; the sky keeps today's places, the land the page's draws.
        for (int i = 0; i < 5; i++) r.Next();
        var layers = new List<Layer>();
        foreach (var depth in recipe.Depths)
        {
            var (items, layerSeed) = ItemsOf(type, depth, roles, r, recipe);
            layers.Add(new Layer(depth, Lay(depth, items, layerSeed, roles, size, grass, recipe)));
        }
        return layers;
    }

    /// <summary>The grass's own green: the opaque green pixels of its tops' pictures (RGBA8), averaged; <see cref="Recipe.Grass"/> where there are none.</summary>
    public static (int R, int G, int B) GrassOf(IEnumerable<byte[]> pictures)
    {
        long r = 0, g = 0, b = 0, n = 0;
        foreach (var d in pictures)
            for (int i = 0; i + 3 < d.Length; i += 4)
                if (d[i + 3] > 200 && d[i + 1] > d[i] && d[i + 1] >= d[i + 2])
                {
                    r += d[i];
                    g += d[i + 1];
                    b += d[i + 2];
                    n++;
                }
        return n == 0 ? First.Grass : ((int)Round((double)r / n), (int)Round((double)g / n), (int)Round((double)b / n));
    }

    /// <summary>The pictures of the soil's grass tops whose green <see cref="GrassOf"/> reads.</summary>
    public static IEnumerable<string> GrassPictures(PlanetType type) =>
        Enumerable.Range(0, First.TopVariants).Select(v => $"top_{type.Soil}_{v}_front_0");

    // ---------- The planet's plants by role ----------

    private sealed record Kind(string[] Names, int Sink, double Width);

    private sealed record Roles(Kind[] Shrub, Kind[] Solid, Kind[] Big, double BigW, string[] Cover, string[] ShrubNames);

    /// <summary>A planet's plants by role, read from their numbers, never their names: trees tower, shrubs want room, solids little, cover none.</summary>
    private static Roles RolesOf(PlanetType type, Func<string, (int W, int H)?> size, Recipe recipe)
    {
        var kinds = new List<(Stillwake.Content.Flora.Kind Spec, Kind Kind)>();
        foreach (var spec in type.Plants)
        {
            var names = FloraLooks.Pictures(spec.Id).Where(n => size(n) != null).ToArray();
            if (names.Length == 0) continue;
            double width = names.Sum(n => (double)size(n)!.Value.W) / names.Length;
            kinds.Add((spec, new Kind(names, FloraLooks.Of[spec.Id].Sink, width)));
        }
        Kind[] Where(Func<Stillwake.Content.Flora.Kind, bool> role) => kinds.Where(k => role(k.Spec)).Select(k => k.Kind).ToArray();
        var tree = Where(k => k.Tall >= recipe.TreeTall);
        var shrub = Where(k => k.Tall < recipe.TreeTall && k.Room >= recipe.ShrubRoom);
        var solid = Where(k => k.Tall < recipe.TreeTall && k.Room > 0 && k.Room < recipe.ShrubRoom);
        // Cover as often as its kind's chance says, each of its pictures alike.
        var cover = kinds.Where(k => k.Spec.Room == 0)
            .SelectMany(k => k.Kind.Names.SelectMany(n => Enumerable.Repeat(n, Math.Max(1, (int)Round((double)k.Spec.Chance / recipe.CoverChance / k.Kind.Names.Length)))))
            .ToArray();
        var big = tree.Length > 0 ? tree : shrub.Length > 0 ? shrub : solid;
        return new Roles(shrub, solid, big, big.Length > 0 ? big.Max(k => k.Width) : recipe.BigWide, cover, shrub.SelectMany(k => k.Names).ToArray());
    }

    // ---------- A layer's items: ridges and the parts standing on them ----------

    private abstract record Item;

    private sealed record RidgeItem(List<(double X, double Y)> Points, string Sheet, string Material, bool Grassy) : Item;

    private sealed record Part(string Name, double X, double Scale, bool Flip, int Sink) : Item;

    /// <summary>One layer's items from the planet and the rng: a stone ridge where a range rises, the grassland before it, its plants, its shelves.</summary>
    private static (List<Item> Items, int Seed) ItemsOf(PlanetType type, Depth d, Roles roles, Rng r, Recipe recipe)
    {
        var g = type.Ground;
        double tile = recipe.Tile, cell = Camera.Cell * d.Scale, across = tile / cell;
        var nR = Noise(r, across, g.RegionWave);
        var nH = Noise(r, across, g.HillWave);
        var nB = Noise(r, across, g.BumpWave);
        var nRange = Noise(r, across, g.RangeWave);
        var nRange2 = Noise(r, across, g.RangeWave / 3.0);
        var nMask = Noise(r, tile, recipe.RangeWave);
        var nPat = Noise(r, tile, recipe.PatternWave);
        // A stony planet, its ranges rising far over its hills, gives its ranges more of the horizon.
        double bias = (double)g.RangeRise / (g.RangeRise + 4 * g.HillRise);
        double RangeW(double u) => d.Range * Ease(recipe.RangeFrom.A - recipe.RangeBias * bias, recipe.RangeFrom.B - recipe.RangeBias * bias, nMask(u));
        (double Forest, double Open, double Groves) Weights(double u)
        {
            double p = nPat(u), forest = d.Forest * Ease(recipe.ForestFrom.A, recipe.ForestFrom.B, p), open = 1 - Ease(recipe.OpenTo.A, recipe.OpenTo.B, p);
            return (forest, open, Math.Max(0, 1 - forest - open));
        }
        double SoilUp(double u) => d.Base + d.Region * g.RegionLift * nR(u) + d.Hill * g.HillRise * nH(u) + g.BumpRise * recipe.BumpShare * nB(u);
        double StoneUp(double u)
        {
            double raw = d.Rise * g.RangeRise * RangeW(u) * (recipe.RangeLeast.A + (1 - recipe.RangeLeast.A) * nRange(u))
                * (recipe.RangeLeast.B + (1 - recipe.RangeLeast.B) * nRange2(u));
            return SoilUp(u) * recipe.RangeSoil + Math.Floor(raw / g.TerraceStep) * g.TerraceStep + g.BumpRise * nB(u);
        }
        List<(double, double)> Points(Func<double, double> up)
        {
            var points = new List<(double, double)>();
            for (int c = 0; c < across; c += 2) points.Add((c * cell, up(c / across) * cell));
            return points;
        }
        string Sheet(string material, bool wall) => $"{(wall ? "wall" : "back")}_{material}";

        var items = new List<Item>();
        if (d.Range > 0) items.Add(new RidgeItem(Points(StoneUp), Sheet(type.Stone, d.Wall), type.Stone, false));
        items.Add(new RidgeItem(Points(SoilUp), Sheet(type.Soil, d.Wall), type.Soil, true));
        Part Pick(Kind[] kinds, double x, double scale)
        {
            var k = kinds[(int)Math.Floor(r.Next() * kinds.Length)];
            return new Part(k.Names[(int)Math.Floor(r.Next() * k.Names.Length)], x, scale, r.Next() < 0.5, k.Sink);
        }
        List<Item> back = [], front = [], low = [];
        double w0 = roles.BigW * d.Scale;
        void AtFeet(double x, int n)
        {
            for (int i = 0; i < n && roles.Shrub.Length > 0; i++)
                low.Add(Pick(roles.Shrub, x + (r.Next() - 0.5) * w0 * recipe.FeetSpread, Between(r, recipe.AtFeet)));
        }
        if (roles.Big.Length > 0)
            for (double x = r.Next() * w0; x < tile;)
            {
                double u = x / tile, rw = RangeW(u), sparse = 1 - recipe.RangeSparse * rw;
                var w = Weights(u);
                double t = r.Next() * (w.Forest + w.Open + w.Groves);
                if (t < w.Forest)
                {
                    var f = recipe.Forest;
                    back.Add(Pick(roles.Big, x + w0 * f.Shift, Between(r, f.Back)));
                    front.Add(Pick(roles.Big, x, Between(r, f.Front)));
                    if (r.Next() < f.Feet) AtFeet(x, 1);
                    x += w0 * Between(r, f.Step) / Math.Max(f.Least, w.Forest);
                }
                else if (t < w.Forest + w.Open)
                {
                    var o = recipe.Open;
                    if (r.Next() < o.Lone * sparse)
                    {
                        front.Add(Pick(roles.Big, x, Between(r, o.Tree)));
                        if (r.Next() < o.FeetOdds) AtFeet(x, 1);
                    }
                    else if (r.Next() < o.ShrubOdds) AtFeet(x, 1 + (int)Math.Floor(r.Next() * o.ShrubsMost));
                    x += w0 * Between(r, o.Step);
                }
                else
                {
                    var gr = recipe.Groves;
                    int n = Math.Max(1, (int)Round(Between(r, gr.Size) * sparse)), big = (int)Math.Floor(r.Next() * n);
                    double cx = x;
                    for (int i = 0; i < n; i++)
                    {
                        (i % 2 == 1 ? back : front).Add(Pick(roles.Big, cx, i == big ? Between(r, gr.Big) : Between(r, gr.Other)));
                        cx += w0 * Between(r, gr.Within);
                    }
                    AtFeet(x, 1);
                    AtFeet(cx - w0 * 0.5, (int)Math.Floor(Between(r, gr.Feet)));
                    x = cx + w0 * Between(r, gr.Gap) * (1 + recipe.RangeGap * rw);
                }
            }
        // Outcrops of the planet's small solids where a range meets its foothills.
        if (roles.Solid.Length > 0)
            for (double x = r.Next() * w0; x < tile; x += w0 * recipe.Outcrop.Every)
            {
                var o = recipe.Outcrop;
                double rw = RangeW(x / tile);
                if (r.Next() > rw * (1 - rw) * o.Odds + o.Least) continue;
                int n = (int)Round(Between(r, o.Size));
                for (int i = 0; i < n; i++) low.Add(Pick(roles.Solid, x + i * w0 * o.Apart, Between(r, o.Scale) * (1 - i * o.Shrink)));
            }
        items.AddRange(back);
        items.AddRange(front);
        items.AddRange(low);
        // Nearer shelves below the crest, so a deep valley looks into more of the same land.
        var s = recipe.Shelves;
        for (int k = 1; k <= d.Lower; k++)
        {
            var nL = Noise(r, across, g.HillWave * s.Wave);
            double drop = s.Drop * k / cell;
            items.Add(new RidgeItem(Points(u => SoilUp(u) * s.Soil - drop + s.Wobble * nL(u)), Sheet(type.Soil, true), type.Soil, true));
            for (double x = r.Next() * w0 * s.Start; x < tile; x += w0 * Between(r, s.Step))
            {
                if (r.Next() < s.TreeOdds && roles.Big.Length > 0) items.Add(Pick(roles.Big, x, Between(r, s.Tree)));
                else if (roles.Shrub.Length > 0) items.Add(Pick(roles.Shrub, x, Between(r, s.Shrub)));
            }
        }
        return (items, (int)Math.Floor(r.Next() * 1e6));
    }

    // ---------- A layer's items laid as draws ----------

    /// <summary>How far a grass top overlaps the next column's, so no seam shows between them.</summary>
    private const double Seam = 0.02;

    /// <summary>
    /// The page's <c>compose</c>: each ridge filled, washed green where grassy, its crest's grass tops in rows and, where grassy,
    /// belts of shrubs and tufts down its slope; each part standing on the ridge before it, sunk by its kind's sink.
    /// </summary>
    private static List<Draw> Lay(Depth d, List<Item> items, int layerSeed, Roles roles, Func<string, (int W, int H)?> size, (int R, int G, int B) grass, Recipe recipe)
    {
        double s = d.Scale, tile = recipe.Tile;
        var draws = new List<Draw>();
        var wash = ((float)(Round(grass.R * recipe.Shade) / 255), (float)(Round(grass.G * recipe.Shade) / 255), (float)(Round(grass.B * recipe.Shade) / 255));
        void Put(string name, double x, double foot, double k, bool flip)
        {
            if (size(name) is not { } p) return;
            double w = p.W * k, h = p.H * k;
            draws.Add(new Picture(name, x - w / 2, foot - h, w, h, flip));
        }
        double Wrap(double x) => ((x % tile) + tile) % tile;
        Ridge? crest = null;
        int ridges = 0;
        foreach (var item in items)
        {
            if (item is RidgeItem ri)
            {
                var ridge = RidgeOf(ri.Points, s, d.GroundRow, tile);
                int seed = 101 + ridges++ + layerSeed * 31;
                draws.Add(new Fill(ridge, ri.Sheet, ri.Grassy && d.Green > 0 ? (wash.Item1, wash.Item2, wash.Item3, (float)d.Green) : default));
                int rows = ri.Grassy ? d.Rows : 1;
                double th = recipe.TopTall * s;
                for (int k = rows - 1; k >= 0; k--)
                    foreach (var side in (string[])["back", "front"])
                    {
                        // A row's columns never overlap: laid picture by picture, so each picture's are drawn together.
                        var row = new List<(string Name, int I)>();
                        for (int i = 0; i < ridge.N; i++)
                            row.Add(($"top_{ri.Material}_{Hash(i / 2 + k * 977, 7) % (uint)recipe.TopVariants}_{side}_{(i + k) % 2}", i));
                        foreach (var (name, i) in row.OrderBy(c => c.Name, StringComparer.Ordinal).ThenBy(c => c.I))
                            if (size(name) != null)
                                draws.Add(new Picture(name, i * ridge.Cx, ridge.Top(i) - recipe.TopRise * s + k * recipe.RowStep * s, ridge.Cx + Seam, th, false));
                    }
                if (ri.Grassy) Belts(ridge, rows, seed);
                crest = ridge;
                continue;
            }
            var part = (Part)item;
            double scale = s * part.Scale;
            Put(part.Name, part.X, (crest?.At(Wrap(part.X)) ?? d.GroundRow) + part.Sink * scale, scale, part.Flip);
        }
        return draws;

        // Belts: shrubs in runs with gaps, tufts between, a row or two down the slope.
        void Belts(Ridge ridge, int rows, int seed)
        {
            var b = recipe.Belts;
            double deep = rows * recipe.RowStep * s, bw = b.ShrubWide * s, tf = b.TuftStep * s;
            string[] shrubs = roles.ShrubNames, tufts = roles.Cover;
            if (shrubs.Length > 0)
                for (int j = 0, many = (int)Math.Floor(tile / (bw * b.ShrubStep)); j < many; j++)
                {
                    uint h = Hash(j, seed), run = Hash(j / b.Run, seed + 1) % 100;
                    if (run < b.RunSkip || h % 100 < b.ShrubSkip) continue;
                    double x = j * bw * b.ShrubStep + (h >> 8) % 100 / 100.0 * bw * b.ShrubJitter;
                    double k = s * b.ShrubScales[(int)((h >> 16) % (uint)b.ShrubScales.Count)];
                    Put(shrubs[(h >> 20) % (uint)shrubs.Length], x, ridge.At(Wrap(x)) + b.Lift * s + (h >> 12) % 3 * deep / 3, k, ((h >> 22) & 1) == 1);
                }
            if (tufts.Length > 0)
                for (int j = 0, many = (int)Math.Floor(tile / tf); j < many; j++)
                {
                    uint h = Hash(j, seed + 2);
                    if (h % 100 < b.TuftSkip) continue;
                    double x = j * tf + (h >> 8) % 100 / 100.0 * tf;
                    Put(tufts[(h >> 16) % (uint)tufts.Length], x, ridge.At(Wrap(x)) + b.Lift * s + (h >> 12) % 4 * deep / 4, s * b.TuftScale, ((h >> 22) & 1) == 1);
                }
        }
    }

    /// <summary>A ridge from its profile: columns a cell of the layer wide, heights in whole cells eased between its points, one-cell stairs between.</summary>
    private static Ridge RidgeOf(List<(double X, double Y)> points, double s, double groundRow, double tile)
    {
        double cw = Camera.Cell * s;
        int n = Math.Max(8, (int)Round(tile / cw));
        double cx = tile / n;
        var up = new int[n];
        int k = -1;
        for (int i = 0; i < n; i++)
        {
            double x = (i + 0.5) * cx;
            while (k + 1 < points.Count && points[k + 1].X <= x) k++;
            var last = points[^1];
            var a = k < 0 ? (last.X - tile, last.Y) : points[k];
            var b = k < 0 ? points[0] : k == points.Count - 1 ? (points[0].X + tile, points[0].Y) : points[k + 1];
            double t = (x - a.Item1) / Math.Max(1e-6, b.Item1 - a.Item1), e = t * t * (3 - 2 * t);
            up[i] = (int)Round((a.Item2 + (b.Item2 - a.Item2) * e) / cw);
        }
        // The page's two passes, until nothing changes: a cell at most between neighbours, the tile's ends neighbours too.
        for (bool changed = true; changed;)
        {
            changed = false;
            for (int i = 0; i < n; i++) changed |= Raise(up, i, up[(i - 1 + n) % n]);
            for (int i = n - 1; i >= 0; i--) changed |= Raise(up, i, up[(i + 1) % n]);
        }
        return new Ridge(up, cw, cx, groundRow);
    }

    private static bool Raise(int[] up, int i, int beside)
    {
        if (up[i] >= beside - 1) return false;
        up[i] = beside - 1;
        return true;
    }

    // ---------- Noise, easing, the rng and the hash ----------

    /// <summary>A smooth noise wrapping at the tile: a value a lattice point, round(across / wave) of them, eased between.</summary>
    private static Func<double, double> Noise(Rng r, double across, double wave)
    {
        int m = Math.Max(1, (int)Round(across / wave));
        var v = new double[m];
        for (int i = 0; i < m; i++) v[i] = r.Next();
        return u =>
        {
            double t = ((u % 1) + 1) % 1 * m, f = t - Math.Floor(t), e = f * f * (3 - 2 * f);
            int g = (int)Math.Floor(t) % m;
            return v[g] + (v[(g + 1) % m] - v[g]) * e;
        };
    }

    private static double Ease(double a, double b, double v)
    {
        double t = Math.Clamp((v - a) / (b - a), 0, 1);
        return t * t * (3 - 2 * t);
    }

    private static double Between(Rng r, Span span) => span.A + (span.B - span.A) * r.Next();

    /// <summary>JavaScript's <c>Math.round</c>: halves up, as the page rounds.</summary>
    private static double Round(double x) => Math.Floor(x + 0.5);

    /// <summary>The page's hash of two integers.</summary>
    internal static uint Hash(int a, int s)
    {
        unchecked
        {
            uint h = (uint)a * 374761393u + (uint)s * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }

    /// <summary>The anime tools' rng (<c>tools/anime-art/engine.js</c>, mulberry32): a double in [0, 1) a draw, the same on every peer.</summary>
    internal sealed class Rng(uint seed)
    {
        private uint _a = seed;

        public double Next()
        {
            unchecked
            {
                _a += 0x6d2b79f5u;
                uint t = (_a ^ (_a >> 15)) * (1u | _a);
                t = (t + (t ^ (t >> 7)) * (61u | t)) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
    }
}
