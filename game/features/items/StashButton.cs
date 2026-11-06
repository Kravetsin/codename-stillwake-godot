using System;
using Godot;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The stash key (2026-11-06-quick-stash), under the bag's sort key and lit as it is: a box and three arrows fanning out of
/// it, the glyph picked. A press stashes the bag into the crates in reach; its tip, set by its panel, says how.
/// </summary>
public partial class StashButton(Vector2 size, Action pressed) : SortButton(size, pressed)
{
    /// <summary>Each arrow's foot, at the box, and tip, in the glyph's 14 by 10 pixels.</summary>
    private static readonly (Vector2 From, Vector2 Tip)[] Arrows =
        [(new(5, 3.6f), new(12.4f, 0.2f)), (new(5, 5), new(14, 5)), (new(5, 6.4f), new(12.4f, 9.8f))];

    /// <summary>The box at the left and the arrows out of it; <paramref name="grow"/> wider for its glow.</summary>
    protected override void Glyph(Vector2 at, Color ink, float grow)
    {
        DrawRect(new Rect2(at.X, at.Y + 2, 4, 6).Grow(grow), ink);
        foreach (var (from, tip) in Arrows)
        {
            var along = (tip - from).Normalized();
            DrawLine(at + from, at + tip - along * 2.6f, ink, 1.6f + 2 * grow, true);
            var foot = tip - along * 3.4f;
            var side = new Vector2(-along.Y, along.X) * 2;
            Vector2[] head = [at + tip, at + foot + side, at + foot - side];
            DrawColoredPolygon(head, ink);
            if (grow > 0) DrawPolyline([.. head, head[0]], ink, 2 * grow, true);
        }
    }
}
