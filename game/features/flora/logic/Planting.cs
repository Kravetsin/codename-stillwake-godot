using System;
using Stillwake.Sim.Grid;
using Anchor = Stillwake.Content.Flora.Anchor;
using Structures = Stillwake.Content.Structures;

namespace Stillwake.Game.Features.Flora;

/// <summary>
/// Where what grows on a planet is drawn against what holds it (0079, 0080), in pixels of the world, plain C# so it is tested:
/// a picture planted in the ground, its base sunk under the ground's top edge, or hung from a ceiling the same way mirrored;
/// where a growth's light shines from, always in the open; where a drip from a stalactite lands.
/// </summary>
public static class Planting
{
    /// <summary>The faces of a growth's cell that are open, for one in the rock.</summary>
    [Flags]
    public enum Faces
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,
    }

    /// <summary>How far a growth's light shines from off the face of what holds it, in pixels: past the half of its size.</summary>
    public const double LightOff = 3;

    /// <summary>
    /// Where a picture <paramref name="width"/> × <paramref name="height"/> of a growth at cell (<paramref name="x"/>,
    /// <paramref name="y"/>), <paramref name="across"/> columns from it, is drawn and which of its rows show: on a floor its
    /// lowest opaque row, <paramref name="bottom"/>, <paramref name="sink"/> pixels into the ground, the rows in it cut off, so
    /// the ground's top edge covers its base; from a ceiling its highest, <paramref name="top"/>, as far up into the ceiling,
    /// mirrored; standing in the middle of its columns.
    /// </summary>
    public static (int X, int Y, int From, int Rows) Picture(Anchor on, int x, int y, int cell, int across, int width, int height, int top, int bottom, int sink)
    {
        int left = x * cell + across * cell / 2 - width / 2;
        if (on == Anchor.Ceiling)
        {
            int ceiling = y * cell, corner = ceiling - sink - top, from = Math.Clamp(ceiling - corner, 0, height);
            return (left, corner, from, height - from);
        }
        int ground = (y + 1) * cell, at = ground + sink - 1 - bottom;
        return (left, at, 0, Math.Clamp(ground - at, 0, height));
    }

    /// <summary>
    /// Where the light of a growth at cell (<paramref name="x"/>, <paramref name="y"/>) shines from, given the middle of its glow
    /// (<paramref name="glowX"/>, <paramref name="glowY"/>): always in the open, <see cref="LightOff"/> off the face of what
    /// holds it, for a light in a wall lights nothing in the open and one on a face casts its shadow along it. One on a floor
    /// or from a ceiling is held off it in its own open cells; one in the rock, its block <paramref name="across"/> by
    /// <paramref name="tall"/> cells down and right of its own, shines from just past its face open nearest its glow, along it
    /// no nearer a corner than the same; null for one with no face open.
    /// </summary>
    public static (double X, double Y)? Light(Anchor on, int x, int y, int cell, int across, int tall, double glowX, double glowY, Faces open)
    {
        double left = x * cell, top = y * cell, right = left + across * cell, bottom = top + (on == Anchor.Rock ? tall : 1) * cell;
        switch (on)
        {
            case Anchor.Floor:
                return (glowX, Math.Min(glowY, bottom - LightOff));
            case Anchor.Ceiling:
                return (glowX, Math.Max(glowY, top + LightOff));
        }
        double along = Math.Clamp(glowX, left + LightOff, right - LightOff), down = Math.Clamp(glowY, top + LightOff, bottom - LightOff);
        (double X, double Y)? best = null;
        double nearest = double.MaxValue;
        void Face(Faces face, double far, double px, double py)
        {
            if ((open & face) == 0 || far >= nearest) return;
            (nearest, best) = (far, (px, py));
        }
        Face(Faces.Up, glowY - top, along, top - LightOff);
        Face(Faces.Down, bottom - glowY, along, bottom + LightOff);
        Face(Faces.Left, glowX - left, left - LightOff, down);
        Face(Faces.Right, right - glowX, right + LightOff, down);
        return best;
    }

    /// <summary>How far a piece of moss's picture reaches past its block on every side, and its whole size round a block of 16.</summary>
    private const int PieceOver = 4, PieceSize = 24;

    /// <summary>How an end of a moss face goes on: into the next block's moss, out past its block into the open, or into a wall.</summary>
    public enum End
    {
        Joins,
        Open,
        Wall,
    }

    /// <summary>
    /// Which part of a moss picture is drawn where along <paramref name="face"/>, in its picture's pixels: cut at its block's edge
    /// where its run <see cref="End.Joins"/> the next block's (2026-11-01-moss-in-ink); elsewhere its tapered end kept, reaching
    /// past the block into the open or squeezed in to stop at a wall. Along only its <paramref name="first"/> and
    /// <paramref name="second"/> cell where the face is open beside them, the whole band squeezed in, so on a stair's step it
    /// tapers into the corner rather than lying over the stone beside it or ending in a straight cut.
    /// </summary>
    public static ((int X, int Y, int W, int H) From, (int X, int Y, int W, int H) To) Piece(Faces face, End start, End end, bool first = true, bool second = true)
    {
        const int middle = PieceSize / 2;
        int from = first && start == End.Joins ? PieceOver : 0, to = second && end == End.Joins ? PieceSize - PieceOver : PieceSize;
        int at = !first ? middle : start == End.Open ? 0 : PieceOver, till = !second ? middle : end == End.Open ? PieceSize : PieceSize - PieceOver;
        return face is Faces.Up or Faces.Down
            ? ((from, 0, to - from, PieceSize), (at, 0, till - at, PieceSize))
            : ((0, from, PieceSize, to - from), (0, at, PieceSize, till - at));
    }

    /// <summary>
    /// Where <paramref name="face"/> of the 2 × 2 block at <paramref name="cell"/>, in a grid <paramref name="width"/> wide, is
    /// <paramref name="open"/>: beside its first cell along it (the left or top one) and its second, and just past its start and
    /// its end on its open side, where a stone cell means the face meets a wall and moss reaching past that end would lie on it.
    /// </summary>
    public static (bool First, bool Second, bool Start, bool End) Along(Faces face, int cell, int width, Func<int, bool> open)
    {
        int x = cell % width, w = width;
        bool At(int dx, int dy) => x + dx >= 0 && x + dx < w && open(cell + dy * w + dx);
        return face switch
        {
            Faces.Up => (At(0, -1), At(1, -1), At(-1, -1), At(2, -1)),
            Faces.Down => (At(0, 2), At(1, 2), At(-1, 2), At(2, 2)),
            Faces.Left => (At(-1, 0), At(-1, 1), At(-1, -1), At(-1, 2)),
            _ => (At(2, 0), At(2, 1), At(2, -1), At(2, 2)),
        };
    }

    /// <summary>
    /// The row of pixels a drip falling from (<paramref name="x"/>, <paramref name="y"/>) lands on: the top of the first wall,
    /// closed door or floor under it, within <paramref name="rows"/> cells; null for none, or for one under the drip's very
    /// tip, where there is no open cell for it to fall through.
    /// </summary>
    public static double? DripFloor(Func<int, int> structureAt, int width, int height, int cell, double x, double y, int rows)
    {
        int cx = (int)Math.Floor(x / cell), cy = (int)Math.Floor(y / cell);
        if (cx < 0 || cx >= width || cy < 0) return null;
        for (int row = cy; row < Math.Min(height, cy + rows); row++)
        {
            int at = row * width + cx, structure = structureAt(at);
            if (!Structures.Solid(structure)) continue;
            double surface = row * cell;
            return surface - y < 2 ? null : surface;
        }
        return null;
    }
}
