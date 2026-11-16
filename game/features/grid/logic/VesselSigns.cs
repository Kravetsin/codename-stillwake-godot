using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Content;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using Looks = Stillwake.Game.VesselLooks;

namespace Stillwake.Game.Features.Grid;

/// <summary>What was left behind inside a derelict (derelict-looks): a picture, some with particles; never a thing.</summary>
public enum SignKind { Frost, Cable, Soot, Gas, Panel, Crate }

/// <summary>
/// A sign of abandonment: its kind, its picture's box in the vessel's frame (<c>X</c>, <c>Y</c>, <c>Width</c> by <c>Height</c> cells), the
/// open rows of the room it is in (from <c>RoomLeft</c>, <c>RoomWidth</c> across, from <c>RoomTop</c>), which way it faces (<c>Dir</c>: 1
/// its picture as drawn, -1 mirrored), the machine soot lies over (its kind and top-left), and <c>Cells</c>, the cells of its box stamped
/// bare (a hull back wall and nothing else), which it shows only while all stay so.
/// </summary>
public sealed record VesselSign(SignKind Kind, int X, int Y, int Width, int Height, int RoomLeft, int RoomTop, int RoomWidth, int Dir,
    IReadOnlyList<(int X, int Y)> Cells, string? Machine = null, int MachineX = 0, int MachineY = 0);

/// <summary>
/// The signs inside an abandoned vessel (derelict-looks), a rule of drawing, never the sim's: frost at a cold wall, a hanging cable that
/// sparks, soot over a burnt machine, a burst pipe leaking gas, a fallen ceiling panel, a toppled crate. Their count, kinds and cells come
/// from the vessel its seed describes, stamped (<see cref="Shipyard.Stamp"/> in reading order), drawn by <see cref="Looks.LookRng"/>;
/// in rooms other than the airlock and the bridge, a kind at most once, a room each while one is left. The live grid decides only whether
/// each shows (<see cref="Shows"/>).
/// </summary>
public sealed class VesselSigns
{
    /// <summary>Each kind's picture in cells across and down, and the row of its room's open rows it starts on; soot's from its machine.</summary>
    public static (int Width, int Height, int Row) Size(SignKind kind) => kind switch
    {
        SignKind.Frost => (5, Vessels.Open, 0),
        SignKind.Cable => (3, 4, 0),
        SignKind.Soot => (6, 5, 0),
        SignKind.Gas => (8, 2, 0),
        SignKind.Panel => (4, Vessels.Open, 0),
        _ => (5, 3, Vessels.Open - 3),
    };

    /// <summary>Frost lies against a wall of the hull at most this many cells past its side, on this many of its rows at least: a bow's slope too.</summary>
    private const int FrostReach = 2, FrostRows = 4;

    /// <summary>The picture a sign of <paramref name="kind"/> is drawn with, its box's size (anime-art subject <c>vessel</c>).</summary>
    public static string PictureOf(SignKind kind) => kind switch
    {
        SignKind.Frost => "vessel_frost",
        SignKind.Cable => "vessel_cable",
        SignKind.Soot => "vessel_soot",
        SignKind.Gas => "vessel_pipe",
        SignKind.Panel => "vessel_fallen",
        _ => "vessel_toppled",
    };

    /// <summary>Whether a sign of <paramref name="kind"/> is painted on the back wall, behind what stands before it: frost and soot.</summary>
    public static bool OnTheWall(SignKind kind) => kind is SignKind.Frost or SignKind.Soot;

    public uint Seed { get; }
    public IReadOnlyList<VesselSign> Signs { get; }

    public VesselSigns(Vessel vessel)
    {
        Seed = vessel.Seed;
        int w = vessel.Width, h = vessel.Height;
        var parts = Shipyard.Stamp(vessel);
        // What each cell was stamped with: a hull back wall, a structure (a wall of the hull among them), a machine.
        var back = new bool[w * h];
        var built = new bool[w * h];
        var wall = new bool[w * h];
        var machine = new bool[w * h];
        foreach (var p in parts)
        {
            if (p.Layer == "back") back[p.Y * w + p.X] = p.Material == Vessels.Hull;
            else
                foreach (var (x, y) in Shipyard.Covers(p))
                    if (x >= 0 && y >= 0 && x < w && y < h)
                    {
                        if (p.Layer == "machine") machine[y * w + x] = true;
                        else built[y * w + x] = true;
                        if (p.Kind == "wall") wall[y * w + x] = true;
                    }
        }
        bool In(int x, int y) => x >= 0 && y >= 0 && x < w && y < h;
        // On the back wall: a hull back wall with no structure, a machine before it or not; bare: with none.
        bool Backed(int x, int y) => In(x, y) && back[y * w + x] && !built[y * w + x];
        bool Bare(int x, int y) => Backed(x, y) && !machine[y * w + x];
        bool Wall(int x, int y) => In(x, y) && wall[y * w + x];

        var (sx, sy) = Shipyard.Door(vessel);
        bool Holds(Vessel.Compartment c, int x, int y) => x >= c.Left && x < c.Left + c.Width && y >= c.Top && y < c.Top + Vessels.Open;
        // A room reaches over the free columns at its ends, to a door or a wall: the bow's hollow past the last room is that room's.
        var all = vessel.Compartments.Select(c =>
        {
            int mid = c.Top + Vessels.Open / 2, left = c.Left, right = c.Left + c.Width;
            bool Free(int x) => Backed(x, mid) && !vessel.Compartments.Any(o => Holds(o, x, mid));
            while (Free(left - 1)) left--;
            while (Free(right)) right++;
            return c with { Left = left, Width = right - left };
        }).ToList();
        var rooms = all.Where(c => c.Role != VesselRole.Bridge && !Holds(c, sx, sy)).ToList();
        Vessel.Compartment? RoomOf(int x, int y) => all.FirstOrDefault(c => Holds(c, x, y));

        var (least, most) = Looks.Signs[vessel.Class];
        int count = least + Looks.Below(Seed, Looks.Salt.SignCount, 0, most - least + 1);
        var kinds = Enum.GetValues<SignKind>().ToList();
        for (int i = kinds.Count - 1; i > 0; i--)
        {
            int j = Looks.Below(Seed, Looks.Salt.SignKinds, i, i + 1);
            (kinds[i], kinds[j]) = (kinds[j], kinds[i]);
        }

        var signs = new List<VesselSign>();
        var used = new HashSet<(int, int)>();
        var full = new HashSet<(int, int)>();
        foreach (var kind in kinds)
        {
            if (signs.Count == count) break;
            var fits = Candidates(kind).Where(s => !Box(s).Any(used.Contains)).ToList();
            // A room of its own while one is left.
            var alone = fits.Where(s => !full.Contains((s.RoomLeft, s.RoomTop))).ToList();
            if (alone.Count > 0) fits = alone;
            if (fits.Count == 0) continue;
            var sign = fits[Looks.Below(Seed, Looks.Salt.Sign, (int)kind, fits.Count)];
            signs.Add(sign);
            full.Add((sign.RoomLeft, sign.RoomTop));
            used.UnionWith(Box(sign));
        }
        Signs = signs;

        // Where a sign of kind may lie, in reading order: from each stamped back wall its box's top-left would be on, soot from each machine.
        IEnumerable<VesselSign> Candidates(SignKind kind)
        {
            var (sw, sh, row) = Size(kind);
            if (kind == SignKind.Soot)
            {
                foreach (var m in parts.Where(p => p.Layer == "machine" && Looks.SootOver.Contains(Vessels.FirstLook(p.Kind))))
                {
                    // Over its middle, its foot two rows down the machine: the burn rose from its top, at least two rows of wall over it.
                    var spec = Machines.Get(m.Kind);
                    if (rooms.FirstOrDefault(c => Holds(c, m.X, m.Y)) is not { } room || m.Y - room.Top < 2) continue;
                    int top = Math.Max(room.Top, m.Y + 2 - sh);
                    if (Fits(new VesselSign(kind, m.X + spec.Width / 2 - sw / 2, top, sw, m.Y + 2 - top, room.Left, room.Top, room.Width, 1, [], m.Kind, m.X, m.Y), room) is { } sign)
                        yield return sign;
                }
                yield break;
            }
            foreach (var p in parts)
            {
                if (p.Layer != "back" || rooms.FirstOrDefault(c => Holds(c, p.X, p.Y)) is not { } room || p.Y != room.Top + row) continue;
                int dir = Looks.Below(Seed, Looks.Salt.SignDir, p.Y * w + p.X, 2) == 0 ? 1 : -1;
                if (kind == SignKind.Frost)
                {
                    // Against a cold wall: down most of its rows, the first cell past its side not on the back wall, a few at most, is a
                    // wall of the hull (a door is none); the picture's wall on its right.
                    int cold(int from, int step) => Enumerable.Range(room.Top, Vessels.Open).Count(y =>
                    {
                        int x = from;
                        while (x != from + step * FrostReach && Backed(x, y)) x += step;
                        return Wall(x, y);
                    });
                    bool right = cold(p.X + sw, 1) >= FrostRows, left = cold(p.X - 1, -1) >= FrostRows;
                    if (!right && !left) continue;
                    dir = right ? 1 : -1;
                }
                if (Fits(new VesselSign(kind, p.X, p.Y, sw, sh, room.Left, room.Top, room.Width, dir, []), room) is { } sign) yield return sign;
            }
        }

        // The sign with the cells it was stamped on, if its box lies in its room's open rows, on no other room's cells and on the back wall:
        // a picture on the wall on half its cells bare at least, behind what stands there; any other on bare cells only.
        VesselSign? Fits(VesselSign s, Vessel.Compartment room)
        {
            if (s.Y < room.Top || s.Y + s.Height > room.Top + Vessels.Open) return null;
            var cells = new List<(int X, int Y)>();
            int all = 0;
            foreach (var (x, y) in Box(s))
            {
                if (RoomOf(x, y) is { } other && other != room) return null;
                if (s.Machine != null && x >= s.MachineX && x < s.MachineX + Machines.Get(s.Machine).Width && y >= s.MachineY) continue;
                all++;
                if (Bare(x, y)) cells.Add((x, y));
                else if (!OnTheWall(s.Kind) || !Backed(x, y)) return null;
            }
            return cells.Count * 2 >= all ? s with { Cells = cells } : null;
        }
    }

    /// <summary>Every cell of <paramref name="s"/>'s box.</summary>
    private static IEnumerable<(int X, int Y)> Box(VesselSign s)
    {
        for (int y = s.Y; y < s.Y + s.Height; y++)
            for (int x = s.X; x < s.X + s.Width; x++)
                yield return (x, y);
    }

    /// <summary>The signs inside the vessel a surface of <paramref name="rules"/> holds from <paramref name="seed"/>; none off a vessel.</summary>
    public static VesselSigns? For(Surfaces.Kind rules, uint seed) =>
        rules is VesselKind kind ? new VesselSigns(Shipyard.Describe(Vessels.Class(kind.Class)!, seed)) : null;

    /// <summary>The grid cell of the frame's (<paramref name="x"/>, <paramref name="y"/>), the frame's top-left at <c>Vessels.Margin</c> every way.</summary>
    public static int CellOf(int x, int y, int gridWidth) => (y + Vessels.Margin) * gridWidth + x + Vessels.Margin;

    /// <summary>Whether <paramref name="sign"/> still shows: every cell it was stamped on still a hull back wall, with no structure and no machine.</summary>
    public static bool Shows(IGridQueries grid, VesselSign sign)
    {
        foreach (var (x, y) in sign.Cells)
        {
            int cell = CellOf(x, y, grid.Width);
            if (grid.StructureAt(cell) != Structure.Empty || grid.MachineAt(cell) != null || grid.BackAt(cell)?.Id != Vessels.Hull) return false;
        }
        return true;
    }
}
