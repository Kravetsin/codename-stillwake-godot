using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Liquids;

namespace Stillwake.Game.Features.Liquids;

/// <summary>
/// The water in a surface's cells (flowing-liquids), over the heroes, so one swimming is seen through it: each frame the
/// cells in view, a cell round them, written into a texture (<see cref="WaterCells"/>), eased between ticks (<see cref="WaterEase"/>) and drawn by <see cref="WaterLook"/> on a
/// sprite scaled a cell a texel. Nothing is drawn, and nothing written, on a surface that has held no liquid. A stream is heard: one
/// pouring loop at the cell liquid fell into last tick nearest the view's middle, within <c>Sound.PourReach</c> cells.
/// </summary>
public partial class LiquidsView : Node2D
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>The rows over the view written too: the shader darkens a cell by the water over it, up to its DARKEST, 6.</summary>
    private const int Above = 6;

    private readonly IGridQueries _grid;
    private readonly ILiquidsQueries _liquids;
    private readonly Func<int, bool> _blocks;
    private readonly Sprite2D _look = new()
    {
        Centered = false,
        ZIndex = Layers.Water,
        Scale = new Vector2(Cell, Cell),
        Material = new ShaderMaterial { Shader = WaterLook.Shader },
    };
    private byte[] _texels = [], _written = [];
    /// <summary>The cells the texels last drawn cover: left, top, wide, tall; none after a frame that drew nothing.</summary>
    private (int, int, int, int) _box;
    private readonly WaterEase _ease = new();
    private Image? _image;
    private ImageTexture? _texture;
    private int _wide, _tall;
    private readonly Heard2D _pour = Cues.Player2D(Game.Sound.WaterPour, Game.Sound.World);
    private readonly Random _random = new();

    public LiquidsView(IGridQueries grid, ILiquidsQueries liquids)
    {
        (_grid, _liquids, _blocks) = (grid, liquids, grid.Blocks);
        ((ShaderMaterial)_look.Material).SetShaderParameter("cell", (float)Cell);
        AddChild(_look);
        AddChild(_pour);
    }

    public override void _Process(double delta)
    {
        var layer = _liquids.Layer;
        if (layer.IsEmpty)
        {
            _look.Visible = false;
            _box = default;
            return;
        }
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewportRect();
        Hear(view.GetCenter());
        int left = Math.Max(0, (int)Math.Floor(view.Position.X / Cell) - 1), top = Math.Max(0, (int)Math.Floor(view.Position.Y / Cell) - Above);
        int right = Math.Min(_grid.Width, (int)Math.Ceiling(view.End.X / Cell) + 1), bottom = Math.Min(_grid.Height, (int)Math.Ceiling(view.End.Y / Cell) + 1);
        int wide = right - left, tall = bottom - top;
        if (wide <= 0 || tall <= 0)
        {
            _look.Visible = false;
            _box = default;
            return;
        }
        if (_written.Length != wide * tall * 4) _written = new byte[wide * tall * 4];
        WaterCells.Write(layer, _grid.Width, _grid.Height, _blocks, left, top, wide, tall, _written);
        // Water at rest in a view that stayed is drawn as it was: the texture is made again only when what it shows would change.
        if (_ease.Settled && _box == (left, top, wide, tall) && _written.AsSpan().SequenceEqual(_texels)) return;
        (_texels, _written, _box) = (_written, _texels, (left, top, wide, tall));
        bool wet = _ease.Step(_texels, left, top, wide, tall, delta);
        _look.Visible = wet;
        if (!wet) return;
        if (_image == null || wide != _wide || tall != _tall)
        {
            (_wide, _tall) = (wide, tall);
            _image = Image.CreateFromData(wide, tall, false, Image.Format.Rgba8, _texels);
            _texture = ImageTexture.CreateFromImage(_image);
            _look.Texture = _texture;
        }
        else
        {
            _image.SetData(wide, tall, false, Image.Format.Rgba8, _texels);
            _texture!.Update(_image);
        }
        _look.Position = new Vector2(left * Cell, top * Cell);
        ((ShaderMaterial)_look.Material).SetShaderParameter("origin", _look.Position);
    }

    /// <summary>The stream nearest <paramref name="ear"/>, the view's middle, heard pouring; silent with none in reach.</summary>
    private void Hear(Vector2 ear)
    {
        int x = Math.Clamp((int)(ear.X / Cell), 0, _grid.Width - 1), y = Math.Clamp((int)(ear.Y / Cell), 0, _grid.Height - 1);
        if (_liquids.FallNear(y * _grid.Width + x, Game.Sound.PourReach) is int fell)
        {
            _pour.GlobalPosition = new Vector2((fell % _grid.Width + 0.5f) * Cell, (fell / _grid.Width + 0.5f) * Cell);
            // Begun somewhere along its loop, so it never starts on the same gurgle.
            if (!_pour.Playing) _pour.Play((float)(_random.NextDouble() * _pour.Stream.GetLength()));
        }
        else if (_pour.Playing) _pour.Stop();
    }
}
