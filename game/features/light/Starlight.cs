using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// The starlight (<see cref="StarField"/>) as a texture of a texel a cell over what is built and a margin
/// round it, handed to <see cref="Lit"/>, whose material reads it smoothed between the cells of a picture's
/// own side and, past its edge, the edge's texel: open space whole in the stars, as all space past what is
/// built is. Worked out only when the ship changes; the texture is kept and filled anew while its size
/// holds, so a door toggled allocates nothing and costs only the sweeps over the ship.
/// </summary>
public partial class Starlight : Node
{
    private const int Cell = ShipCamera.Cell;

    private readonly StarField _field = new();
    private Image? _image;
    private ImageTexture? _texture;
    /// <summary>Where <see cref="Lit"/> was last told the texture lies, in cells.</summary>
    private Box _shown = Box.None;

    /// <summary>Works the starlight out anew for the grid as it stands.</summary>
    public void Show(IGridQueries grid)
    {
        _field.Work(grid);
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
