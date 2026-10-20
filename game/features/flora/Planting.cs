using System;
using Stillwake.Sim.Grid;
using Anchor = Stillwake.Content.Flora.Anchor;

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
    /// Where a picture <paramref name="width"/> × <paramref name="height"/> of a growth in cell (<paramref name="x"/>,
    /// <paramref name="y"/>) is drawn and which of its rows show: on a floor its lowest opaque row, <paramref name="bottom"/>,
    /// <paramref name="sink"/> pixels into the ground, the rows in it cut off, so the ground's top edge covers its base; from a
    /// ceiling its highest, <paramref name="top"/>, as far up into the ceiling, mirrored; standing in the middle of its cell.
    /// </summary>
    public static (int X, int Y, int From, int Rows) Picture(Anchor on, int x, int y, int cell, int width, int height, int top, int bottom, int sink)
    {
        int left = x * cell + cell / 2 - width / 2;
        if (on == Anchor.Ceiling)
        {
            int ceiling = y * cell, corner = ceiling - sink - top, from = Math.Clamp(ceiling - corner, 0, height);
            return (left, corner, from, height - from);
        }
        int ground = (y + 1) * cell, at = ground + sink - 1 - bottom;
        return (left, at, 0, Math.Clamp(ground - at, 0, height));
    }

    /// <summary>
    /// Where the light of a growth in cell (<paramref name="x"/>, <paramref name="y"/>) shines from, given the middle of its glow
    /// (<paramref name="glowX"/>, <paramref name="glowY"/>): always in the open, <see cref="LightOff"/> off the face of what
    /// holds it, for a light in a wall lights nothing in the open and one on a face casts its shadow along it. One on a floor
    /// or from a ceiling is held off it in its own open cell; one in the rock shines from just past its face open nearest
    /// its glow, along it no nearer a corner than the same; null for one with no face open.
    /// </summary>
    public static (double X, double Y)? Light(Anchor on, int x, int y, int cell, double glowX, double glowY, Faces open)
    {
        double left = x * cell, top = y * cell, right = left + cell, bottom = top + cell;
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

    /// <summary>
    /// The band along <paramref name="face"/> of moss in cell (<paramref name="x"/>, <paramref name="y"/>), drawn the whole
    /// edge of its cell, <paramref name="thick"/> pixels, where a run of it goes on <paramref name="before"/> or
    /// <paramref name="after"/> it along that face: a run joins, rather than repeating a piece with its ends. Null for a lone
    /// piece, which keeps its own ends.
    /// </summary>
    public static (int X, int Y, int W, int H)? Strip(Faces face, int x, int y, int cell, int thick, bool before, bool after)
    {
        if (!before && !after) return null;
        int left = x * cell, top = y * cell;
        return face switch
        {
            Faces.Up => (left, top, cell, thick),
            Faces.Down => (left, top + cell - 1, cell, thick),
            Faces.Left => (left, top, thick, cell),
            _ => (left + cell - 1, top, thick, cell),
        };
    }

    /// <summary>
    /// The row of pixels a drip falling from (<paramref name="x"/>, <paramref name="y"/>) lands on: the top of the first wall,
    /// closed door or floor under it, down a slope's diagonal where it is cut to one, within <paramref name="rows"/> cells;
    /// null for none, or for one under the drip's very tip, where there is no open cell for it to fall through.
    /// </summary>
    public static double? DripFloor(Func<int, int> structureAt, Func<int, int> shapeAt, int width, int height, int cell, double x, double y, int rows)
    {
        int cx = (int)Math.Floor(x / cell), cy = (int)Math.Floor(y / cell);
        if (cx < 0 || cx >= width || cy < 0) return null;
        for (int row = cy; row < Math.Min(height, cy + rows); row++)
        {
            int at = row * width + cx, structure = structureAt(at);
            if (structure is Structure.Empty or Structure.DoorOpen) continue;
            int shape = structure == Structure.Wall ? shapeAt(at) : Shape.None;
            double surface = row * cell + (double)Shape.TopAt(shape, (int)((x / cell - cx) * Shape.Unit)) * cell / Shape.Unit;
            return surface - y < 2 ? null : surface;
        }
        return null;
    }
}
