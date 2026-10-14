using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// The light of every lamp, fire and glowing particle cell by cell, unshadowed: all of it summed, and
/// the brightest of it. <c>render/Lit.cs</c> lets a spot show the brightest, as the TypeScript game did
/// where lights met, each colour at most whole, and shares that out among Godot's lights by what each
/// gives of the sum, rather than adding them past a surface's own colour. Cells from
/// (<paramref name="x"/>, <paramref name="y"/>), <paramref name="width"/> by <paramref name="height"/>,
/// red, green, blue and a fourth number unused a cell, as a texture of floats takes them; light keeps
/// <paramref name="air"/> of itself a cell it crosses (<c>Light.Air</c>).
/// </summary>
public sealed class LightField(int x, int y, int width, int height, float air)
{
    /// <summary>A light's spread is worked out for where in its cell it stands to an eighth of a cell.</summary>
    private const int Steps = 8;

    public int X { get; } = x;
    public int Y { get; } = y;
    public int Width { get; } = width;
    public int Height { get; } = height;

    /// <summary>All the light, four numbers a cell (red, green, blue, unused), row by row.</summary>
    public float[] Light { get; } = new float[width * height * 4];

    /// <summary>The brightest of the lights in each colour, as <see cref="Light"/> holds them.</summary>
    public float[] Brightest { get; } = new float[width * height * 4];

    /// <summary>A light's spread over the cells round it, and each row's first and last lit cell.</summary>
    private sealed record Kernel(int Across, float[] Light, int[] From, int[] To);

    private static readonly Dictionary<((float, float, float) Colour, int Reach, float Air, int Fx, int Fy), Kernel> Kernels = new();

    /// <summary>
    /// How much of a light's colour is left <paramref name="d"/> cells off: <paramref name="air"/> a cell,
    /// as the TypeScript game spread it, faded to nothing over the last cells of <paramref name="reach"/>.
    /// </summary>
    public static float Falloff(float d, int reach, float air) => MathF.Pow(air, d) * Math.Clamp((reach - d) / 4f, 0, 1);

    /// <summary>
    /// The light of <paramref name="colour"/> <paramref name="d"/> cells off at full strength: each colour
    /// clamped at whole, so a lamp is white by the bulb and warm further off.
    /// </summary>
    public static (float R, float G, float B) At((float R, float G, float B) colour, float d, int reach, float air)
    {
        float f = Falloff(d, reach, air);
        return (Math.Min(1, colour.R * f), Math.Min(1, colour.G * f), Math.Min(1, colour.B * f));
    }

    public void Clear()
    {
        Array.Clear(Light);
        Array.Clear(Brightest);
    }

    public void CopyFrom(LightField other)
    {
        Array.Copy(other.Light, Light, Light.Length);
        Array.Copy(other.Brightest, Brightest, Brightest.Length);
    }

    /// <summary>
    /// Adds a light at (<paramref name="cx"/>, <paramref name="cy"/>), in cells, of <paramref name="colour"/>
    /// spread as <see cref="At"/> out to <paramref name="reach"/> cells, times <paramref name="strength"/>
    /// (how bright it shines, tinted); worked out at each cell's middle.
    /// </summary>
    public void Add(float cx, float cy, (float R, float G, float B) colour, (float R, float G, float B) strength, int reach)
    {
        int left = (int)MathF.Floor(cx) - reach, top = (int)MathF.Floor(cy) - reach;
        var kernel = KernelOf(colour, reach, (int)((cx - MathF.Floor(cx)) * Steps), (int)((cy - MathF.Floor(cy)) * Steps));
        var times = new Vector4(strength.R, strength.G, strength.B, 0);
        int y0 = Math.Max(Y, top), y1 = Math.Min(Y + Height, top + kernel.Across);
        for (int gy = y0; gy < y1; gy++)
        {
            int row = gy - top;
            int x0 = Math.Max(X, left + kernel.From[row]), x1 = Math.Min(X + Width, left + kernel.To[row]);
            if (x0 >= x1) continue;
            int n = (x1 - x0) * 4, at = ((gy - Y) * Width + x0 - X) * 4;
            var from = MemoryMarshal.Cast<float, Vector4>(kernel.Light.AsSpan((row * kernel.Across + x0 - left) * 4, n));
            var sum = MemoryMarshal.Cast<float, Vector4>(Light.AsSpan(at, n));
            var most = MemoryMarshal.Cast<float, Vector4>(Brightest.AsSpan(at, n));
            for (int k = 0; k < from.Length; k++)
            {
                var light = from[k] * times;
                sum[k] += light;
                most[k] = Vector4.Max(most[k], light);
            }
        }
    }

    /// <summary>The spread of a light standing <paramref name="fx"/>, <paramref name="fy"/> eighths into its cell, worked out once.</summary>
    private Kernel KernelOf((float R, float G, float B) colour, int reach, int fx, int fy)
    {
        var key = (colour, reach, air, fx, fy);
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
                (light[k], light[k + 1], light[k + 2]) = At(colour, d, reach, air);
                from[j] = Math.Min(from[j], i);
                to[j] = i + 1;
            }
        }
        return Kernels[key] = new Kernel(across, light, from, to);
    }
}
