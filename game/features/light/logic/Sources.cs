using System;
using System.Collections.Generic;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;
using Materials = Stillwake.Content.Materials;
using Structures = Stillwake.Content.Structures;
using Trait = Stillwake.Content.Trait;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// One light as the lightmap draws it, in pixels of the world: <see cref="Colour"/> falling off by
/// <see cref="Keep"/> a cell out to <see cref="Reach"/> cells, each colour at most <see cref="Most"/>, then
/// times <see cref="Strength"/> — a lamp's flicker, a fire's flame, an ember's fade; <see cref="Size"/>
/// pixels across, which softens its shadows' edges, or a point at 0. With <see cref="Toward"/>, a unit vector, a beam that
/// way (0077: the helmet lamp), the lightmap's beam wide, its most whole.
/// </summary>
public readonly record struct LightSource(
    double X, double Y, (float R, float G, float B) Colour, float Strength, int Reach, float Keep, float Most, float Size = 0,
    (double X, double Y)? Toward = null);

/// <summary>A view whose lights the lightmap draws (<c>LightView.Shining</c>): its lights near the view this frame.</summary>
public interface ILightSource
{
    IReadOnlyList<LightSource> Lights { get; }
}

/// <summary>What the lightmap's shader reads: the walls and doors light stops at, and each light it draws.</summary>
public static class Sources
{
    /// <summary>
    /// How the shader tells a cell: open, a closed door (its panel up and down blocks), a wall, a clear wall light passes dimmed
    /// (2026-10-29-clear-glass); a doorway two cells thick (0104), its far column's own, so its 16 px panel stops light where it is
    /// drawn. A hatch is a floor and lets light through (0111), so no hatch byte is written; the shader still knows one.
    /// </summary>
    public const byte Open = 0, Clear = 32, Hatch = 64, HatchFar = 96, Door = 128, DoorFar = 160, Wall = 255;

    /// <summary>Floats an instance takes in the multimesh: a 2D transform, a colour, custom data.</summary>
    public const int Stride = 16;

    /// <summary>
    /// A byte a cell of <paramref name="box"/> of <paramref name="structure"/>, the grid's layer, which <paramref name="structureAt"/>
    /// reads too, a grid <paramref name="width"/> wide, row by row into <paramref name="cells"/>: what stops light there, a wall's
    /// <paramref name="material"/> whether it is clear. Most cells of a planet are open or rock, read straight from the layers; a
    /// door's shape is found from its neighbours.
    /// </summary>
    public static void Occupy(ReadOnlySpan<int> structure, ReadOnlySpan<int> material, Func<int, int> structureAt, int width, Box box, byte[] cells)
    {
        int i = 0;
        for (int y = box.Top; y <= box.Bottom; y++)
        {
            int cell = y * width + box.Left;
            for (int end = cell + box.Width; cell < end; cell++)
            {
                int s = structure[cell];
                cells[i++] = !Structures.Is(s, Trait.BlocksLight) ? Open
                    : !Doorways.IsDoor(s) ? WallOf(material[cell]) : DoorOf(structureAt, width, structure.Length, cell);
            }
        }
    }

    /// <summary>What stops light in <paramref name="cell"/>: a closed door's panel by the doorway's shape (0067), a wall, clear by its <paramref name="materialAt"/>.</summary>
    public static byte Of(Func<int, int> structureAt, Func<int, int> materialAt, int width, int size, int cell)
    {
        int structure = structureAt(cell);
        if (!Structures.Is(structure, Trait.BlocksLight)) return Open;
        // A shut door's panel stops light where it is drawn; anything else that stops it, a shut hatch filling its row too (0111), as a wall does.
        return !Doorways.IsDoor(structure) ? WallOf(materialAt(cell)) : DoorOf(structureAt, width, size, cell);
    }

    /// <summary>A closed door's byte in <paramref name="cell"/>: its doorway's near column's or far column's (0104).</summary>
    private static byte DoorOf(Func<int, int> structureAt, int width, int size, int cell)
    {
        var spot = Render.DoorShape.SpotOf(cell, width, c => Doorways.LinksOf(c, width, size, structureAt), hatch: false);
        return spot.Across % Doorways.Thick == 1 ? DoorFar : Door;
    }

    /// <summary>A wall of <paramref name="material"/>, by its code: one light passes dimmed if it is clear (glass), else one it stops at.</summary>
    private static byte WallOf(int material) => Materials.ByCode(material)?.Clear == true ? Clear : Wall;

    /// <summary>
    /// Whether the point (<paramref name="x"/>, <paramref name="y"/>), in pixels of the world, <paramref name="cell"/> pixels a
    /// cell, lies where light stops: in a wall or a closed doorway. Past the grid it is open.
    /// </summary>
    public static bool Blocks(Func<int, int> structureAt, int width, int height, int cell, double x, double y)
    {
        int cx = (int)Math.Floor(x / cell), cy = (int)Math.Floor(y / cell);
        if (cx < 0 || cy < 0 || cx >= width || cy >= height) return false;
        int at = cy * width + cx;
        return Structures.Is(structureAt(at), Trait.BlocksLight);
    }

    /// <summary>
    /// A helmet lamp's two lights (0077): its beam the way it points and a faint glow round it, both from its
    /// <paramref name="bulb"/>, the lamp's lens. A light in a wall lights nothing in the open, and the lamp pokes into a wall
    /// the hero stands under or against: then both shine from the first open point back toward the <paramref name="neck"/>,
    /// inside the hero, so never in a wall.
    /// </summary>
    public static (LightSource Beam, LightSource Glow) HeadLamp(
        (double X, double Y) bulb, (double X, double Y) toward, (double X, double Y) neck, Func<double, double, bool> blocks)
    {
        var at = bulb;
        for (int i = 1; i <= LampBackSteps && blocks(at.X, at.Y); i++)
        {
            double t = (double)i / LampBackSteps;
            at = (bulb.X + (neck.X - bulb.X) * t, bulb.Y + (neck.Y - bulb.Y) * t);
        }
        return (
            new LightSource(at.X, at.Y, L.HeadLamp, 1, L.HeadLampReach, L.HeadLampKeep, L.LampMost, L.HeadLampSize, toward),
            new LightSource(at.X, at.Y, L.HeadLamp, L.HeadGlow, L.HeadGlowReach, L.HeadGlowKeep, L.LampMost));
    }

    /// <summary>How finely a helmet lamp in a wall looks back toward the neck for open air: a pixel or two a step.</summary>
    private const int LampBackSteps = 8;

    /// <summary>
    /// Packs each of <paramref name="lights"/> whose reach touches <paramref name="view"/> (pixels) into
    /// <paramref name="buffer"/>, grown as needed, <paramref name="cell"/> pixels a cell: a quad over its
    /// reach, its colour with its strength, its reach, keep, most and size. Answers how many it packed.
    /// </summary>
    public static int Pack(IReadOnlyList<LightSource> lights, (double X, double Y, double W, double H) view, int cell, ref float[] buffer)
    {
        int n = 0;
        foreach (var l in lights)
        {
            double r = l.Reach * cell;
            if (l.Strength <= 0 || l.X + r <= view.X || l.X - r >= view.X + view.W || l.Y + r <= view.Y || l.Y - r >= view.Y + view.H) continue;
            if (buffer.Length < (n + 1) * Stride) Array.Resize(ref buffer, Math.Max(Stride * 64, buffer.Length * 2));
            int k = n++ * Stride;
            // Godot's 2D transform rows: x.x, y.x, -, origin.x; x.y, y.y, -, origin.y. The quad is -1..1 across.
            buffer[k] = (float)r;
            buffer[k + 1] = 0;
            buffer[k + 2] = 0;
            buffer[k + 3] = (float)l.X;
            buffer[k + 4] = 0;
            buffer[k + 5] = (float)r;
            buffer[k + 6] = 0;
            buffer[k + 7] = (float)l.Y;
            (buffer[k + 8], buffer[k + 9], buffer[k + 10], buffer[k + 11]) = (l.Colour.R, l.Colour.G, l.Colour.B, l.Strength);
            // A beam's most is past 1: 2 and its heading as a share of a turn, which the shader reads back.
            float most = l.Toward is { } t ? (float)(2 + (Math.Atan2(t.Y, t.X) / (2 * Math.PI) + 1) % 1) : l.Most;
            (buffer[k + 12], buffer[k + 13], buffer[k + 14], buffer[k + 15]) = (l.Reach, l.Keep, most, l.Size);
        }
        return n;
    }
}
