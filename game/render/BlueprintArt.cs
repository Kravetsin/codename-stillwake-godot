using System.Collections.Generic;
using Godot;
using Stillwake.Content;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>Which of a set of tile layers a piece of a placement goes on, back to front, as the grid draws what is built.</summary>
public enum TileLayer { Back, Pipes, Valves, Walls, Edges, Fixtures, Doors, Machines }

/// <summary>
/// A picture of the ship's atlas laid on a cell: whole from its top-left, or <c>Cut</c> from a picture laid over the world; a
/// machine's standing <c>Rise</c> cells above its footprint (<see cref="ShipTiles.RiseOf"/>, the arc bench's vise, 0099).
/// </summary>
public readonly record struct TilePiece(TileLayer Layer, string Picture, bool Cut = false, int Rise = 0);

/// <summary>
/// What a placement looks like built (render/blueprint-art.ts): the ship's own pictures, for plans,
/// ghosts and the build menu's thumbnails, so a plan's outline says what it is.
/// </summary>
public static class BlueprintArt
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>Which sides of a cell, a bit each, as the pictures are numbered.</summary>
    private const int Right = 2, Left = 8, AllSides = 15;

    private static readonly Dictionary<Placement, ImageTexture> Thumbs = new();

    /// <summary>
    /// The pictures <paramref name="placement"/> is drawn with, as the ship draws it built: <paramref name="linked"/>
    /// are the sides a pipe reaches toward, a door joins another door on and a wall runs on into another wall: a wall is
    /// edged on every other side, alone all round, a wall cut to a shape only on its whole sides (0069). A machine is one
    /// picture from its corner.
    /// </summary>
    public static IReadOnlyList<TilePiece> PiecesOf(Placement placement, int linked) => placement.Kind switch
    {
        "wall" => (~linked & AllSides & Shape.ClosedSides(placement.Shape)) is var open and > 0
            ? [new(TileLayer.Walls, ShipTiles.WallName(placement.Material!, placement.Shape), Cut: true),
                new(TileLayer.Edges, ShipTiles.EdgeName(placement.Material!, open, placement.Shape))]
            : [new(TileLayer.Walls, ShipTiles.WallName(placement.Material!, placement.Shape), Cut: true)],
        // Scrap's and the ground's are tiles laid over the world, cut a cell at a time (0079); iron's a plain panel.
        "backWall" => [placement.Material == "scrap" || Materials.Get(placement.Material!).Natural
            ? new(TileLayer.Back, ShipTiles.BackName(placement.Material!), Cut: true)
            : new(TileLayer.Back, $"back_{placement.Material}_plain", Cut: true)],
        // A 16 px picture over the cells of a character's, cut a cell at a time where it lies (0104).
        "floor" or "ladder" => [new(TileLayer.Fixtures, placement.Kind, Cut: true)],
        // A planned door is shut, a door or a hatch by the doors it joins (0067).
        "door" => [new(TileLayer.Doors, DoorShape.Picture(closed: true, linked))],
        "pipe" => [new(TileLayer.Pipes, $"pipe_{linked}")],
        "valve" => [new(TileLayer.Pipes, $"pipe_{linked}"), new(TileLayer.Valves, "valve_open")],
        _ => [new(TileLayer.Machines, $"machine_{placement.Kind}", Rise: ShipTiles.RiseOf(placement.Kind))],
    };

    /// <summary>
    /// What <paramref name="placement"/> builds, as one picture: a build menu card's. A pipe or a valve
    /// reaches both ways; a door is a door whole, the shape it is known by; a picture cut over the world gives its first
    /// cell; a wall cut to a shape shows the cut.
    /// </summary>
    public static Texture2D Thumbnail(Placement placement)
    {
        if (Thumbs.TryGetValue(placement, out var kept)) return kept;
        int linked = placement.Layer == "conduit" ? Left | Right : 0;
        var layers = new List<(Image Image, Vector2I At)>();
        var size = new Vector2I(Cell, Cell);
        IReadOnlyList<TilePiece> pieces = placement.Kind == "door" ? [new(TileLayer.Doors, "door_closed_0")] : PiecesOf(placement, linked);
        foreach (var piece in pieces)
        {
            var image = ShipTiles.ImageOf(piece.Picture);
            // A sheet gives its first cell; a 16 px picture cut over its cells, all of it (0104).
            if (piece.Cut && !ShipTiles.CutDrawn(piece.Picture)) image = image.GetRegion(new Rect2I(0, 0, Cell, Cell));
            // A valve's wheel sits in the middle of its cell, as the grid lays it.
            var at = piece.Layer == TileLayer.Valves ? new Vector2I(Cell / 2 - image.GetWidth() / 2, Cell / 2 - image.GetHeight() / 2) : Vector2I.Zero;
            size = new Vector2I(Mathf.Max(size.X, at.X + image.GetWidth()), Mathf.Max(size.Y, at.Y + image.GetHeight()));
            layers.Add((image, at));
        }
        var picture = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
        foreach (var (image, at) in layers) picture.BlendRect(image, new Rect2I(Vector2I.Zero, image.GetSize()), at);
        return Thumbs[placement] = ImageTexture.CreateFromImage(picture);
    }
}
