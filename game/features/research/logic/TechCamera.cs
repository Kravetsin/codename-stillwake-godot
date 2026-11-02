using System;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// How the tree's map lies in the tablet's view (2026-10-30-tablet-tree): dragged, zoomed about the pointer, from the whole map
/// in view to larger than life. A map smaller than the view stands in its middle; a larger one keeps its edges past the view's,
/// so it cannot be dragged away and lost.
/// </summary>
public sealed class TechCamera
{
    /// <summary>The largest it is drawn, and how much a notch of the wheel changes it.</summary>
    public const double Most = 1.5, Notch = 1.2;
    /// <summary>A zoom this close to life size is life size: its text is sharp only there.</summary>
    private const double Snap = 0.08;

    private double _viewWide = 1, _viewTall = 1, _mapWide = 1, _mapTall = 1;

    /// <summary>How large the map is drawn.</summary>
    public double Zoom { get; private set; } = 1;

    /// <summary>Where the map's top-left is in the view.</summary>
    public double X { get; private set; }

    /// <summary>Where the map's top-left is in the view.</summary>
    public double Y { get; private set; }

    /// <summary>The smallest it is drawn: all of the map in view, and never above life size for it.</summary>
    public double Least => Math.Min(1, Math.Min(_viewWide / _mapWide, _viewTall / _mapTall));

    /// <summary>The view and the map now, in pixels: the map is kept in bounds of them.</summary>
    public void Fit(double viewWide, double viewTall, double mapWide, double mapTall)
    {
        (_viewWide, _viewTall, _mapWide, _mapTall) = (viewWide, viewTall, mapWide, mapTall);
        Zoom = Math.Clamp(Zoom, Least, Most);
        Keep();
    }

    /// <summary>Moves the map by the pointer's move.</summary>
    public void Drag(double dx, double dy)
    {
        (X, Y) = (X + dx, Y + dy);
        Keep();
    }

    /// <summary>The wheel, <paramref name="notches"/> up to come closer, the point of the map under the pointer staying under it.</summary>
    public void Wheel(int notches, double atX, double atY)
    {
        var (mapX, mapY) = ToMap(atX, atY);
        double zoom = Math.Clamp(Zoom * Math.Pow(Notch, notches), Least, Most);
        Zoom = Math.Abs(zoom - 1) < Snap ? 1 : zoom;
        (X, Y) = (atX - mapX * Zoom, atY - mapY * Zoom);
        Keep();
    }

    /// <summary>Brings the map's point to the middle of the view, at life size or as near as the map allows.</summary>
    public void Centre(double mapX, double mapY)
    {
        Zoom = Math.Max(1, Least);
        (X, Y) = (_viewWide / 2 - mapX * Zoom, _viewTall / 2 - mapY * Zoom);
        Keep();
    }

    /// <summary>The point of the map at a point of the view.</summary>
    public (double X, double Y) ToMap(double x, double y) => ((x - X) / Zoom, (y - Y) / Zoom);

    private void Keep() => (X, Y) = (Within(X, _viewWide, _mapWide * Zoom), Within(Y, _viewTall, _mapTall * Zoom));

    private static double Within(double at, double view, double shown) => shown <= view ? (view - shown) / 2 : Math.Clamp(at, view - shown, 0);
}
