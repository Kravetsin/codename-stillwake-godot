using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// The light of every lamp, fire and glowing particle cell by cell, unshadowed, summed: lights add, so
/// a fire warms what a lamp lights. <c>render/Lit.cs</c> stops each colour of the sum at whole, as the
/// TypeScript game's <c>min(1, ambient + light)</c> did, and shares that out among Godot's lights by
/// what each gives of the sum, rather than adding them past a surface's own colour. Cells from
/// (<paramref name="x"/>, <paramref name="y"/>), <paramref name="width"/> by <paramref name="height"/>,
/// red, green, blue and the glowing particles' spots a cell, as a texture of floats takes them.
/// </summary>
public sealed class LightField(int x, int y, int width, int height)
{
    /// <summary>A light's spread is worked out for where in its cell it stands to an eighth of a cell.</summary>
    private const int Steps = 8;

    public int X { get; } = x;
    public int Y { get; } = y;
    public int Width { get; } = width;
    public int Height { get; } = height;

    /// <summary>
    /// All the light, four numbers a cell, row by row: red, green, blue, and the spots of glowing particles
    /// summed apart, their brightest colour, which <c>Lit.cs</c> keeps from adding up past its most.
    /// </summary>
    public float[] Light { get; } = new float[width * height * 4];

    /// <summary>A light's spread over the cells round it, and each row's first and last lit cell.</summary>
    private sealed record Kernel(int Across, float[] Light, int[] From, int[] To);

    private static readonly Dictionary<((float, float, float) Colour, float Most, int Reach, float Keep, int Fx, int Fy), Kernel> Kernels = new();

    /// <summary>
    /// How much of a light's colour is left <paramref name="d"/> cells off: <paramref name="keep"/> a cell,
    /// as the TypeScript game spread it, faded to nothing over the last cells of <paramref name="reach"/>
    /// (four, or half of a short reach).
    /// </summary>
    public static float Falloff(float d, int reach, float keep) =>
        MathF.Pow(keep, d) * Math.Clamp((reach - d) / Math.Min(4f, reach / 2f), 0, 1);

    /// <summary>
    /// The light of <paramref name="colour"/> <paramref name="d"/> cells off at full strength: each colour
    /// clamped at <paramref name="most"/>, so a lamp is white by the bulb and warm further off.
    /// </summary>
    public static (float R, float G, float B) At((float R, float G, float B) colour, float most, float d, int reach, float keep)
    {
        float f = Falloff(d, reach, keep);
        return (Math.Min(most, colour.R * f), Math.Min(most, colour.G * f), Math.Min(most, colour.B * f));
    }

    public void Clear() => Array.Clear(Light);

    public void CopyFrom(LightField other) => Array.Copy(other.Light, Light, Light.Length);

    /// <summary>
    /// Adds a light at (<paramref name="cx"/>, <paramref name="cy"/>), in cells, of <paramref name="colour"/>
    /// at <paramref name="most"/> a colour, spread as <see cref="At"/> out to <paramref name="reach"/> cells, times <paramref name="strength"/>
    /// (how bright it shines, tinted), keeping <paramref name="keep"/> of itself a cell it crosses; worked
    /// out at each cell's middle. A <paramref name="spot"/> adds its brightest colour to the spots alone.
    /// </summary>
    public void Add(float cx, float cy, (float R, float G, float B) colour, float most, (float R, float G, float B) strength, int reach, float keep, bool spot = false)
    {
        int left = (int)MathF.Floor(cx) - reach, top = (int)MathF.Floor(cy) - reach;
        var kernel = KernelOf(colour, most, reach, keep, (int)((cx - MathF.Floor(cx)) * Steps), (int)((cy - MathF.Floor(cy)) * Steps));
        var times = spot
            ? new Vector4(0, 0, 0, Math.Max(strength.R, Math.Max(strength.G, strength.B)))
            : new Vector4(strength.R, strength.G, strength.B, 0);
        int y0 = Math.Max(Y, top), y1 = Math.Min(Y + Height, top + kernel.Across);
        for (int gy = y0; gy < y1; gy++)
        {
            int row = gy - top;
            int x0 = Math.Max(X, left + kernel.From[row]), x1 = Math.Min(X + Width, left + kernel.To[row]);
            if (x0 >= x1) continue;
            int n = (x1 - x0) * 4, at = ((gy - Y) * Width + x0 - X) * 4;
            var from = MemoryMarshal.Cast<float, Vector4>(kernel.Light.AsSpan((row * kernel.Across + x0 - left) * 4, n));
            var sum = MemoryMarshal.Cast<float, Vector4>(Light.AsSpan(at, n));
            for (int k = 0; k < from.Length; k++) sum[k] += from[k] * times;
        }
    }

    /// <summary>The spread of a light standing <paramref name="fx"/>, <paramref name="fy"/> eighths into its cell, worked out once.</summary>
    private static Kernel KernelOf((float R, float G, float B) colour, float most, int reach, float keep, int fx, int fy)
    {
        var key = (colour, most, reach, keep, fx, fy);
        if (Kernels.TryGetValue(key, out var kept)) return kept;
        int across = 2 * reach + 1;
        var light = new float[across * across * 4];
        var from = new int[across];
        var to = new int[across];
        float cx = reach + (float)fx / Steps, cy = reach + (float)fy / Steps;
        for (int j = 0; j < across; j++)
        {
            from[j] = across;
            for (int i = 0; i < across; i++)
            {
                float dx = i + 0.5f - cx, dy = j + 0.5f - cy, d = MathF.Sqrt(dx * dx + dy * dy);
                if (d >= reach) continue;
                int k = (j * across + i) * 4;
                (light[k], light[k + 1], light[k + 2]) = At(colour, most, d, reach, keep);
                light[k + 3] = Math.Max(light[k], Math.Max(light[k + 1], light[k + 2]));
                from[j] = Math.Min(from[j], i);
                to[j] = i + 1;
            }
        }
        return Kernels[key] = new Kernel(across, light, from, to);
    }
}
