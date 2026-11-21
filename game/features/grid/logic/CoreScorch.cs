using System;
using System.Collections.Generic;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// A plate's face cut by the core: the path's cell (<paramref name="X"/>, <paramref name="Y"/>) of the drawing it faces, the way (<paramref name="Dx"/>,
/// <paramref name="Dy"/>) from it to the plate, and its place among all the faces (<paramref name="Index"/>), which sets its look.
/// </summary>
public sealed record ScorchFace(int Index, int X, int Y, int Dx, int Dy)
{
    /// <summary>The plate's cell of the drawing.</summary>
    public (int X, int Y) Plate => (X + Dx, Y + Dy);
}

/// <summary>A spark off a face: where it leaves and how fast, in fine pixels a second; how often it flies, when in that and for how long, in seconds.</summary>
public sealed record ScorchSpark(ScorchFace Face, double X, double Y, double Vx, double Vy, double Period, double Offset, double Life);

/// <summary>
/// The scorch round the hole the alien core punched through the wreck's roof (<see cref="Wreck.CorePath"/>), a rule of drawing, never the sim's, ported from the
/// user's pick (wreckHoleEdges): an ember line breathing on each plate's face the path cut, a rare spark off it, soot over the plates near it. Each lasts while
/// its plate stands. Worked out in fine pixels, the pick's: <see cref="CellFine"/> to a cell of the drawing, (0, 0) at its corner.
/// </summary>
public sealed class CoreScorch
{
    /// <summary>Fine pixels to a cell of the drawing, and to a pixel of the world: the pick drew a cell at 24 px, three times the game's.</summary>
    public const int CellFine = 24, Fine = CellFine / Camera.Cell;

    /// <summary>How far from the path soot may reach, in fine pixels.</summary>
    public const int Reach = 30;

    /// <summary>How deep behind a face its dull band may go, in fine pixels; how long a stretch of the line breathes as one.</summary>
    public const int Depth = 3, Chunk = 4;

    /// <summary>The ember line's colour, its dull band's, and a spark's at its hottest.</summary>
    public static readonly (int R, int G, int B) Ember = (0xff, 0x8a, 0x3a), EmberDull = (0x9a, 0x34, 0x18), SparkHot = (0xff, 0xd2, 0x9a);

    private const double BreathSeconds = 5.5, Tau = Math.PI * 2;
    // The pick's frame: its noise read at these, so the soot falls as it was picked.
    private const int PageLeft = -1520, PageTop = -69;
    // The soot's steps out from the hole, darkest first, each with how much of the plate's colour it keeps: the dips are its tide lines.
    private static readonly (double Below, double Keep)[] Zones = [(2.5, 0.3), (7.5, 0.52), (8.5, 0.44), (17, 0.74), (18.3, 0.64)];
    private static readonly (double X, double Y) Way = Unit(-0.42, 0.91);

    /// <summary>The path's cells of the drawing.</summary>
    public static readonly IReadOnlyList<(int X, int Y)> Path = PathCells();

    /// <summary>Every face of a plate drawn beside the path, in the pick's order.</summary>
    public static readonly IReadOnlyList<ScorchFace> Faces = FindFaces();

    /// <summary>The plates drawn within <see cref="Reach"/> of the path: what may be sooted.</summary>
    public static readonly IReadOnlyList<(int X, int Y)> SootCells = FindSoot();

    /// <summary>The cells of the drawing holding the path and its soot.</summary>
    public static readonly (int X, int Y, int Width, int Height) Box = BoxOf();

    /// <summary>The few sparks, the pick's, each off a face.</summary>
    public static readonly IReadOnlyList<ScorchSpark> Sparks = MakeSparks();

    private readonly IGridQueries _grid;
    private readonly (int X, int Y) _corner;
    private readonly List<ScorchFace> _faces = [];
    private readonly List<(int X, int Y)> _soot = [];
    private readonly List<ScorchSpark> _sparks = [];
    private readonly bool[] _faceLive = new bool[Faces.Count], _sootLive = new bool[SootCells.Count];
    private int _version = -1;

    public CoreScorch(IGridQueries grid)
    {
        _grid = grid;
        _corner = WreckDress.CornerIn(grid.Width, grid.Height);
        Origin = ((_corner.X + Wreck.Left) * Camera.Cell, (_corner.Y + Wreck.Top) * Camera.Cell);
    }

    /// <summary>The drawing's corner in pixels of the world: fine (0, 0).</summary>
    public (int X, int Y) Origin { get; }

    /// <summary>The faces whose plate stands, the path's cell before it open.</summary>
    public IReadOnlyList<ScorchFace> LiveFaces => _faces;

    /// <summary>The sooted plates standing.</summary>
    public IReadOnlyList<(int X, int Y)> LiveSoot => _soot;

    /// <summary>The sparks whose face is live.</summary>
    public IReadOnlyList<ScorchSpark> LiveSparks => _sparks;

    /// <summary>Looks at the grid again if it changed since: only the cells round the path. Whether what is drawn changed.</summary>
    public bool Look()
    {
        if (_grid.Version == _version) return false;
        _version = _grid.Version;
        bool changed = false;
        for (int i = 0; i < Faces.Count; i++)
            changed |= _faceLive[i] != (_faceLive[i] = Plate(Faces[i].Plate) && Open((Faces[i].X, Faces[i].Y)));
        for (int i = 0; i < SootCells.Count; i++)
            changed |= _sootLive[i] != (_sootLive[i] = Plate(SootCells[i]));
        if (!changed) return false;
        _faces.Clear();
        _soot.Clear();
        _sparks.Clear();
        for (int i = 0; i < Faces.Count; i++)
            if (_faceLive[i]) _faces.Add(Faces[i]);
        for (int i = 0; i < SootCells.Count; i++)
            if (_sootLive[i]) _soot.Add(SootCells[i]);
        foreach (var spark in Sparks)
            if (_faceLive[spark.Face.Index]) _sparks.Add(spark);
        return true;
    }

    /// <summary>Whether the original plate stands at the cell of the drawing: a wall built there since is clean scrap, never scorched.</summary>
    private bool Plate((int X, int Y) cell) => GridCell(cell) is int i && _grid.WallAt(i) is { } wall && wall.Code == WreckDress.Code;

    private bool Open((int X, int Y) cell) => GridCell(cell) is int i && _grid.WallAt(i) == null;

    private int? GridCell((int X, int Y) cell)
    {
        int x = cell.X + Wreck.Left + _corner.X, y = cell.Y + Wreck.Top + _corner.Y;
        return x < 0 || y < 0 || x >= _grid.Width || y >= _grid.Height ? null : y * _grid.Width + x;
    }

    /// <summary>Whether the cell of the drawing is on the path.</summary>
    public static bool OnPath(int x, int y)
    {
        foreach (var (row, first, last) in Wreck.CorePath)
            if (row == y && x >= first && x <= last) return true;
        return false;
    }

    /// <summary>The face's cut, from one end to the other, in fine pixels.</summary>
    public static (int X1, int Y1, int X2, int Y2) LineOf(ScorchFace f)
    {
        int left = f.X * CellFine, top = f.Y * CellFine;
        return (f.Dx, f.Dy) switch
        {
            (0, -1) => (left, top, left + CellFine, top),
            (0, 1) => (left, top + CellFine, left + CellFine, top + CellFine),
            (-1, 0) => (left, top, left, top + CellFine),
            _ => (left + CellFine, top, left + CellFine, top + CellFine),
        };
    }

    /// <summary>The plate's fine pixel <paramref name="k"/> deep behind the face, <paramref name="s"/> along it.</summary>
    public static (int X, int Y) BandPixel(ScorchFace f, int s, int k)
    {
        var (x1, y1, _, _) = LineOf(f);
        return f.Dy != 0 ? (x1 + s, f.Dy < 0 ? y1 - 1 - k : y1 + k) : (f.Dx < 0 ? x1 - 1 - k : x1 + k, y1 + s);
    }

    /// <summary>Along the face, how deep its dull band goes, and whether the hot line runs there or breaks for a cooler stretch.</summary>
    public static (int Depth, bool Core)[] Band(ScorchFace f)
    {
        var band = new (int, bool)[CellFine];
        for (int s = 0; s < CellFine; s++)
        {
            double n = Noise(s * 0.16 + f.Index * 7.3, 0.5, 11);
            bool gap = Noise(s * 0.13 + f.Index * 3.1, 2.5, 17) < -0.15;
            band[s] = ((int)Math.Floor((n + 1) * 1.5 + 0.5), !gap);
        }
        return band;
    }

    /// <summary>How far through its slow breath the face's <paramref name="chunk"/>th stretch is at <paramref name="seconds"/>, from 0 to 1.</summary>
    public static double Breath(ScorchFace f, int chunk, double seconds) => 0.5 + 0.5 * Math.Sin(Tau * seconds / BreathSeconds + f.Index * 0.9 + chunk * 0.35);

    /// <summary>The hot line's opacity at a breath, and its dull band's.</summary>
    public static (double Line, double Band) Glow(double breath) => (0.4 + 0.6 * breath, 0.3 + 0.4 * breath);

    /// <summary>
    /// The spark at <paramref name="seconds"/>, null between its flights: its fine pixel and the dull one trailing it, and how far through its life it is,
    /// which cools it.
    /// </summary>
    public static ((int X, int Y) Head, (int X, int Y) Trail, double Age)? SparkAt(ScorchSpark s, double seconds)
    {
        double a = (seconds + s.Offset) % s.Period;
        if (a > s.Life) return null;
        double x = s.X + s.Vx * a, y = s.Y + s.Vy * a + 3 * a * a;
        return ((Round(x), Round(y)), (Round(x - s.Vx * 0.12), Round(y - s.Vy * 0.12)), a / s.Life);
    }

    /// <summary>A spark's colour as it ages: hot, then the line's, then dull.</summary>
    public static (int R, int G, int B) SparkColour(double age) => age < 0.3 ? SparkHot : age < 0.7 ? Ember : EmberDull;

    /// <summary>
    /// The soot at a fine pixel of a plate: how much of its colour it keeps, 1 for none, and the faint heat tint over it at the very edge, its alpha 0 for none.
    /// </summary>
    public static (double Keep, (int R, int G, int B, int A) Tint) SootAt(int fx, int fy)
    {
        double dist = ChannelDistance(fx + 0.5, fy + 0.5);
        if (dist > Reach) return (1, default);
        int x = fx + PageLeft, y = fy + PageTop;
        double u = x * Way.X + y * Way.Y, v = -x * Way.Y + y * Way.X;
        // Streaks along the core's way.
        double dd = dist * (1 - 0.4 * Noise(u * 0.05, v * 0.2, 3)) - 2 * Noise(x * 0.3, y * 0.3, 7);
        double keep = 1;
        foreach (var (below, k) in Zones)
            if (dd < below) { keep = k; break; }
        if (keep < 1) keep *= 1 + (Hash(x, y, 5) - 0.5) * 0.08;
        if (dist >= 3) return (keep, default);
        double h = Noise(x * 0.09, y * 0.09, 13);
        var (r, g, b) = h < -0.25 ? (118, 82, 160) : h < 0.2 ? (66, 108, 160) : (168, 122, 64);
        return (keep, (r, g, b, dist < 1.5 ? 74 : 40));
    }

    /// <summary>How far a fine point is from the path, in fine pixels.</summary>
    public static double ChannelDistance(double px, double py)
    {
        double best = double.MaxValue;
        foreach (var (row, first, last) in Wreck.CorePath)
        {
            double dx = Math.Max(Math.Max(first * CellFine - px, 0), px - (last + 1) * CellFine);
            double dy = Math.Max(Math.Max(row * CellFine - py, 0), py - (row + 1) * CellFine);
            best = Math.Min(best, Math.Sqrt(dx * dx + dy * dy));
        }
        return best;
    }

    private static List<(int X, int Y)> PathCells()
    {
        var cells = new List<(int X, int Y)>();
        foreach (var (row, first, last) in Wreck.CorePath)
            for (int x = first; x <= last; x++) cells.Add((x, row));
        return cells;
    }

    private static List<ScorchFace> FindFaces()
    {
        var faces = new List<ScorchFace>();
        foreach (var (x, y) in Path)
            foreach (var (dx, dy) in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
                if (!OnPath(x + dx, y + dy) && WreckDress.IsWall(x + dx, y + dy)) faces.Add(new ScorchFace(faces.Count, x, y, dx, dy));
        return faces;
    }

    private static List<(int X, int Y)> FindSoot()
    {
        var cells = new List<(int X, int Y)>();
        int reach = Reach / CellFine + 1;
        int top = Wreck.CorePath[0].Row - reach, bottom = Wreck.CorePath[^1].Row + reach;
        int left = int.MaxValue, right = int.MinValue;
        foreach (var (_, first, last) in Wreck.CorePath) (left, right) = (Math.Min(left, first - reach), Math.Max(right, last + reach));
        for (int y = top; y <= bottom; y++)
            for (int x = left; x <= right; x++)
                if (!OnPath(x, y) && WreckDress.IsWall(x, y) && CellDistance(x, y) <= Reach) cells.Add((x, y));
        return cells;
    }

    private static double CellDistance(int x, int y)
    {
        // The nearest point of the cell to the path: the path's own distance there, clamped into the cell.
        double best = double.MaxValue;
        foreach (var (row, first, last) in Wreck.CorePath)
        {
            double dx = Math.Max(0, Math.Max(first - x - 1, x - last - 1)) * CellFine, dy = Math.Max(0, Math.Max(row - y - 1, y - row - 1)) * CellFine;
            best = Math.Min(best, Math.Sqrt(dx * dx + dy * dy));
        }
        return best;
    }

    private static (int X, int Y, int Width, int Height) BoxOf()
    {
        int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
        foreach (var (x, y) in Path) (left, top, right, bottom) = (Math.Min(left, x), Math.Min(top, y), Math.Max(right, x), Math.Max(bottom, y));
        foreach (var (x, y) in SootCells) (left, top, right, bottom) = (Math.Min(left, x), Math.Min(top, y), Math.Max(right, x), Math.Max(bottom, y));
        return (left, top, right - left + 1, bottom - top + 1);
    }

    private static List<ScorchSpark> MakeSparks()
    {
        var next = Rng(4242);
        var sparks = new List<ScorchSpark>();
        for (int k = 0; k < 6 && Faces.Count > 0; k++)
        {
            var f = Faces[(int)(next() * Faces.Count)];
            double u = 0.15 + next() * 0.7, sp = 3 + next() * 5, dr = 4 + next() * 7;
            var (x1, y1, x2, y2) = LineOf(f);
            var (nx, ny) = (-f.Dx, -f.Dy);
            double period = 6 + next() * 5, offset = next() * 11, life = 1.2 + next() * 1.0;
            sparks.Add(new ScorchSpark(f, x1 + (x2 - x1) * u + nx, y1 + (y2 - y1) * u + ny, nx * sp + Way.X * dr, ny * sp + Way.Y * dr, period, offset, life));
        }
        return sparks;
    }

    private static (double X, double Y) Unit(double x, double y)
    {
        double l = Math.Sqrt(x * x + y * y);
        return (x / l, y / l);
    }

    private static int Round(double v) => (int)Math.Floor(v + 0.5);

    // The pick's mulberry32, its hash and value noise, as JavaScript computes them.
    private static Func<double> Rng(int seed)
    {
        uint a = (uint)seed;
        return () =>
        {
            a = unchecked(a + 0x6d2b79f5);
            uint t = unchecked((a ^ (a >> 15)) * (1 | a));
            t = unchecked((t + (t ^ (t >> 7)) * (61 | t)) ^ t);
            return (t ^ (t >> 14)) / 4294967296.0;
        };
    }

    private static double Hash(int i, int j, int seed)
    {
        uint h = unchecked((uint)i * 374761393u + (uint)j * 668265263u + (uint)seed * 1442695041u);
        h = unchecked((h ^ (h >> 13)) * 1274126177u);
        h ^= h >> 16;
        return h / 4294967296.0;
    }

    private static double Noise(double x, double y, int seed)
    {
        int i = (int)Math.Floor(x), j = (int)Math.Floor(y);
        double fx = x - i, fy = y - j, sx = fx * fx * (3 - 2 * fx), sy = fy * fy * (3 - 2 * fy);
        double a = Hash(i, j, seed), b = Hash(i + 1, j, seed), c = Hash(i, j + 1, seed), d = Hash(i + 1, j + 1, seed);
        return (a + (b - a) * sx + (c - a) * sy + (a - b - c + d) * sx * sy) * 2 - 1;
    }
}
