using System.Collections.Generic;
using Godot;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>
/// What a placement builds, as one picture (<c>placementPicture</c> in render/blueprint-art.ts): a
/// blueprint just learned. A wall is a cell of its material outlined all round, a pipe or a valve reaches
/// both ways, a machine is its whole picture.
/// </summary>
public static class Thumbnails
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The bits of the sides a pipe reaches toward, as the pipe pictures are numbered: left and right.</summary>
    private const int LeftRight = 8 | 2;

    private static readonly Dictionary<Placement, Texture2D> Made = new();

    public static Texture2D Of(Placement placement)
    {
        if (Made.TryGetValue(placement, out var kept)) return kept;
        (string Name, Vector2I At)[] pieces = placement.Kind switch
        {
            "wall" => [($"wall_{placement.Material}", Vector2I.Zero), ($"wall-edge_{placement.Material}_15", Vector2I.Zero)],
            "backWall" => [(placement.Material == "iron" ? "back_iron_plain" : $"back_{placement.Material}", Vector2I.Zero)],
            "floor" or "ladder" => [(placement.Kind, Vector2I.Zero)],
            "door" => [("door_closed_0", Vector2I.Zero)],
            "pipe" => [($"pipe_{LeftRight}", Vector2I.Zero)],
            "valve" => [($"pipe_{LeftRight}", Vector2I.Zero), ("valve_open", new Vector2I(Cell / 2 - 5, Cell / 2 - 5))],
            _ => [($"machine_{placement.Kind}", Vector2I.Zero)],
        };
        var size = Vector2I.One * Cell;
        var images = new List<(Image, Vector2I)>();
        foreach (var (name, at) in pieces)
        {
            var image = PixelArt.Picture(name).GetImage();
            if (image.IsCompressed()) image.Decompress();
            image.Convert(Image.Format.Rgba8);
            // A picture cut into cells (walls, scrap back walls) gives its first cell; a machine is whole.
            if (placement.Layer != "machine") image = image.GetRegion(new Rect2I(0, 0, Mathf.Min(Cell, image.GetWidth()), Mathf.Min(Cell, image.GetHeight())));
            size = new Vector2I(Mathf.Max(size.X, at.X + image.GetWidth()), Mathf.Max(size.Y, at.Y + image.GetHeight()));
            images.Add((image, at));
        }
        var picture = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
        foreach (var (image, at) in images)
            picture.BlendRect(image, new Rect2I(Vector2I.Zero, image.GetSize()), at);
        return Made[placement] = ImageTexture.CreateFromImage(picture);
    }
}
