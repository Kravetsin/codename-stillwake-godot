using System;
using Godot;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The appearance key (hero-skins), on the equipment's metal at its left edge, lit as the sort key is: a coat hanger. A press
/// turns the equipment's screen to the skins (Appearance) and back; its tip, set by its panel, says which.
/// </summary>
public partial class LooksButton(Vector2 size, Action pressed) : SortButton(size, pressed)
{
    /// <summary>The hanger's hook, neck and shoulders down to its bar, in the glyph's 14 by 10 pixels; <paramref name="grow"/> wider for its glow.</summary>
    protected override void Glyph(Vector2 at, Color ink, float grow)
    {
        float w = 1.4f + 2 * grow;
        DrawArc(at + new Vector2(7, 1.6f), 1.5f, Mathf.Pi, Mathf.Tau * 1.15f, 10, ink, w, true);
        DrawLine(at + new Vector2(8.4f, 2.2f), at + new Vector2(7, 3.6f), ink, w, true);
        DrawPolyline([at + new Vector2(0.8f, 9), at + new Vector2(7, 3.6f), at + new Vector2(13.2f, 9), at + new Vector2(0.8f, 9)], ink, w, true);
    }
}
