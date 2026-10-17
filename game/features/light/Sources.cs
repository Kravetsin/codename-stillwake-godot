using System;
using System.Collections.Generic;
using Stillwake.Sim;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// One light as the lightmap draws it, in pixels of the world: <see cref="Colour"/> falling off by
/// <see cref="Keep"/> a cell out to <see cref="Reach"/> cells, each colour at most <see cref="Most"/>, then
/// times <see cref="Strength"/> — a lamp's flicker, a fire's flame, an ember's fade; <see cref="Size"/>
/// pixels across, which softens its shadows' edges, or a point at 0.
/// </summary>
public readonly record struct LightSource(double X, double Y, (float R, float G, float B) Colour, float Strength, int Reach, float Keep, float Most, float Size = 0);

/// <summary>What the lightmap's shader reads: the walls and doors light stops at, and each light it draws.</summary>
public static class Sources
{
    /// <summary>How the shader tells a cell: open, a closed door (its panel blocks), a wall.</summary>
    public const byte Open = 0, Door = 128, Wall = 255;

    /// <summary>Floats an instance takes in the multimesh: a 2D transform, a colour, custom data.</summary>
    public const int Stride = 16;

    /// <summary>
    /// A byte a cell of <paramref name="box"/> of <paramref name="structure"/>, a grid <paramref name="width"/>
    /// wide, row by row into <paramref name="cells"/>: what stops light there.
    /// </summary>
    public static void Occupy(ReadOnlySpan<int> structure, int width, Box box, byte[] cells)
    {
        int i = 0;
        for (int y = box.Top; y <= box.Bottom; y++)
            foreach (int code in structure.Slice(y * width + box.Left, box.Width))
                cells[i++] = Of(code);
    }

    /// <summary>What stops light in a cell of structure <paramref name="code"/>.</summary>
    public static byte Of(int code) => code switch
    {
        Structure.Wall => Wall,
        Structure.DoorClosed => Door,
        _ => Open,
    };

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
            (buffer[k + 12], buffer[k + 13], buffer[k + 14], buffer[k + 15]) = (l.Reach, l.Keep, l.Most, l.Size);
        }
        return n;
    }
}
