using System.Collections.Generic;
using Godot;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>
/// The ship's tile layers again, for what is not built yet: plans and ghosts are laid here, each picture
/// on the layer the grid would draw it on, all tinted together by <see cref="CanvasItem.Modulate"/>.
/// Unlit, so a plan reads in the dark; a <c>projected</c> one as a projection — every other row of
/// pixels a little darker, a band of light sweeping slowly across, breathing — so a plan in the colours
/// of what is built is never taken for it. <see cref="Begin"/> erases only the cells laid since the last one.
/// </summary>
public partial class PlanTiles : Node2D
{
    private readonly TileMapLayer[] _layers = new TileMapLayer[8];
    private readonly List<(TileLayer Layer, Vector2I At)> _laid = [];
    private readonly ShipTiles _tiles = ShipTiles.Shared;

    private static readonly Shader Projection = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded;
            varying vec2 world;

            void vertex() {
                world = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
            }

            void fragment() {
                vec4 c = texture(TEXTURE, UV) * COLOR;
                vec2 px = floor(world);
                // A band of light down the diagonal every few seconds, as a projector's sweep.
                float sweep = fract((px.x + px.y) / 160.0 - TIME * 0.25);
                float sheen = smoothstep(0.0, 0.04, sweep) * (1.0 - smoothstep(0.04, 0.1, sweep));
                float rows = mod(px.y, 2.0) < 1.0 ? 1.0 : 0.8;
                c.rgb = c.rgb * rows + vec3(0.4 * sheen) * c.a;
                c.a *= 0.88 + 0.12 * sin(TIME * 2.2);
                COLOR = c;
            }
            """,
    };

    /// <param name="backAlpha">How strongly a back wall shows against the rest: fainter, as it is behind.</param>
    /// <param name="projected">Drawn as a projection, as a plan or a mark is; a ghost under the pointer is not.</param>
    public PlanTiles(float backAlpha = 1, bool projected = false)
    {
        Material unlit = projected
            ? new ShaderMaterial { Shader = Projection }
            : new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
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
