using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;
using Vector4 = System.Numerics.Vector4;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// Starlight as the TypeScript game worked it out (0019), cell by cell: the stars shine in every open
/// cell with no back wall and everywhere past the grid's edge, and spread from there in sweeps along
/// every row and column, fading by what they cross (<see cref="Sweeps"/>). Worked out over the whole grid
/// only when the ship changes, a texel a cell marked wall or open, and handed to <see cref="Lit"/>, whose
/// material reads it smoothed between the cells of a picture's own side wherever this light, a
/// directional one with no shadows, falls: the sweeps have already stopped it at walls. Its buffers and
/// texture are kept and filled anew, so a door toggled allocates nothing and costs only the sweeps.
/// </summary>
public partial class Starlight : DirectionalLight2D
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>Cells of open space worked out round the grid, so its edge is lit as the space beyond it.</summary>
    private const int Margin = 4;

    private byte[] _kind = [], _bytes = [];
    private Vector4[] _light = [];
    private Image? _image;
    private ImageTexture? _texture;

    public Starlight() => RangeItemCullMask = Lit.StarMask;

    /// <summary>Works the starlight out anew for the grid as it stands.</summary>
    public void Show(IGridQueries grid)
    {
        int w = grid.Width + Margin * 2, h = grid.Height + Margin * 2;
        if (_kind.Length != w * h) (_kind, _light, _bytes) = (new byte[w * h], new Vector4[w * h], new byte[w * h * 4]);
        var kind = _kind;
        var light = _light;
        var (r, g, b) = L.Star;
        Sweeps.Read(grid, -Margin, -Margin, w, h, kind, light, new Vector4(r, g, b, 0));
        Sweeps.Spread(light, kind, w, h);
        var bytes = _bytes;
        for (int i = 0; i < w * h; i++)
        {
            bool wall = Sweeps.Blocks(kind[i]);
            // Walls are drawn taking WallIn of the light on their face, so a wall cell holds its face's light.
            var face = wall ? light[i] / L.WallIn : light[i];
            bytes[i * 4] = (byte)(Math.Min(1, face.X) * 255);
            bytes[i * 4 + 1] = (byte)(Math.Min(1, face.Y) * 255);
            bytes[i * 4 + 2] = (byte)(Math.Min(1, face.Z) * 255);
            // Its alpha says whether it is a wall's, so the shader blends it only with its own side.
            bytes[i * 4 + 3] = wall ? (byte)255 : (byte)0;
        }
        if (_image != null && _texture != null && _image.GetWidth() == w && _image.GetHeight() == h)
        {
            _image.SetData(w, h, false, Image.Format.Rgba8, bytes);
            _texture.Update(_image);
            return;
        }
        _image = Image.CreateFromData(w, h, false, Image.Format.Rgba8, bytes);
        _texture = ImageTexture.CreateFromImage(_image);
        Lit.ShowStarlight(_texture, new Rect2(-Margin * Cell, -Margin * Cell, w * Cell, h * Cell));
    }
}
