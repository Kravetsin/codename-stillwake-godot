using System.Collections.Generic;
using Godot;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>
/// The ship's tile layers again, for what is not built yet: plans and ghosts are laid here, each picture
/// on the layer the grid would draw it on, all tinted together by <see cref="CanvasItem.Modulate"/>.
/// Unlit, so a plan reads in the dark. <see cref="Begin"/> erases only the cells laid since the last one.
/// </summary>
public partial class PlanTiles : Node2D
{
    private readonly TileMapLayer[] _layers = new TileMapLayer[8];
    private readonly List<(TileLayer Layer, Vector2I At)> _laid = [];
    private readonly ShipTiles _tiles = ShipTiles.Shared;

    /// <param name="backAlpha">How strongly a back wall shows against the rest: fainter, as it is behind.</param>
    public PlanTiles(float backAlpha = 1)
    {
        var unlit = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        for (int i = 0; i < _layers.Length; i++)
        {
            _layers[i] = new TileMapLayer { TileSet = _tiles.Set, Material = unlit };
            if (i == (int)TileLayer.Back) _layers[i].Modulate = new Color(1, 1, 1, backAlpha);
            AddChild(_layers[i]);
        }
    }

    /// <summary>Starts a drawing: what was laid before is erased.</summary>
    public void Begin()
    {
        foreach (var (layer, at) in _laid) _layers[(int)layer].EraseCell(at);
        _laid.Clear();
    }

    /// <summary>Lays <paramref name="placement"/> at <paramref name="cell"/>, joined on the sides in <paramref name="linked"/>.</summary>
    public void Lay(Placement placement, int cell, int width, int linked)
    {
        var at = new Vector2I(cell % width, cell / width);
        foreach (var piece in BlueprintArt.PiecesOf(placement, linked))
        {
            var tile = piece.Cut ? _tiles.CutAt(piece.Picture, at.X, at.Y) : _tiles.At(piece.Picture);
            _layers[(int)piece.Layer].SetCell(at, ShipTiles.Source, tile);
            _laid.Add((piece.Layer, at));
        }
    }
}
