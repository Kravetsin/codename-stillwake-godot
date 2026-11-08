using Godot;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The favourite mark (2026-11-06-quick-stash), the outline picked: a thin pale star on a dark edge with a sky glow. In a slot's
/// corner on every stack of an item kept in the bag, and by the pointer while Alt is held in the open inventory.
/// </summary>
public partial class FavouriteStar : Control
{
    private static readonly Color Edge = new(4 / 255f, 7 / 255f, 13 / 255f, 0.7f), Sky = new("38bdf8"), Pale = new("e0f2fe");

    private readonly Vector2[] _points = new Vector2[11];

    /// <param name="radius">Its points' reach from its middle, pixels: 5 in a slot, 6.3 by the pointer.</param>
    public FavouriteStar(float radius)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        float side = 2 * radius + 4;
        Size = new Vector2(side, side);
        var middle = Size / 2;
        for (int i = 0; i < 10; i++)
        {
            float a = -Mathf.Pi / 2 + i * Mathf.Pi / 5, r = i % 2 == 1 ? radius * 0.45f : radius;
            _points[i] = middle + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }
        _points[10] = _points[0];
    }

    public override void _Draw()
    {
        DrawPolyline(_points, Edge, 3, true);
        DrawPolyline(_points, Sky with { A = 0.14f }, 3.6f, true);
        DrawPolyline(_points, Sky with { A = 0.34f }, 2.2f, true);
        DrawPolyline(_points, Pale, 1.05f, true);
    }
}
