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
    /// The test for wrecks drawn <paramref name="progress"/> of the way to their next tick and turned by <paramref name="turns"/>
    /// (radians by id, none unturned), a point in thousandths of a cell.
    /// </summary>
    public Func<Piece, double, double, bool> Solid(double progress, IReadOnlyDictionary<int, double> turns) => (piece, x, y) =>
    {
        var (cx, cy) = PieceDrawn.At(piece, progress);
        double turn = turns.TryGetValue(piece.Id, out double t) ? t : 0, cos = Math.Cos(turn), sin = Math.Sin(turn);
        // Into the picture's own frame: the offset turned back by the picture's turn.
        double dx = (x - cx) * cos + (y - cy) * sin, dy = -(x - cx) * sin + (y - cy) * cos;
        var image = DebrisLooks.NameOf(piece.Item, piece.Id) is { } name ? PictureOf(name) : null;
        if (image == null)
        {
            var (halfW, halfH) = Aiming.PictureOf(piece.Item);
            return Math.Abs(dx) <= halfW && Math.Abs(dy) <= halfH;
        }
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
