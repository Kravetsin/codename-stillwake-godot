using System.Collections.Generic;
using Godot;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>How <see cref="PlanTiles"/> draws what is not built yet.</summary>
public enum PlanLook
{
    /// <summary>
    /// A plan or a ghost in hand: its own picture greyed and lifted most of the way to white, tinted a little
    /// by the node's colour, so what was planned, a wall's material too, stays plain to see and is never
    /// taken for what is built.
    /// </summary>
    Ghost,

    /// <summary>
    /// A mark: its picture in the node's colour as a projection — every other row of pixels a little
    /// darker, a band of light sweeping slowly across, breathing.
    /// </summary>
    Projection,
}

/// <summary>
/// The ship's tile layers again, for what is not built yet: plans and ghosts are laid here, each picture
/// on the layer the grid would draw it on, all drawn together in one <see cref="PlanLook"/> coloured by
/// <see cref="CanvasItem.Modulate"/>. Unlit, so a plan reads in the dark and gives no light. <see cref="Begin"/>
/// erases only the cells laid since the last one.
/// </summary>
public partial class PlanTiles : Node2D
{
    private readonly TileMapLayer[] _layers = new TileMapLayer[8];
    private readonly List<(TileLayer Layer, Vector2I At)> _laid = [];
    private readonly ShipTiles _tiles = ShipTiles.Shared;

    private static readonly Shader Ghost = new()
    {
        Code = """
            shader_type canvas_item;
            render_mode unshaded;
            // How far toward white the greyed picture is lifted, and how much of the node's colour tints it.
            uniform float lift = 0.6;
            uniform float tint = 0.3;

            void fragment() {
                vec4 c = texture(TEXTURE, UV);
                float l = dot(c.rgb, vec3(0.299, 0.587, 0.114));
                vec3 ghost = mix(vec3(l), vec3(1.0), lift);
                COLOR = vec4(mix(ghost, COLOR.rgb, tint), c.a * COLOR.a);
            }
            """,
    };

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

    /// <param name="look">A plan's or a ghost's, or a mark's.</param>
    /// <param name="backAlpha">How strongly a back wall shows against the rest: fainter, as it is behind.</param>
    /// <param name="tint">How much of the node's colour a <see cref="PlanLook.Ghost"/> takes on.</param>
    /// <param name="backZ">
    /// Where back walls are drawn in the world, if not with the rest: a plan's just over the rooms' air,
    /// under what is built in front of them.
    /// </param>
    public PlanTiles(PlanLook look, float backAlpha = 1, float tint = 0.3f, int? backZ = null)
    {
        var unlit = new ShaderMaterial { Shader = look == PlanLook.Ghost ? Ghost : Projection };
        if (look == PlanLook.Ghost) unlit.SetShaderParameter("tint", tint);
        for (int i = 0; i < _layers.Length; i++)
        {
            _layers[i] = new TileMapLayer { TileSet = _tiles.Set, Material = unlit };
            if (i == (int)TileLayer.Back)
            {
                _layers[i].Modulate = new Color(1, 1, 1, backAlpha);
                if (backZ is int z) (_layers[i].ZAsRelative, _layers[i].ZIndex) = (false, z);
            }
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
            Put(piece.Layer, at, tile);
        }
    }

    /// <summary>
    /// A built pipe's reach toward the plans beside it, on the sides in <paramref name="sides"/>, drawn as
    /// they are: the half cell between them is not left empty.
    /// </summary>
    public void Stub(int cell, int width, int sides) =>
        Put(TileLayer.Pipes, new Vector2I(cell % width, cell / width), _tiles.At($"pipe_{sides}"));

    private void Put(TileLayer layer, Vector2I at, Vector2I tile)
    {
        _layers[(int)layer].SetCell(at, ShipTiles.Source, tile);
        _laid.Add((layer, at));
    }
}
