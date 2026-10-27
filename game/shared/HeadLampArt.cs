using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// The helmet lamp as drawn on a helmet (2026-10-27-suits-are-worn): a torch on a clamp, the same picture on the hero and
/// on its card, fixed at the helmet's mount; its lens is where the light leaves.
/// </summary>
public static class HeadLampArt
{
    /// <summary>The lamp's picture, from the side, facing +x.</summary>
    public static Texture2D Picture => GD.Load<Texture2D>("res://art/hero_headLamp.png");

    /// <summary>Where the picture's top left sits on the helmet's picture, in the helmet's pixels: on its brow.</summary>
    public static readonly Vector2 On = new(8, -1);

    /// <summary>The lens's middle on the lamp's picture, in its pixels: one pixel wide, two high, at its front.</summary>
    public static readonly Vector2 Lens = new(5.5f, 2);
}
