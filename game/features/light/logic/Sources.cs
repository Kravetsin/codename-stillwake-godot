using System;
using System.Collections.Generic;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;
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
    /// How the shader tells a cell: open, a closed door (its panel up and down blocks), a wall; a doorway two cells thick (0104),
    /// its far column's own, so its 16 px panel stops light where it is drawn. A hatch is a floor and lets light through (0111), so
    /// no hatch byte is written; the shader still knows one.
    /// </summary>
    public const byte Open = 0, Hatch = 64, HatchFar = 96, Door = 128, DoorFar = 160, Wall = 255;

    /// <summary>Floats an instance takes in the multimesh: a 2D transform, a colour, custom data.</summary>
    public const int Stride = 16;

    /// <summary>
    /// A byte a cell of <paramref name="box"/> of <paramref name="structure"/>, the grid's layer, which <paramref name="structureAt"/>
    /// reads too, a grid <paramref name="width"/> wide, row by row into <paramref name="cells"/>: what stops light there. Most cells
    /// of a planet are open or rock, read straight from the layer; a door's shape is found from its neighbours.
    /// </summary>
    public static void Occupy(ReadOnlySpan<int> structure, Func<int, int> structureAt, int width, Box box, byte[] cells)
    {
        int i = 0;
        for (int y = box.Top; y <= box.Bottom; y++)
        {
            int cell = y * width + box.Left;
            for (int end = cell + box.Width; cell < end; cell++)
            {
                int s = structure[cell];
                cells[i++] = !Structures.Is(s, Trait.BlocksLight) ? Open : !Doorways.IsDoor(s) ? Wall : Of(structureAt, width, structure.Length, cell);
            }
        }
    }

    /// <summary>What stops light in <paramref name="cell"/>: a closed door's panel by the doorway's shape (0067), a wall.</summary>
    public static byte Of(Func<int, int> structureAt, int width, int size, int cell)
    {
        int structure = structureAt(cell);
        if (!Structures.Is(structure, Trait.BlocksLight)) return Open;
        // A shut door's panel stops light where it is drawn; anything else that stops it, a shut hatch filling its row too (0111), as a wall does.
        if (!Doorways.IsDoor(structure)) return Wall;
        var spot = Render.DoorShape.SpotOf(cell, width, c => Doorways.LinksOf(c, width, size, structureAt), hatch: false);
        return spot.Across % Doorways.Thick == 1 ? DoorFar : Door;
    }

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
    /// A helmet lamp's two lights (0077): its beam from the <paramref name="bulb"/> the way it points, and its glow round the
    /// head from halfway between the bulb and the <paramref name="neck"/> — inside the hero, so never in a wall, for a light in
    /// one lights nothing in the open, and the bulb pokes into a wall the hero stands against. The beam from there too then.
    /// </summary>
    public static (LightSource Beam, LightSource Glow) HeadLamp(
        (double X, double Y) bulb, (double X, double Y) toward, (double X, double Y) neck, Func<double, double, bool> blocks)
    {
        var head = ((bulb.X + neck.X) / 2, (bulb.Y + neck.Y) / 2);
        var beamAt = blocks(bulb.X, bulb.Y) ? head : bulb;
        return (
            new LightSource(beamAt.Item1, beamAt.Item2, L.HeadLamp, 1, L.HeadLampReach, L.HeadLampKeep, L.LampMost, L.HeadLampSize, toward),
            new LightSource(head.Item1, head.Item2, L.HeadLamp, L.HeadGlow, L.HeadGlowReach, L.HeadGlowKeep, L.LampMost));
    }

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
