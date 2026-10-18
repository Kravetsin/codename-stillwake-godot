using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// What of the plans being built is already real (0063): pieces of the ship's own pictures, each up to its plan's
/// <see cref="PlanCut"/>, drawn as the grid draws what is built — at each picture's layer of the ship, lit by
/// <see cref="Lit"/>, walls, their edges and doors keeping a wall's share of the light. One canvas item a tile
/// layer, every piece a region of the one atlas, so hundreds of plans cost a few batched draws, redrawn only when
/// the plans change; no node and no shader parameter a plan.
/// </summary>
public partial class BuiltParts : Node2D
{
    private readonly ShipTiles _tiles;
    private readonly Sketch[] _layers = new Sketch[8];
    private readonly List<(Rect2 Rect, Rect2 Source)>[] _pieces = new List<(Rect2, Rect2)>[8];

    public BuiltParts(ShipTiles tiles)
    {
        _tiles = tiles;
        for (int i = 0; i < _layers.Length; i++)
        {
            var layer = (TileLayer)i;
            var pieces = _pieces[i] = [];
            _layers[i] = new Sketch(ZOf(layer), lit: true) { Paint = g => DrawPieces(g, pieces) };
            Lit.Set(_layers[i], layer is TileLayer.Walls or TileLayer.Edges or TileLayer.Doors ? Light.WallIn : 1f);
            AddChild(_layers[i]);
        }
    }

    /// <summary>Where the grid draws a picture of <paramref name="layer"/> in the world, as <c>GridView</c> stacks its layers.</summary>
    private static int ZOf(TileLayer layer) => layer switch
    {
        TileLayer.Back => Layers.BackWalls,
        TileLayer.Pipes => Layers.Pipes,
        TileLayer.Valves => Layers.Valves,
        TileLayer.Walls => Layers.Walls,
        TileLayer.Edges => Layers.Edges,
        TileLayer.Fixtures => Layers.Fixtures,
        TileLayer.Doors => Layers.Doors,
        _ => Layers.Machines,
    };

    /// <summary>Starts a drawing: what was drawn before goes.</summary>
    public void Begin()
    {
        for (int i = 0; i < _pieces.Length; i++)
        {
            if (_pieces[i].Count == 0) continue;
            _pieces[i].Clear();
            _layers[i].QueueRedraw();
        }
    }

    /// <summary>Draws the atlas's <paramref name="source"/> at <paramref name="rect"/>, in pixels of the world, on <paramref name="layer"/>.</summary>
    public void Add(TileLayer layer, Rect2 rect, Rect2 source)
    {
        _pieces[(int)layer].Add((rect, source));
        _layers[(int)layer].QueueRedraw();
    }

    private void DrawPieces(Sketch g, List<(Rect2 Rect, Rect2 Source)> pieces)
    {
        foreach (var (rect, source) in pieces) g.DrawTextureRectRegion(_tiles.Texture, rect, source);
    }
}
