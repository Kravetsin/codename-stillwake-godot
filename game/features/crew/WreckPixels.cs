using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// Whether a wreck's picture is opaque at a point (tool-edge): its hi-res picture, read once by name, where the wreck is drawn and
/// turned as drawn. A picture that will not load counts as its whole box, so a wreck is never unhittable.
/// </summary>
public sealed class WreckPixels
{
    private readonly Dictionary<string, Image?> _pictures = new();

    /// <summary>
    /// The test for wrecks drawn <paramref name="progress"/> of the way to their next tick and turned as drawn (<see cref="PieceDrawn.Pose"/>),
    /// a point in thousandths of a cell.
    /// </summary>
    public Func<Piece, double, double, bool> Solid(double progress) => (piece, x, y) =>
    {
        var (halfW, halfH) = Aiming.PictureOf(piece.Item);
        // Into the picture's own frame by the sim's turned box (debris-physics), never a turn of the client's own.
        var shape = PieceDrawn.Shape(piece, progress, halfW, halfH);
        var (dx, dy) = PieceDrawn.Local(shape, x, y);
        var image = DebrisLooks.NameOf(piece.Item, piece.Id) is { } name ? PictureOf(name) : null;
        if (image == null) return Math.Abs(dx) <= halfW && Math.Abs(dy) <= halfH;
        double perPixel = Hero.Unit / Rig.Cell;
        int ix = (int)Math.Floor((dx / perPixel + image.GetWidth() / (2.0 * HiRes.Scale)) * HiRes.Scale);
        int iy = (int)Math.Floor((dy / perPixel + image.GetHeight() / (2.0 * HiRes.Scale)) * HiRes.Scale);
        return ix >= 0 && iy >= 0 && ix < image.GetWidth() && iy < image.GetHeight() && image.GetPixel(ix, iy).A > 0.5f;
    };

    private Image? PictureOf(string name)
    {
        if (!_pictures.TryGetValue(name, out var image)) _pictures[name] = image = HiRes.Load(name);
        return image;
    }
}
