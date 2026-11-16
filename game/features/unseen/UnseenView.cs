using System.Collections.Generic;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Unseen;

/// <summary>
/// A derelict's inside dark till its crew has seen it (hidden-rooms, <see cref="Sight"/>): ink over every unseen cell of the vessel's
/// frame, over all that stands, glows and moves in it, a cell to a texel. A room seen goes from dark at once on arrival, fading
/// after. Seen from where every hero's body on the surface stands, so friends share what they open.
/// </summary>
public partial class UnseenView : Node2D
{
    private const int Cell = ShipCamera.Cell;
    private static readonly Color Ink = new(0x030406ff);

    private readonly IGridQueries _grid;
    private readonly System.Func<IReadOnlyList<Vector2>> _bodies;
    private readonly int _left, _top;
    private readonly Sight _sight;
    private readonly byte[] _texels;
    private readonly ImageTexture _texture;
    private readonly List<int> _heroes = [];
    private int _version = -1;

    private UnseenView(IGridQueries grid, System.Func<IReadOnlyList<Vector2>> bodies, int left, int top, int width, int height)
    {
        (_grid, _bodies, _left, _top) = (grid, bodies, left, top);
        _sight = new Sight(width, height, c => Open(Grid(c)), c => Seals(Grid(c)));
        _texels = new byte[width * height * 4];
        for (int i = 0; i < width * height; i++) (_texels[i * 4], _texels[i * 4 + 1], _texels[i * 4 + 2]) = ((byte)Ink.R8, (byte)Ink.G8, (byte)Ink.B8);
        _texture = ImageTexture.CreateFromImage(Image.CreateFromData(width, height, false, Image.Format.Rgba8, _texels));
        ZIndex = Layers.Unseen;
        AddChild(new Sprite2D
        {
            Texture = _texture,
            Centered = false,
            Position = new Vector2(left, top) * Cell,
            Scale = new Vector2(Cell, Cell),
            TextureFilter = TextureFilterEnum.Nearest,
            // Ink, whatever light falls there: the ship's dark would tint it, a lamp behind it would show through.
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded },
        });
    }

    /// <summary>The view over a vessel world's vessel, its frame every cell with a back wall; null on any other surface.</summary>
    public static UnseenView? Of(IGridQueries grid, Surfaces.Kind rules, System.Func<IReadOnlyList<Vector2>> bodies)
    {
        if (rules is not VesselKind) return null;
        int w = grid.Width, h = grid.Height, left = w, top = h, right = -1, bottom = -1;
        // Once, as the surface loads: the frame, so a frame's look costs the vessel and not the space round it.
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (grid.BackAt(y * w + x) != null)
                    (left, top, right, bottom) = (System.Math.Min(left, x), System.Math.Min(top, y), System.Math.Max(right, x), System.Math.Max(bottom, y));
        return right < 0 ? null : new UnseenView(grid, bodies, left, top, right - left + 1, bottom - top + 1);
    }

    private int Grid(int frameCell) => (_top + frameCell / _sight.Width) * _grid.Width + _left + frameCell % _sight.Width;

    private bool Open(int cell) => _grid.BackAt(cell) != null && !Structures.Is(_grid.StructureAt(cell), Trait.Seals);

    private bool Seals(int cell) => Structures.Is(_grid.StructureAt(cell), Trait.Seals);

    /// <summary>Whether a cell of the grid is still dark: the pointer names nothing there.</summary>
    public bool Hides(int cell)
    {
        int x = cell % _grid.Width - _left, y = cell / _grid.Width - _top;
        return x >= 0 && y >= 0 && x < _sight.Width && y < _sight.Height && _sight.Hides(y * _sight.Width + x);
    }

    public override void _Process(double delta)
    {
        _heroes.Clear();
        foreach (var body in _bodies())
        {
            int x = Mathf.FloorToInt(body.X / Cell) - _left, y = Mathf.FloorToInt(body.Y / Cell) - _top;
            _heroes.Add(x >= 0 && y >= 0 && x < _sight.Width && y < _sight.Height ? y * _sight.Width + x : -1);
        }
        bool changed = _grid.Version != _version;
        _version = _grid.Version;
        if (!_sight.Look(_heroes, changed, delta * 1000)) return;
        for (int i = 0; i < _sight.Width * _sight.Height; i++) _texels[i * 4 + 3] = (byte)(_sight.Dark(i) * 255 + 0.5f);
        _texture.Update(Image.CreateFromData(_sight.Width, _sight.Height, false, Image.Format.Rgba8, _texels));
    }
}
