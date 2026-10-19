using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// A plane of the backdrop behind the ship (render/README.md): things scattered over its tiles by
/// <see cref="Scatter"/>, drawn once in the plane's own pixels and carried by this node's transform as
/// the camera moves and the plane drifts. It draws again only when the screen nears the edge of the
/// tiles drawn, or the zoom changes, so a frame costs it two properties. Unlit: no material of
/// <see cref="Lit"/>, on the stars' canvas layer, under the world.
/// </summary>
public abstract partial class DriftLayer : Node2D
{
    private readonly uint _seed;
    private readonly int _tile;
    private readonly float _mean, _parallax, _drift, _reach;
    private readonly List<Scattered> _spots = [];
    private (int X0, int Y0, int X1, int Y1) _drawn = (0, 0, -1, -1);
    private float _zoom;

    /// <param name="reach">How far, in the plane's pixels, a thing drawn may stand out of its tile.</param>
    protected DriftLayer(uint seed, int tile, float mean, float parallax, float drift, float reach) =>
        (_seed, _tile, _mean, _parallax, _drift, _reach) = (seed, tile, mean, parallax, drift, reach);

    /// <summary>The zoom drawn at: a pixel of the plane is this many of the screen's.</summary>
    protected float Zoom => _zoom;

    /// <summary>
    /// Lays the plane under a camera centred on <paramref name="camera"/> (pixels of the world) at
    /// <paramref name="zoom"/>, over a screen <paramref name="screen"/> wide, <paramref name="seconds"/>
    /// of drift on. Zoomed out past the player's farthest, as only a dev view does, it is drawn as there.
    /// </summary>
    public void Follow(Vector2 camera, float zoom, Vector2 screen, double seconds)
    {
        zoom = Mathf.Max(zoom, ShipCamera.FarthestZoom(screen));
        var centre = Scatter.Centre(camera.X, camera.Y, _parallax, _drift, seconds);
        double w = screen.X / 2 / zoom + _reach, h = screen.Y / 2 / zoom + _reach;
        var need = Scatter.Tiles(centre.X - w, centre.Y - h, centre.X + w, centre.Y + h, _tile);
        bool inside = need.X0 >= _drawn.X0 && need.Y0 >= _drawn.Y0 && need.X1 <= _drawn.X1 && need.Y1 <= _drawn.Y1;
        if (!inside || zoom != _zoom)
        {
            // A tile more each way, so a slow drift or a short pan draws nothing new.
            _drawn = (need.X0 - 1, need.Y0 - 1, need.X1 + 1, need.Y1 + 1);
            _zoom = zoom;
            QueueRedraw();
        }
        // Drawn from the first tile's corner, so the numbers stay small however far the plane has drifted.
        var at = Scatter.OnScreen((double)_drawn.X0 * _tile, (double)_drawn.Y0 * _tile, centre, zoom, screen.X, screen.Y);
        Position = new Vector2((float)at.X, (float)at.Y);
        Scale = new Vector2(zoom, zoom);
    }

    public override void _Draw()
    {
        _spots.Clear();
        Scatter.Over(_seed, _tile, _mean, _drawn, _spots);
        foreach (var spot in _spots)
            DrawSpot(new Vector2((spot.TileX - _drawn.X0) * _tile + spot.X, (spot.TileY - _drawn.Y0) * _tile + spot.Y), spot.Hash);
        DrawSetTransformMatrix(Transform2D.Identity);
    }

    /// <summary>Draws the thing at <paramref name="at"/> (the plane's pixels from the drawn corner), the rest of it drawn from <paramref name="hash"/>.</summary>
    protected abstract void DrawSpot(Vector2 at, uint hash);
}
