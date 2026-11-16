using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// The helmet lamp as drawn on a helmet (2026-10-27-suits-are-worn): a torch on a clamp, the same picture on the hero and
/// on its card, fixed at the helmet's mount; its lens is where the light leaves.
/// </summary>
public static class HeadLampArt
{
    /// <summary>The lamp's picture, from the side, facing +x.</summary>
    public static Texture2D Picture => PixelArt.Picture("hero_headLamp");

    /// <summary>Where the picture's top left sits on the basic helmet's picture, in its pixels: on its brow.</summary>
    public static Vector2 On => OnOf(null);

    /// <summary>Where it sits on <paramref name="helmet"/>'s picture, a helmet's or a skin's picture id (<see cref="LampMounts"/>).</summary>
    public static Vector2 OnOf(string? helmet)
    {
        var (x, y) = LampMounts.Of(helmet);
        return new Vector2((float)x, (float)y);
    }

    /// <summary>The lens's middle on the lamp's picture, in its pixels: one pixel wide, two high, at its front.</summary>
    public static readonly Vector2 Lens = new(5.5f, 2);
}
