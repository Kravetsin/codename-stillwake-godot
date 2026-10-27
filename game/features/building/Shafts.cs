using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// The shafts launch platforms keep clear for the pod's fall (the descent's), seen while building on the plan: under every
/// platform built or planned a red hatch down to the grid's edge, where the sim refuses anything solid (<c>grid.underPlatform</c>);
/// under a platform card's ghost its own shaft, green where empty and red where something stands (<c>grid.shaftBlocked</c>).
/// Laid again only when what it shows changes.
/// </summary>
public partial class Shafts : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private static readonly Color Kept = new(0.94f, 0.27f, 0.27f, 0.22f), Hatch = new(0.98f, 0.45f, 0.45f, 0.7f),
        Clear = new(0.29f, 0.87f, 0.5f, 0.22f), Blocked = new(0.94f, 0.27f, 0.27f, 0.5f);

    private IGridQueries? _grid;
    private int _version = -1;
    private int[] _kept = [], _ghost = [];

    public Shafts()
    {
        ZIndex = Layers.PlanGhost;
        // Unlit, as the plan is: it reads in the dark under the hull.
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    /// <summary>
    /// Shows the shafts under <paramref name="kept"/>, the platform cells built and planned, and under <paramref name="ghost"/>, those
    /// of the platform card in hand; nothing with no grid.
    /// </summary>
    public void Show(IGridQueries? grid, IEnumerable<int> kept, IEnumerable<int> ghost)
    {
        int[] k = [.. kept.Distinct().Order()], g = [.. ghost.Distinct().Order()];
        int version = grid?.Version ?? -1;
        if (grid == _grid && version == _version && k.SequenceEqual(_kept) && g.SequenceEqual(_ghost)) return;
        (_grid, _version, _kept, _ghost) = (grid, version, k, g);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_grid is not { } grid) return;
        int width = grid.Width, bottom = grid.Height * Cell;
        foreach (int top in _kept)
        {
            int x = top % width * Cell, y = (top / width + 1) * Cell;
            DrawRect(new Rect2(x, y, Cell, bottom - y), Kept);
            // A hatch of one stroke a cell, so neighbouring columns read as one band.
            for (int at = y; at < bottom; at += Cell) DrawLine(new Vector2(x, at + Cell), new Vector2(x + Cell, at), Hatch, 1);
        }
        foreach (int top in _ghost)
            for (int c = top + width; c < width * grid.Height; c += width)
                DrawRect(new Rect2(c % width * Cell, c / width * Cell, Cell, Cell), grid.Taken(c) ? Blocked : Clear);
    }
}
