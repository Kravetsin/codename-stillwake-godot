using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Sim.Grid;

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
            // Walls are no things standing in the light: flat.
            pictures.Add(Picture($"wall_{material}", Relief.None));
            for (int open = 1; open < 16; open++) pictures.Add(Picture($"wall-edge_{material}_{open}", Relief.None));
            // Walls cut to a shape (0069): the same pictures, each cell cut, the diagonal edged as an open side is.
            foreach (int shape in Shape.Cut)
            {
                pictures.Add(Picture(WallName(material, shape), Made(WallName(material, shape), ShapeMasks.Cut(Art($"wall_{material}"), shape, Art($"wall-edge_{material}_1"))), Relief.None));
                for (int open = 1; open < 16; open++)
                    if ((open & ~Shape.ClosedSides(shape)) == 0)
                        pictures.Add(Picture(EdgeName(material, open, shape), Made(EdgeName(material, open, shape), ShapeMasks.Cut(Art($"wall-edge_{material}_{open}"), shape)), Relief.None));
            }
        }
        foreach (var name in (string[])["back_scrap", "back_iron_plain", "back_iron_grille", "floor", "ladder"])
            pictures.Add(Picture(name, Relief.None));
        for (int linked = 0; linked < 16; linked++)
        {
            pictures.Add(Picture($"door_closed_{linked}", Relief.Across));
            pictures.Add(Picture($"door_open_{linked}", Relief.Across));
            pictures.Add(Picture($"pipe_{linked}", Relief.None));
        }
        foreach (var name in DoorShape.HatchPictures) pictures.Add(Picture(name, Relief.Down));
        pictures.Add(Picture("valve_open", Relief.None, centred: true));
        pictures.Add(Picture("valve_closed", Relief.None, centred: true));
        foreach (var kind in Machines.All.Select(m => m.Kind)) pictures.Add(Picture($"machine_{kind}", Relief.Round));

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

        Texture = new CanvasTexture
        {
            DiffuseTexture = ImageTexture.CreateFromImage(atlas),
            NormalTexture = ImageTexture.CreateFromImage(normals),
        };
        var source = new TileSetAtlasSource
        {
            Texture = Texture,
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

    /// <summary>The atlas with its normal map, for pictures drawn from it a piece at a time (a plan being built).</summary>
    public CanvasTexture Texture { get; }

    /// <summary>Where the whole picture <paramref name="name"/> is in the atlas.</summary>
    public Vector2I At(string name) => _placed[name].At;

    /// <summary>How many cells the picture <paramref name="name"/> spans, across and down: what a tile of it covers from its cell.</summary>
    public Vector2I SizeOf(string name) => _placed[name].Size;

    /// <summary>Which piece of the picture <paramref name="name"/>, laid over the world from its corner, lies on cell (x, y).</summary>
    public Vector2I CutAt(string name, int x, int y)
    {
        var (at, size) = _placed[name];
        return at + new Vector2I(x % size.X, y % size.Y);
    }

    /// <summary>
    /// The picture <paramref name="name"/> and its normal map (<see cref="PixelArt.VolumeMap"/>), a flat one without relief, filled out to
    /// whole cells: from the top-left (a floor along the top of its cell) or round the middle
    /// (a valve's wheel, as ship-art.ts lays it).
    /// </summary>
    private static (string, Image, Image) Picture(string name, Relief relief, bool centred = false) => Picture(name, Art(name), relief, centred);

    /// <summary>The picture <paramref name="diffuse"/>, made here, as <paramref name="name"/>.</summary>
    private static (string, Image, Image) Picture(string name, Image diffuse, Relief relief, bool centred = false)
    {
        var normal = relief == Relief.None ? Filled(diffuse.GetSize(), Flat) : PixelArt.VolumeMap(diffuse, across: relief == Relief.Across, down: relief == Relief.Down);
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

    /// <summary>The pictures made here rather than drawn (walls cut to a shape), by name, as pixels and as textures.</summary>
    private static readonly Dictionary<string, Image> MadeImages = new();
    private static readonly Dictionary<string, Texture2D> MadeTextures = new();

    private static Image Made(string name, Image image) => MadeImages[name] = image;

    /// <summary>
    /// The picture <paramref name="name"/> of the atlas, before it was padded into it: one drawn
    /// (<see cref="PixelArt.Picture"/>), or one made here, a wall cut to a shape (0069), for what draws a plan's pictures alone
    /// (a thumbnail, a silhouette, the aim's outline).
    /// </summary>
    public static Texture2D PictureOf(string name)
    {
        if (MadeTextures.TryGetValue(name, out var made)) return made;
        if (!MadeImages.ContainsKey(name) && _shared == null) _ = Shared;
        return MadeImages.TryGetValue(name, out var image) ? MadeTextures[name] = ImageTexture.CreateFromImage(image) : PixelArt.Picture(name);
    }

    /// <summary><see cref="PictureOf"/> as pixels to work on, RGBA8.</summary>
    public static Image ImageOf(string name)
    {
        if (!MadeImages.ContainsKey(name) && _shared == null) _ = Shared;
        return MadeImages.TryGetValue(name, out var image) ? image : Art(name);
    }

    /// <summary>The picture <c>res://art/{name}.png</c> as pixels to work on.</summary>
    private static Image Art(string name)
    {
        var image = PixelArt.Picture(name).GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        return image;
    }

    /// <summary>A wall of <paramref name="material"/>'s picture, cut to <paramref name="shape"/> (0069).</summary>
    public static string WallName(string material, int shape) => shape == Shape.None ? $"wall_{material}" : $"wall_{material}_{shape}";

    /// <summary>The edges of a wall of <paramref name="material"/> on its sides <paramref name="open"/>, cut to <paramref name="shape"/>: only its closed sides have one.</summary>
    public static string EdgeName(string material, int open, int shape) =>
        shape == Shape.None ? $"wall-edge_{material}_{open}" : $"wall-edge_{material}_{open}_{shape}";

    /// <summary>How a picture stands out toward a light: flat, rolled from side to side (a door), from top to bottom (a hatch), or domed.</summary>
    private enum Relief { None, Across, Down, Round }

    /// <summary>A normal facing straight out of the screen.</summary>
    private static readonly Color Flat = new(0.5f, 0.5f, 1f);

    private static Image Filled(Vector2I size, Color colour)
    {
        var image = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
        image.Fill(colour);
        return image;
    }
}
