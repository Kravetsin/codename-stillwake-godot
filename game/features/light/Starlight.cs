using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// The starlight (<see cref="StarField"/>) as a texture array of a texel a cell over what is built and a margin
/// round it, cut into tiles (<see cref="StarTiles"/>), handed to <see cref="Lit"/>, whose material reads it smoothed
/// between the cells of a picture's own side and, past its edge, the edge's texel: open space whole in the stars, as all
/// space past what is built is. Worked out only when the ship changes, and then only around the cells changed; only the
/// tiles that work wrote are sent again, so a cell dug on a planet sends a tile or four, not the planet.
/// </summary>
public partial class Starlight : Node
{
    private const int Cell = ShipCamera.Cell;

    private readonly StarField _field;
    private Texture2DArray? _tiles;
    /// <summary>Where <see cref="Lit"/> was last told the tiles lie, in cells: the region they were cut from.</summary>
    private Box _shown = Box.None;
    /// <summary>Scratch: one tile's texels, and its image, sent a layer at a time.</summary>
    private readonly byte[] _tile = new byte[StarTiles.Size * StarTiles.Size * 4];
    private Image? _tileImage;

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
        if (_tiles == null || r != _shown)
        {
            // The region moved or grew: every tile cut again, in a new array.
            var (across, down) = StarTiles.Of(r);
            var images = new Godot.Collections.Array<Image>();
            for (int layer = 0; layer < across * down; layer++)
            {
                StarTiles.Copy(_field.Texels, r, layer, _tile);
                images.Add(Image.CreateFromData(StarTiles.Size, StarTiles.Size, false, Image.Format.Rgba8, _tile));
            }
            _tiles = new Texture2DArray();
            _tiles.CreateFromImages(images);
            _shown = r;
            Lit.ShowStarlight(_tiles, new Rect2(r.Left * Cell, r.Top * Cell, r.Width * Cell, r.Height * Cell), new Vector2I(r.Width, r.Height), StarTiles.Size);
            return;
        }
        _tileImage ??= Image.CreateEmpty(StarTiles.Size, StarTiles.Size, false, Image.Format.Rgba8);
        foreach (int layer in StarTiles.Touched(r, _field.Written))
        {
            StarTiles.Copy(_field.Texels, r, layer, _tile);
            _tileImage.SetData(StarTiles.Size, StarTiles.Size, false, Image.Format.Rgba8, _tile);
            _tiles.UpdateLayer(_tileImage, layer);
        }
    }
}
