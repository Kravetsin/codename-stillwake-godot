using System;
using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The sort key (0008, 0081): a key of the wrist computer's metal whose face shows three bars, longest at the top, and
/// an arrow down beside them, drawn in code; the glyph lights sky under the pointer, white while held, and glows a moment
/// after. The bag's stands on the display's right edge, a crate's in its header. A press sorts; what it does is said in
/// the one tooltip, set by its panel.
/// </summary>
public partial class SortButton : WristKey
{
    private static readonly Color Ink = new("8b98ad"), Afterglow = new("e0f2fe"), Hover = new("bae6fd"), Sky = new("38bdf8"), Held = new("7dd3fc");

    private readonly Action _pressed;

    /// <param name="size">The bag's tall side key, or a header's square keycap.</param>
    public SortButton(Vector2 size, Action pressed) : base(size) => _pressed = pressed;

    protected override void Pressed() => _pressed();

    protected override void DrawCap(Rect2 face)
    {
        Face(face);
        var (ink, glow, strength) = State switch
        {
            KeyState.Down => (Colors.White, Held, 1f),
            KeyState.Hover => (Hover, Sky, 0.6f),
            _ => (Ink.Lerp(Afterglow, After), Sky, After),
        };
        var at = new Vector2(Mathf.Round(face.Position.X + (face.Size.X - 14) / 2), Mathf.Round(face.Position.Y + (face.Size.Y - 10) / 2) + (State == KeyState.Down ? 1 : 0));
        if (strength > 0)
        {
            Glyph(at, glow with { A = 0.12f * strength }, 2);
            Glyph(at, glow with { A = 0.3f * strength }, 1);
        }
        Glyph(at, ink, 0);
    }

    /// <summary>Three bars, each shorter, and an arrow down at their right: what a sort makes of a jumble; <paramref name="grow"/> wider for its glow.</summary>
    private void Glyph(Vector2 at, Color ink, float grow)
    {
        float x = at.X, y = at.Y;
        DrawRect(new Rect2(x, y, 6, 2).Grow(grow), ink);
        DrawRect(new Rect2(x, y + 3, 4, 2).Grow(grow), ink);
        DrawRect(new Rect2(x, y + 6, 2, 2).Grow(grow), ink);
        DrawRect(new Rect2(x + 9, y, 2, 6).Grow(grow), ink);
        DrawColoredPolygon([new Vector2(x + 6.5f - grow, y + 6 - grow / 2), new Vector2(x + 13.5f + grow, y + 6 - grow / 2), new Vector2(x + 10, y + 9.5f + grow)], ink);
    }
}
