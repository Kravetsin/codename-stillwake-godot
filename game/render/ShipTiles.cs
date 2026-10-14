using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;

namespace Stillwake.Game.Render;

/// <summary>
/// Every picture of the ship's cells packed into one atlas, with one normal map beside it, as the single
/// source of one <see cref="TileSet"/>: a layer's piece of the screen is then one draw, whatever
/// pictures it holds. A picture is cut into cells (walls, scrap back walls) or laid whole from a cell's
/// top-left (doors, pipes, machines); a valve, smaller than a cell, sits in the middle of one.
/// </summary>
public sealed class ShipTiles
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The atlas's width, in cells.</summary>
    private const int Across = 32;

    private readonly Dictionary<string, (Vector2I At, Vector2I Size)> _placed = new();

    public TileSet Set { get; } = new() { TileSize = new Vector2I(Cell, Cell) };

    private static ShipTiles? _shared;

    /// <summary>One atlas for the views that draw what is not built yet (plans, ghosts): made on first use.</summary>
    public static ShipTiles Shared => _shared ??= new();

    public ShipTiles()
    {
        var pictures = new List<(string Name, Image Diffuse, Image Normal)>();
        foreach (var material in Materials.All.Select(m => m.Id))
        {
            pictures.Add(Picture($"wall_{material}", relief: true));
            float edge = MeanLuminance(PixelArt.Picture($"wall_{material}").GetImage());
            for (int open = 1; open < 16; open++) pictures.Add(Picture($"wall-edge_{material}_{open}", relief: true, edge));
        }
        foreach (var name in (string[])["back_scrap", "back_iron_plain", "back_iron_grille", "floor", "ladder"])
            pictures.Add(Picture(name, relief: false));
        for (int linked = 0; linked < 16; linked++)
        {
            pictures.Add(Picture($"door_closed_{linked}", relief: true));
            pictures.Add(Picture($"door_open_{linked}", relief: true));
            pictures.Add(Picture($"pipe_{linked}", relief: false));
        }
        pictures.Add(Picture("valve_open", relief: false, centred: true));
        pictures.Add(Picture("valve_closed", relief: false, centred: true));
        foreach (var kind in Machines.All.Select(m => m.Kind)) pictures.Add(Picture($"machine_{kind}", relief: true));

        // Shelves of pictures, tallest first, a cell at a time.
        var order = pictures.OrderByDescending(p => p.Diffuse.GetHeight()).ThenBy(p => p.Name, System.StringComparer.Ordinal).ToList();
        int x = 0, y = 0, shelf = 0;
        foreach (var (name, diffuse, _) in order)
        {
            var size = new Vector2I(diffuse.GetWidth() / Cell, diffuse.GetHeight() / Cell);
            if (x + size.X > Across) (x, y, shelf) = (0, y + shelf, 0);
            _placed[name] = (new Vector2I(x, y), size);
            x += size.X;
            shelf = Mathf.Max(shelf, size.Y);
        }
        int down = y + shelf;
        var atlas = Image.CreateEmpty(Across * Cell, down * Cell, false, Image.Format.Rgba8);
        var normals = Image.CreateEmpty(Across * Cell, down * Cell, false, Image.Format.Rgba8);
        normals.Fill(Flat);
        foreach (var (name, diffuse, normal) in pictures)
        {
            var at = _placed[name].At * Cell;
            atlas.BlitRect(diffuse, new Rect2I(Vector2I.Zero, diffuse.GetSize()), at);
            normals.BlitRect(normal, new Rect2I(Vector2I.Zero, normal.GetSize()), at);
        }

        var source = new TileSetAtlasSource
        {
            Texture = new CanvasTexture
            {
                DiffuseTexture = ImageTexture.CreateFromImage(atlas),
                NormalTexture = ImageTexture.CreateFromImage(normals),
            },
            TextureRegionSize = new Vector2I(Cell, Cell),
        };
        Set.AddSource(source, 0);
        foreach (var (name, (at, size)) in _placed)
        {
            bool cut = name.StartsWith("wall_") || name == "back_scrap";
            if (cut)
            {
                for (int j = 0; j < size.Y; j++)
                    for (int i = 0; i < size.X; i++)
                        source.CreateTile(at + new Vector2I(i, j));
                continue;
            }
            source.CreateTile(at, size);
            // A tile is drawn centred on its cell: one larger than a cell is moved so its corner is the cell's.
            source.GetTileData(at, 0).TextureOrigin = -(size - Vector2I.One) * Cell / 2;
        }
    }

    /// <summary>The source every tile is in.</summary>
    public const int Source = 0;

    /// <summary>Where the whole picture <paramref name="name"/> is in the atlas.</summary>
    public Vector2I At(string name) => _placed[name].At;

    /// <summary>Which piece of the picture <paramref name="name"/>, laid over the world from its corner, lies on cell (x, y).</summary>
    public Vector2I CutAt(string name, int x, int y)
    {
        var (at, size) = _placed[name];
        return at + new Vector2I(x % size.X, y % size.Y);
    }

    /// <summary>
    /// The picture <paramref name="name"/> and its normal map, a flat one without relief, filled out to
    /// whole cells: from the top-left (a floor along the top of its cell) or round the middle
    /// (a valve's wheel, as ship-art.ts lays it).
    /// </summary>
    private static (string, Image, Image) Picture(string name, bool relief, float clearHeight = 0, bool centred = false)
    {
        var diffuse = PixelArt.Picture(name).GetImage();
        if (diffuse.IsCompressed()) diffuse.Decompress();
        diffuse.Convert(Image.Format.Rgba8);
        var normal = relief
            ? PixelArt.NormalMap(diffuse, clearHeight, wraps: name.StartsWith("wall_"))
            : Filled(diffuse.GetSize(), Flat);
        var whole = (diffuse.GetSize() + new Vector2I(Cell - 1, Cell - 1)) / Cell * Cell;
        if (whole == diffuse.GetSize()) return (name, diffuse, normal);
        var at = centred ? (whole - diffuse.GetSize()) / 2 : Vector2I.Zero;
        Image Padded(Image from, Color clear)
        {
            var padded = Filled(whole, clear);
            padded.BlitRect(from, new Rect2I(Vector2I.Zero, from.GetSize()), at);
            return padded;
        }
        return (name, Padded(diffuse, Colors.Transparent), Padded(normal, Flat));
    }

    /// <summary>A normal facing straight out of the screen.</summary>
    private static readonly Color Flat = new(0.5f, 0.5f, 1f);

    private static Image Filled(Vector2I size, Color colour)
    {
        var image = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
        image.Fill(colour);
        return image;
    }

    /// <summary>A wall's mean brightness: how high its edge's clear pixels stand, so the bevel rises from the wall under it.</summary>
    private static float MeanLuminance(Image image)
    {
        float sum = 0;
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
                sum += image.GetPixel(x, y).Luminance;
        return sum / (image.GetWidth() * image.GetHeight());
    }
}
