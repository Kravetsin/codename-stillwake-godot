using System.Collections.Generic;
using Godot;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Render;

/// <summary>
/// The ship's tile layers again, for what is not built yet: plans and ghosts are laid here, each picture on the
/// layer the grid would draw it on, all drawn as ghosts — each picture greyed and lifted most of the way to white,
/// tinted a little by <see cref="CanvasItem.Modulate"/> — so what was planned, a wall's material too, stays plain to
/// see and is never taken for what is built. Unlit, so a plan reads in the dark and gives no light. <see cref="Begin"/>
/// erases only the cells laid since the last one. A plan being built is laid cut (<see cref="PlanCut"/>): its ghost
/// only beyond the line, drawn here a region of the atlas at a time beside each layer, the rest real in
/// <see cref="BuiltParts"/>; with none, the rest is left to what draws it already, as a mark's thing is the grid's.
/// </summary>
public partial class PlanTiles : Node2D
{
    private readonly TileMapLayer[] _layers = new TileMapLayer[8];
    private readonly List<(TileLayer Layer, Vector2I At)> _laid = [];
    /// <summary>The ghosts of plans being built, beyond their cuts: a region of the atlas each, a canvas item a layer over its tiles.</summary>
    private readonly Sketch[] _cut = new Sketch[8];
    private readonly List<(Rect2 Rect, Rect2 Source)>[] _cutPieces = new List<(Rect2, Rect2)>[8];
    private readonly BuiltParts? _built;
    private const int Cell = ShipCamera.Cell;
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

    /// <param name="backAlpha">How strongly a back wall shows against the rest: fainter, as it is behind.</param>
    /// <param name="tint">How much of the node's colour the ghost takes on.</param>
    /// <param name="built">Where the real share of a plan laid cut is drawn.</param>
    /// <param name="backZ">
    /// Where back walls are drawn in the world, if not with the rest: a plan's just over the rooms' air,
    /// under what is built in front of them.
    /// </param>
    public PlanTiles(float backAlpha = 1, float tint = 0.3f, int? backZ = null, BuiltParts? built = null)
    {
        _built = built;
        var unlit = new ShaderMaterial { Shader = Ghost };
        unlit.SetShaderParameter("tint", tint);
        for (int i = 0; i < _layers.Length; i++)
        {
            _layers[i] = new TileMapLayer { TileSet = _tiles.Set, Material = unlit };
            var pieces = _cutPieces[i] = [];
            _cut[i] = new Sketch(0, lit: true) { Material = unlit, Paint = g => DrawCut(g, pieces) };
            foreach (CanvasItem item in (CanvasItem[])[_layers[i], _cut[i]])
            {
                if (i == (int)TileLayer.Back)
                {
                    item.Modulate = new Color(1, 1, 1, backAlpha);
                    if (backZ is int z) (item.ZAsRelative, item.ZIndex) = (false, z);
                }
                AddChild(item);
            }
        }
    }

    /// <summary>Starts a drawing: what was laid before is erased.</summary>
    public void Begin()
    {
        foreach (var (layer, at) in _laid) _layers[(int)layer].EraseCell(at);
        _laid.Clear();
        for (int i = 0; i < _cutPieces.Length; i++)
        {
            if (_cutPieces[i].Count == 0) continue;
            _cutPieces[i].Clear();
            _cut[i].QueueRedraw();
        }
        _built?.Begin();
    }

    /// <summary>
    /// Lays <paramref name="placement"/> at <paramref name="cell"/>, joined on the sides in <paramref name="linked"/>;
    /// with a <paramref name="cut"/>, only its ghost beyond the line, the rest real in the <see cref="BuiltParts"/>.
    /// </summary>
    public void Lay(Placement placement, int cell, int width, int linked, PlanCut? cut = null)
    {
        var at = new Vector2I(cell % width, cell / width);
        foreach (var piece in BlueprintArt.PiecesOf(placement, linked))
        {
            var tile = piece.Cut ? _tiles.CutAt(piece.Picture, at.X, at.Y) : _tiles.At(piece.Picture);
            if (cut is not { } line)
            {
                Put(piece.Layer, at, tile);
                continue;
            }
            // A tile covers its picture from the cell, whole cells of it: one cut from a picture over the world, one.
            var size = (piece.Cut ? Vector2I.One : _tiles.SizeOf(piece.Picture)) * Cell;
            var (real, plan) = line.Split((at.X * Cell, at.Y * Cell, size.X, size.Y));
            Region(plan, at, tile, (rect, source) => _cutPieces[(int)piece.Layer].Add((rect, source)));
            Region(real, at, tile, (rect, source) => _built?.Add(piece.Layer, rect, source));
            _cut[(int)piece.Layer].QueueRedraw();
        }
    }

    /// <summary>The atlas region under <paramref name="part"/> of the tile at <paramref name="tile"/> laid at cell <paramref name="at"/>, handed to <paramref name="to"/> with where it goes; nothing for an empty part.</summary>
    private static void Region((int X, int Y, int W, int H) part, Vector2I at, Vector2I tile, System.Action<Rect2, Rect2> to)
    {
        if (part.W <= 0 || part.H <= 0) return;
        var rect = new Rect2(part.X, part.Y, part.W, part.H);
        to(rect, new Rect2((Vector2)(tile * Cell) + rect.Position - (Vector2)(at * Cell), rect.Size));
    }

    private void DrawCut(Sketch g, List<(Rect2 Rect, Rect2 Source)> pieces)
    {
        foreach (var (rect, source) in pieces) g.DrawTextureRectRegion(_tiles.Texture, rect, source);
    }

    /// <summary>
    /// A built pipe's reach toward the plans beside it, on the sides in <paramref name="sides"/>, drawn as
    /// they are: the half cell between them is not left empty. Its arms only: its joint stays the grid's, built.
    /// </summary>
    public void Stub(int cell, int width, int sides)
    {
        var at = new Vector2I(cell % width, cell / width);
        var tile = _tiles.At($"pipe_{sides}");
        var pieces = _cutPieces[(int)TileLayer.Pipes];
        foreach (var arm in Blueprint.ArmsOf(sides, Joint, Cell))
            Region((at.X * Cell + arm.X, at.Y * Cell + arm.Y, arm.W, arm.H), at, tile, (rect, source) => pieces.Add((rect, source)));
        _cut[(int)TileLayer.Pipes].QueueRedraw();
    }

    /// <summary>The joint in the middle of every pipe's picture: where a lone pipe is opaque.</summary>
    private static (int X, int Y, int W, int H) Joint => _joint ??= JointOf(ShipTiles.ImageOf("pipe_0").GetUsedRect());
    private static (int X, int Y, int W, int H)? _joint;
    private static (int, int, int, int) JointOf(Rect2I r) => (r.Position.X, r.Position.Y, r.Size.X, r.Size.Y);

    private void Put(TileLayer layer, Vector2I at, Vector2I tile)
    {
        _layers[(int)layer].SetCell(at, ShipTiles.Source, tile);
        _laid.Add((layer, at));
    }
}
