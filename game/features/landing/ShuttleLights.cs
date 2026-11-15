using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// Each shuttle's slits, without a word: slit i lit warm while place i is taken, its rider never seen behind the frost. A
/// placeholder till the lit slits are picked: a warm bar over each slit. The seats are compared every frame, a handful at most,
/// and the slits drawn again only when they change.
/// </summary>
public partial class ShuttleLights : Node2D
{
    /// <summary>The slits' left edges in the shuttle's picture, in pixels from its corner, slit i place i (<c>tools/anime-art/subjects/shuttle.js</c>).</summary>
    private static readonly float[] Slits = [99.4f, 115.8f, 132.2f, 148.6f];
    private static readonly Vector2 SlitSize = new(13, 4.6f);
    private const float SlitTop = 23.6f;
    private static readonly Color Warm = new(1f, 0.74f, 0.38f, 0.9f);

    private readonly IGridQueries _grid;
    private readonly LandingModule _landing;
    private Seat[] _seen = [];

    public ShuttleLights(IGridQueries grid, LandingModule landing)
    {
        _grid = grid;
        _landing = landing;
        ZIndex = Layers.Screens;
        // Lit from inside: the slits glow in the dark as in the lamp's light.
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    public override void _Process(double delta)
    {
        if (_landing.Seats.SequenceEqual(_seen)) return;
        _seen = [.. _landing.Seats];
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var seat in _seen)
        {
            if (_grid.MachineById(seat.Pod) is not { } shuttle || Stillwake.Content.Machines.Get(shuttle.Kind).Riders == 1 || seat.Place >= Slits.Length) continue;
            // Over the shuttle's picture, which rises over its footprint as a tall machine's does (0099).
            var corner = new Vector2(shuttle.Cell % _grid.Width, shuttle.Cell / _grid.Width - ShipTiles.RiseOf(shuttle.Kind)) * ShipCamera.Cell;
            DrawRect(new Rect2(corner + new Vector2(Slits[seat.Place], SlitTop), SlitSize), Warm);
        }
    }
}
