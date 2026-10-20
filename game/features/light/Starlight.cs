using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// The starlight (<see cref="StarField"/>) as a texture of a texel a cell over what is built and a margin
/// round it, handed to <see cref="Lit"/>, whose material reads it smoothed between the cells of a picture's
/// own side and, past its edge, the edge's texel: open space whole in the stars, as all space past what is
/// built is. Worked out only when the ship changes, and then only around the cells changed; the texture is
/// kept and filled anew while its size holds, so a door toggled allocates nothing.
/// </summary>
public partial class Starlight : Node
{
    private const int Cell = ShipCamera.Cell;

    private readonly StarField _field;
    private Image? _image;
    private ImageTexture? _texture;
    /// <summary>Where <see cref="Lit"/> was last told the texture lies, in cells.</summary>
    private Box _shown = Box.None;

    /// <param name="sky">The light through the open sky: the stars', or a planet's daylight (0076).</param>
    public Starlight((float R, float G, float B)? sky = null) => _field = sky == null ? new StarField() : new StarField(sky, L.GroundKeep, L.GroundDownKeep, L.GroundSoak);

    /// <summary>Works the starlight out anew for the grid as it stands.</summary>
    public void Show(IGridQueries grid)
    {
        _field.Work(grid);
        Upload();
    }

    /// <summary>Works the starlight out again around <paramref name="cells"/>, changed in what stops, dims or lets in the stars.</summary>
    public void Follow(IGridQueries grid, IReadOnlyList<int> cells)
    {
        _field.Follow(grid, cells);
        Upload();
    }

    private void Upload()
    {
        var r = _field.Region;
        int w = r.Width, h = r.Height;
        if (_image != null && _texture != null && _image.GetWidth() == w && _image.GetHeight() == h)
        {
            _image.SetData(w, h, false, Image.Format.Rgba8, _field.Texels);
            _texture.Update(_image);
        }
        else
        {
            _image = Image.CreateFromData(w, h, false, Image.Format.Rgba8, _field.Texels);
            _texture = ImageTexture.CreateFromImage(_image);
            _shown = Box.None;
        }
        if (r == _shown) return;
        _shown = r;
        Lit.ShowStarlight(_texture, new Rect2(r.Left * Cell, r.Top * Cell, w * Cell, h * Cell));
    }
}
