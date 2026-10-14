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
/// in <see cref="Texels"/> as a texture of floats takes it, and <see cref="Settle"/> works out where relief
/// is shaded from, steady over frames, in <see cref="Relief"/>. Its buffers are kept while the stretch keeps
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

    /// <summary>
    /// Where a normal map's relief takes its light from, a cell at a time (<see cref="Settle"/>): toward where
    /// the light grows, as long as 1 where it falls off as steeply as a lamp's in air; x and y, two floats a
    /// cell, row by row.
    /// </summary>
    public byte[] Relief { get; private set; } = [];

    public Span<Vector2> Toward => MemoryMarshal.Cast<byte, Vector2>(Relief.AsSpan());

    /// <summary>How steeply light falls off a cell, as a share of itself, to come from one side whole: a lamp's in air.</summary>
    private static readonly float Directional = 1 / -MathF.Log(L.Air);

    // A mask's glowing pixels counted a cell, and the cells they fell in, cleared after each mask.
    private int[] _count = [], _touched = [];
    // Each cell's brightness smoothed over frames for the relief, -1 where not yet known; and a spare to shift it into.
    private float[] _settled = [], _shifted = [];

    /// <summary>
    /// Starts the light of the cells from (<paramref name="x"/>, <paramref name="y"/>), <paramref name="width"/>
    /// by <paramref name="height"/>: each cell's kind read from <paramref name="grid"/>, and the stars shining
    /// into every open cell with no back wall and past the grid's edge.
    /// </summary>
    public void Begin(IGridQueries grid, int x, int y, int width, int height)
    {
        int n = width * height;
        if (Kind.Length != n || Width != width)
        {
            (Kind, Texels, _count, Relief) = (new byte[n], new byte[n * 16], new int[n], new byte[n * 8]);
            (_settled, _shifted) = (new float[n], new float[n]);
            _settled.AsSpan().Fill(-1);
        }
        else if (x != X || y != Y) Shift(x - X, y - Y);
        (X, Y, Width, Height) = (x, y, width, height);
        var ((r, g, b), strength, _) = L.Emits.Star;
        Sweeps.Read(grid, x, y, width, height, Kind, Light, new Vector4(r, g, b, 0) * strength);
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

    /// <summary>
    /// Works out <see cref="Toward"/> from the light as <see cref="Finish"/> left it, its brightness smoothed
    /// over frames — <paramref name="keep"/> of what it was, the rest of what it is now — so a fire's crackle
    /// and its flames moving between cells do not swing a picture's relief from side to side. A cell's slope
    /// is taken a cell either way, from cells of its own side only, as the lit shader blends them; only where it
    /// is drawn: past <paramref name="marginX"/> cells in from either side and <paramref name="marginY"/> from the
    /// top and bottom, less one for the shader blending a cell with the next.
    /// </summary>
    public void Settle(float keep, int marginX = 0, int marginY = 0)
    {
        var cells = Light;
        var settled = _settled.AsSpan(0, Width * Height);
        ReadOnlySpan<byte> kind = Kind;
        for (int i = 0; i < settled.Length; i++)
        {
            var c = cells[i];
            float now = 0.2126f * c.X + 0.7152f * c.Y + 0.0722f * c.Z;
            settled[i] = settled[i] < 0 ? now : now + (settled[i] - now) * keep;
        }
        var toward = Toward;
        int w = Width, h = Height;
        for (int j = Math.Max(0, marginY - 1); j < Math.Min(h, h - marginY + 1); j++)
            for (int k = Math.Max(0, marginX - 1), i = j * w + k; k < Math.Min(w, w - marginX + 1); k++, i++)
            {
                bool blocks = Sweeps.Blocks(kind[i]);
                float here = settled[i];
                // A neighbour of the other side, or past the stretch, counts as here: the slope from the rest.
                bool left = k > 0 && Sweeps.Blocks(kind[i - 1]) == blocks, right = k < w - 1 && Sweeps.Blocks(kind[i + 1]) == blocks;
                bool up = j > 0 && Sweeps.Blocks(kind[i - w]) == blocks, down = j < h - 1 && Sweeps.Blocks(kind[i + w]) == blocks;
                var slope = new Vector2(
                    ((right ? settled[i + 1] : here) - (left ? settled[i - 1] : here)) * Across[(left ? 1 : 0) + (right ? 1 : 0)],
                    ((down ? settled[i + w] : here) - (up ? settled[i - w] : here)) * Across[(up ? 1 : 0) + (down ? 1 : 0)]);
                float steep = slope.Length();
                toward[i] = steep > 0 ? slope * (MathF.Min(1, steep / MathF.Max(here, 1e-4f) * Directional) / steep) : Vector2.Zero;
            }
    }

    /// <summary>One over the cells a slope is taken across, by how many neighbours of its side a cell has that way.</summary>
    private static readonly float[] Across = [0, 1, 0.5f];

    /// <summary>The smoothed brightness kept as the stretch moves by (<paramref name="dx"/>, <paramref name="dy"/>) cells; unknown where it is new.</summary>
    private void Shift(int dx, int dy)
    {
        for (int j = 0, i = 0; j < Height; j++)
            for (int k = 0; k < Width; k++, i++)
            {
                int fx = k + dx, fy = j + dy;
                _shifted[i] = fx >= 0 && fy >= 0 && fx < Width && fy < Height ? _settled[fy * Width + fx] : -1;
            }
        (_settled, _shifted) = (_shifted, _settled);
    }
}
