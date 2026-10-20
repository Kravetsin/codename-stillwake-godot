using Godot;

namespace Stillwake.Game.Render;

/// <summary>The soft round pictures luminous things are drawn with (0075), white to tint, drawn with linear filtering.</summary>
public static class SoftDots
{
    /// <summary>A fine dot: a white core fading out. A mote, a speck of air.</summary>
    public static readonly GradientTexture2D Dot = Radial([0f, 0.22f, 0.55f, 1f], [1f, 0.9f, 0.22f, 0f], 32);

    /// <summary>A soft puff, for smoke and vapour.</summary>
    public static readonly GradientTexture2D Puff = Radial([0f, 0.35f, 0.7f, 1f], [1f, 0.62f, 0.18f, 0f], 64);

    private static GradientTexture2D Radial(float[] offsets, float[] alphas, int size)
    {
        var colours = new Color[alphas.Length];
        for (int i = 0; i < alphas.Length; i++) colours[i] = new Color(1, 1, 1, alphas[i]);
        return new GradientTexture2D
        {
            Width = size,
            Height = size,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1f, 0.5f),
            Gradient = new Gradient { Offsets = offsets, Colors = colours },
        };
    }
}
