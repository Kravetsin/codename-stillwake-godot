using System;
using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// What a cape hides (a prototype, <see cref="Cape"/>): the figure's parts are drawn through it, and while a cape is worn
/// nothing of them shows behind the cape's back edge from its top to its hem, an air pack, a jetpack, a gun's stock;
/// above and below the cape, and ahead of its edge, all of them show. A child of the figure's frame, so its edge is drawn
/// where the cape gives it.
/// </summary>
public partial class CapeMask(Cape cape) : Node2D
{
    /// <summary>Far enough round the figure, in pixels, for anything it holds or swings.</summary>
    private const float Reach = 120;

    /// <summary>Whether a cape is worn: the parts are cut to it; else drawn whole.</summary>
    public bool Cutting
    {
        get => ClipChildren != ClipChildrenMode.Disabled;
        set
        {
            ClipChildren = value ? ClipChildrenMode.Only : ClipChildrenMode.Disabled;
            QueueRedraw();
        }
    }

    /// <summary>Takes the cape's edge as it last moved.</summary>
    public void Refresh()
    {
        if (Cutting) QueueRedraw();
    }

    public override void _Draw()
    {
        if (!Cutting) return;
        var points = cape.Points;
        double top = double.MaxValue, hem = double.MinValue;
        for (int r = 0; r < Cape.Rows; r++)
        {
            double y = points[r * Cape.Cols + Cape.Cols - 1].Y;
            (top, hem) = (Math.Min(top, y), Math.Max(hem, y));
        }
        int from = (int)Math.Floor(top), to = (int)Math.Ceiling(hem);
        DrawRect(new Rect2(-Reach, -Reach, 2 * Reach, from + Reach), Colors.White);
        DrawRect(new Rect2(-Reach, to, 2 * Reach, Reach - to), Colors.White);
        // A row a pixel tall at a time, from the cape's back edge where it crosses the row (the farthest back where it
        // crosses more than once) forward.
        for (int y = from; y < to; y++)
        {
            double mid = y + 0.5, edge = double.MaxValue;
            for (int r = 1; r < Cape.Rows; r++)
            {
                var (a, b) = (points[(r - 1) * Cape.Cols + Cape.Cols - 1], points[r * Cape.Cols + Cape.Cols - 1]);
                if ((a.Y - mid) * (b.Y - mid) > 0 || a.Y == b.Y) continue;
                edge = Math.Min(edge, a.X + (b.X - a.X) * (mid - a.Y) / (b.Y - a.Y));
            }
            float x = edge == double.MaxValue ? -Reach : (float)edge;
            DrawRect(new Rect2(x, y, Reach - x, 1), Colors.White);
        }
    }
}
