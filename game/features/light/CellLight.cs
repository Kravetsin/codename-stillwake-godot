using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// The light of the cells mode (0026), a colour a cell over a stretch of the world, as the TypeScript
/// game worked it out every frame (cells.ts, scene.ts): <see cref="Begin"/> reads what each cell is and
/// lets the stars in, emitters <see cref="Give(EmitterMask, Matrix3x2, Vector4)"/> their glowing pixels'
/// light to the cells they fall in, <see cref="Spread"/> sweeps it on, and <see cref="Finish"/> leaves it
/// in <see cref="Texels"/> as a texture of floats takes it. Its buffers are kept while the stretch keeps
/// its size: a frame allocates nothing.
/// </summary>
/// <param name="cell">A cell's size in pixels of the world.</param>
public sealed class CellLight(int cell)
{
    public int X { get; private set; }
    public int Y { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    /// <summary>What each cell is to light (<see cref="Sweeps"/>), row by row.</summary>
    public byte[] Kind { get; private set; } = [];

    /// <summary>The cells' light as bytes, sixteen a cell: red, green, blue and a fourth float, row by row.</summary>
    public byte[] Texels { get; private set; } = [];

    /// <summary>
    /// Each cell's light: what it gives off until <see cref="Spread"/>, then what reaches it; after
    /// <see cref="Finish"/>, the light on a blocking cell's face and whether it blocks.
    /// </summary>
    public Span<Vector4> Light => MemoryMarshal.Cast<byte, Vector4>(Texels.AsSpan());

    // A mask's glowing pixels counted a cell, and the cells they fell in, cleared after each mask.
    private int[] _count = [], _touched = [];

    /// <summary>
    /// Starts the light of the cells from (<paramref name="x"/>, <paramref name="y"/>), <paramref name="width"/>
    /// by <paramref name="height"/>: each cell's kind read from <paramref name="grid"/>, and the stars shining
    /// into every open cell with no back wall and past the grid's edge.
    /// </summary>
    public void Begin(IGridQueries grid, int x, int y, int width, int height)
    {
        (X, Y, Width, Height) = (x, y, width, height);
        int n = width * height;
        if (Kind.Length != n) (Kind, Texels, _count) = (new byte[n], new byte[n * 16], new int[n]);
        var ((r, g, b), strength, _) = L.Emits.Star;
        Sweeps.Read(grid, x, y, width, height, Kind, Light, new Vector4(r, g, b, 0) * strength);
    }

    /// <summary>Adds <paramref name="light"/> to the cell holding the world point (<paramref name="wx"/>, <paramref name="wy"/>), in pixels.</summary>
    public void Give(float wx, float wy, Vector4 light)
    {
        int cx = (int)MathF.Floor(wx / cell) - X, cy = (int)MathF.Floor(wy / cell) - Y;
        if (cx < 0 || cy < 0 || cx >= Width || cy >= Height) return;
        Light[cy * Width + cx] += light;
    }

    /// <summary>
    /// Adds the light of an emitter whose glowing pixels are <paramref name="mask"/>'s, laid in the world by
    /// <paramref name="toWorld"/> (a picture's pixels to the world's): <paramref name="light"/> in the cell
    /// most of them fall in, and to each other cell they fall in its share by their count there.
    /// </summary>
    public void Give(EmitterMask mask, Matrix3x2 toWorld, Vector4 light)
    {
        if (_touched.Length < mask.Count) _touched = new int[mask.Count];
        var pixels = mask.Pixels;
        int touched = 0, most = 0;
        for (int k = 0; k < pixels.Length; k += 2)
        {
            var at = Vector2.Transform(new Vector2(pixels[k], pixels[k + 1]), toWorld);
            int cx = (int)MathF.Floor(at.X / cell) - X, cy = (int)MathF.Floor(at.Y / cell) - Y;
            if (cx < 0 || cy < 0 || cx >= Width || cy >= Height) continue;
            int i = cy * Width + cx;
            if (_count[i]++ == 0) _touched[touched++] = i;
            most = Math.Max(most, _count[i]);
        }
        var cells = Light;
        for (int t = 0; t < touched; t++)
        {
            int i = _touched[t];
            cells[i] += light * ((float)_count[i] / most);
            _count[i] = 0;
        }
    }

    /// <summary>Spreads what the cells give off to the rest (<see cref="Sweeps.Spread"/>).</summary>
    public void Spread() => Sweeps.Spread(Light, Kind, Width, Height);

    /// <summary>
    /// Readies the light for the lit shader: a blocking cell's fourth number whole, the rest nothing, and its
    /// light that on its face, for its picture takes <see cref="L.WallIn"/> of it.
    /// </summary>
    public void Finish()
    {
        var cells = Light;
        var face = new Vector4(1 / L.WallIn, 1 / L.WallIn, 1 / L.WallIn, 0);
        for (int i = 0; i < cells.Length; i++)
        {
            if (Sweeps.Blocks(Kind[i])) cells[i] = cells[i] * face + Vector4.UnitW;
            else cells[i].W = 0;
        }
    }
}
