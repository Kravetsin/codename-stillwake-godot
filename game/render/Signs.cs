using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// The small signs the interface and the overlays share, as picked (the ink-signs ADR): glyphs a screen lights for power,
/// and the credit chip; drawn by <c>tools/anime-art</c> (<c>signs</c>, <c>credit</c>), sampled smoothly at any size.
/// </summary>
public static class Signs
{
    /// <summary>The power bolt: before a power number, and over a machine short of power; red with none.</summary>
    public static Texture2D Bolt(bool none = false) => PixelArt.Picture(none ? "sign_boltNone" : "sign_bolt");

    /// <summary>A plug hanging loose: over a machine that makes or draws power with no cable to it.</summary>
    public static Texture2D Unplugged() => PixelArt.Picture("sign_unplugged");

    /// <summary>A wrench in an orange frame over a machine a flare broke: it wants a hand, not power.</summary>
    public static Texture2D Broken() => PixelArt.Picture("sign_broken");

    /// <summary>The chip credits are written with, before every amount.</summary>
    public static Texture2D Credit() => PixelArt.Picture("credit");
}
