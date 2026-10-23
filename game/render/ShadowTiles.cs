using System;
using System.Collections.Generic;
using Godot;

namespace Stillwake.Game.Render;

/// <summary>
/// The shadows walls cast on natural back walls (<see cref="GroundShadow"/>, 0088) as tiles of one <see cref="TileSet"/>, made the
/// first time a cell's walls round it ask for one and kept: a sprint over a planet's surface made 166, a deep cave 102. Its slots are made at start, white, so a
/// shadow made later only changes the atlas's pixels, never the tile set, which would make every layer drawing it rebuild. A tile
/// multiplies what is under it, so it is drawn just over the back walls and darkens them by their light.
/// </summary>
public sealed class ShadowTiles
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The atlas's side, in tiles: slots for this many squared shadows.</summary>
    private const int Across = 32;

    private readonly Image _atlas = Image.CreateEmpty(Across * Cell, Across * Cell, false, Image.Format.Rgba8);
    private readonly ImageTexture _texture;
    /// <summary>The tile of each three by three of walls asked for, by <see cref="KeyOf"/>; null where it casts none.</summary>
    private readonly Dictionary<int, Vector2I?> _byWalls = new();
    /// <summary>The tile of each shadow made, by its tones: walls round a cell that cast the same shadow share it.</summary>
    private readonly Dictionary<string, Vector2I> _byLook = new();
    private int _made;
    private bool _changed, _warned;

    public TileSet Set { get; } = new() { TileSize = new Vector2I(Cell, Cell) };

    /// <summary>The material a layer of shadows is drawn with: multiplying, unlit.</summary>
    public static readonly CanvasItemMaterial Multiply = new() { BlendMode = CanvasItemMaterial.BlendModeEnum.Mul, LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };

    private static ShadowTiles? _shared;

    /// <summary>One set for every grid drawn, the ship's and a planet's: a shadow hangs on the walls round a cell alone.</summary>
    public static ShadowTiles Shared => _shared ??= new();

    private ShadowTiles()
    {
        _atlas.Fill(Colors.White);
        _texture = ImageTexture.CreateFromImage(_atlas);
        var source = new TileSetAtlasSource { Texture = _texture, TextureRegionSize = new Vector2I(Cell, Cell) };
        Set.AddSource(source, 0);
        for (int y = 0; y < Across; y++)
            for (int x = 0; x < Across; x++)
                source.CreateTile(new Vector2I(x, y));
    }

    /// <summary>The source every tile is in.</summary>
    public const int Source = 0;

    /// <summary>
    /// The key of the walls in the three by three round a cell, row by row (<paramref name="around"/>: whether each holds one): a
    /// bit a cell.
    /// </summary>
    private static int KeyOf(ReadOnlySpan<bool> around)
    {
        int key = 0;
        for (int i = 0; i < 9; i++) key |= (around[i] ? 1 : 0) << i;
        return key;
    }

    /// <summary>The tile of the shadow the walls <paramref name="around"/> a cell cast on it; null for none, or once every slot is taken.</summary>
    public Vector2I? TileOf(ReadOnlySpan<bool> around)
    {
        int key = KeyOf(around);
        if (_byWalls.TryGetValue(key, out var known)) return known;
        var shade = GroundShadow.Of(around);
        Vector2I? tile = shade == null ? null : Place(shade);
        return _byWalls[key] = tile;
    }

    private Vector2I? Place(byte[] shade)
    {
        string look = Convert.ToHexString(shade);
        if (_byLook.TryGetValue(look, out var same)) return same;
        if (_made == Across * Across)
        {
            // Accepted (0088): past this many a cell shows none; a planet was seen to need a few hundred.
            if (!_warned) GD.PushWarning($"ShadowTiles: all {Across * Across} shadows taken, new ones not drawn");
            _warned = true;
            return null;
        }
        var tile = new Vector2I(_made % Across, _made / Across);
        _made++;
        for (int py = 0; py < Cell; py++)
            for (int px = 0; px < Cell; px++)
            {
                float lit = 1f - (float)(shade[py * Cell + px] * GroundLooks.ShadowDepth / GroundLooks.ShadowSteps);
                _atlas.SetPixel(tile.X * Cell + px, tile.Y * Cell + py, new Color(lit, lit, lit));
            }
        _changed = true;
        return _byLook[look] = tile;
    }

    /// <summary>Sends the shadows made since the last call to the screen; called once the cells of a frame are drawn.</summary>
    public void Flush()
    {
        if (!_changed) return;
        // Straight to the renderer: ImageTexture.Update would tell the tile set it changed, and every layer of it would rebuild.
        RenderingServer.Texture2DUpdate(_texture.GetRid(), _atlas, 0);
        _changed = false;
    }
}
