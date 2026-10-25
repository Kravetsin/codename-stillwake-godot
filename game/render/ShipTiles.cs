using System;
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

    /// <summary>
    /// The pixels of a cell the art was drawn at, 16 (0104): sheets of tops and details by its columns; the pictures of a cell
    /// alone, pipes, doors, hatches, valves and the hull's edges, drawn at half their size till they are drawn at 8.
    /// </summary>
    public const int Drawn = Camera.Drawn;

    /// <summary>The atlas's width, in cells.</summary>
    private const int Across = 64;

    private readonly Dictionary<string, (Vector2I At, Vector2I Size)> _placed = new();

    public TileSet Set { get; } = new() { TileSize = new Vector2I(Cell, Cell) };

    private static ShipTiles? _shared;

    /// <summary>One atlas for the views that draw what is not built yet (plans, ghosts): made on first use.</summary>
    public static ShipTiles Shared => _shared ??= new();

    public ShipTiles()
    {
        var pictures = new List<(string Name, Image Diffuse, Image Normal)>();
        // A planet's ground (0079): its walls and back walls drawn in art/planet, their edges made here; by name as the hull's are.
        // A vein is drawn as the stone it runs through (Material.In): its wall that stone's flecked with its ore, the rest that stone's.
        static string ArtOf(string natural) => Materials.Get(natural).In ?? natural;
        foreach (var natural in Materials.All.Where(m => m.Natural).Select(m => m.Id))
        {
            var wall = Art($"planet/wall_{ArtOf(natural)}");
            Made($"wall_{natural}", ArtOf(natural) == natural ? wall : GroundArt.Vein(wall, natural));
            for (int open = 1; open < 16; open++) Made($"wall-edge_{natural}_{open}", GroundArt.Edge(natural, open));
            // Its back wall recedes behind it, paler, cooler and darker (0088).
            Made(BackName(natural), GroundArt.Recede(Art($"planet/back_{ArtOf(natural)}")));
        }
        foreach (var natural in Materials.All.Where(m => m.Natural).Select(m => m.Id))
        {
            // Its living edge, up into the cell above (0082): two 16 px cells tall, behind the heroes and before them; each 16 px
            // column in two halves, a cell each (0104), a cell showing the half its column's parity picks.
            var tops = Art($"planet/top_{ArtOf(natural)}");
            for (int v = 0; v < GroundArt.TopVariants; v++)
                foreach (bool front in (bool[])[false, true])
                    for (int half = 0; half < 2; half++)
                    {
                        var top = tops.GetRegion(new Rect2I(v * Drawn + half * Cell, front ? 2 * Drawn : 0, Cell, 2 * Drawn));
                        // A row with nothing in it (bedrock has no tufts before the heroes) is no tile.
                        if (!top.IsInvisible()) pictures.Add(Picture(TopName(natural, v, front, half), top, Relief.None));
                    }
            // Its details over its whole 16 px cells, each cut into the cells it covers: singles along the sheet's first row, features over
            // several on the second.
            var details = Art($"planet/detail_{ArtOf(natural)}");
            for (int k = 0; k < GroundDetails.Singles; k++)
                pictures.Add(Picture(DetailName(natural, k), details.GetRegion(new Rect2I(k * Drawn, 0, Drawn, Drawn)), Relief.None));
            for (int f = 0, at = 0; f < GroundDetails.Features.Length; at += GroundDetails.Features[f++])
            {
                int side = GroundDetails.Features[f] * Drawn;
                pictures.Add(Picture(DetailName(natural, GroundDetails.Singles + f), details.GetRegion(new Rect2I(at * Drawn, Drawn, side, side)), Relief.None));
            }
        }
        foreach (var material in Materials.All.Select(m => m.Id))
        {
            // Walls are no things standing in the light: flat.
            pictures.Add(Picture($"wall_{material}", Relief.None));
            for (int open = 1; open < 16; open++) pictures.Add(Picture($"wall-edge_{material}_{open}", Relief.None));
        }
        foreach (var name in (string[])["back_scrap", "back_iron_plain", "back_iron_grille", .. StructureTiles])
            pictures.Add(Picture(name, Relief.None));
        foreach (var natural in Materials.All.Where(m => m.Natural)) pictures.Add(Picture(BackName(natural.Id), Relief.None));
        for (int linked = 0; linked < 16; linked++)
        {
            pictures.Add(Picture($"door_closed_{linked}", Relief.Across));
            pictures.Add(Picture($"door_open_{linked}", Relief.Across));
            pictures.Add(Picture($"pipe_{linked}", Relief.None));
        }
        foreach (var name in DoorShape.HatchPictures) pictures.Add(Picture(name, Relief.Down));
        // A door's panel part way open, as it slides (a door's relief rolls across it, a hatch's down).
        var pixels = new Dictionary<string, byte[]>();
        byte[]? PixelsOf(string? name) => name == null ? null : pixels.TryGetValue(name, out var known) ? known : pixels[name] = Art(name).GetData();
        foreach (var p in DoorShape.SlidPictures)
        {
            var slid = DoorShape.Slid(PixelsOf(p.Closed)!, PixelsOf(p.Open)!, PixelsOf(p.OverClosed)!, PixelsOf(p.OverOpen)!,
                PixelsOf(p.NextClosed), PixelsOf(p.NextOpen), Drawn, p.Shift, p.Hatch);
            pictures.Add(Picture(p.Name, Image.CreateFromData(Drawn, Drawn, false, Image.Format.Rgba8, slid), p.Hatch ? Relief.Down : Relief.Across));
        }
        // A piece whole (0104), laid from its top-left as a machine is: what a plan, a ghost or a mark of one draws.
        foreach (var kind in Builds.All.Where(b => Builds.PieceOf(b.Id) != null).Select(b => b.Id))
            pictures.Add(Picture(PieceName(kind), Made(PieceName(kind), Whole(kind)), Relief.None));
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
            // Cut a cell at a time, a cell drawing the piece where it lies: sheets laid over the world, and the 16 px pictures of what
            // fills a map's character's cells, two by two or two across (0104): a ladder, a floor, iron's panels.
            bool cut = name.StartsWith("wall_") || name.StartsWith("detail_") || name == "back_scrap" || name.StartsWith("back_") && MadeImages.ContainsKey(name)
                || CutDrawn(name) || name.StartsWith("door_") || name.StartsWith("hatch_");
            if (cut)
            {
                for (int j = 0; j < size.Y; j++)
                    for (int i = 0; i < size.X; i++)
                        source.CreateTile(at + new Vector2I(i, j));
                continue;
            }
            source.CreateTile(at, size);
            // A tile is drawn centred on its cell: one larger than a cell is moved so its corner is the cell's; a machine's picture
            // taller than its footprint stands up out of it, its bottom the footprint's (0099); one with an exhaust reaches out of its
            // left side, its right the footprint's.
            var spec = name.StartsWith("machine_") && Machines.TryGet(name["machine_".Length..], out var m) ? m : null;
            int rise = spec != null ? Mathf.Max(0, size.Y - spec.Height) : 0, reach = spec?.Exhaust ?? 0;
            source.GetTileData(at, 0).TextureOrigin = -(size - Vector2I.One) * Cell / 2 + new Vector2I(reach * Cell, rise * Cell);
        }
    }

    /// <summary>The 16 px pictures cut into the cells of what fills a character's cells (0104): a ladder, a floor, iron's back walls.</summary>
    public static bool CutDrawn(string name) => name is "back_iron_plain" or "back_iron_grille" || StructureTiles.Contains(name);

    /// <summary>
    /// The pictures structures are cut from over the world (<see cref="StructureDraw.Cut"/>, <see cref="StructureDraw.Paired"/>,
    /// <see cref="StructureDraw.Joined"/>): a floor's, a ladder's, a launch platform's by its joins.
    /// </summary>
    private static string[] StructureTiles => _structureTiles ??= Structures.All
        .SelectMany(s => s.Tile is not { } tile ? Array.Empty<string>()
            : s.Draw == StructureDraw.Joined ? JoinedLinks.Select(l => JoinedName(tile, l)).Prepend(tile).ToArray() : [tile])
        .Distinct().ToArray();

    /// <summary>A joined structure's pictures by its piece's joins, a hatch's links: alone, on the right, both sides, on the left.</summary>
    private static readonly int[] JoinedLinks = [0, 2, 10, 8];

    /// <summary>The picture of a <see cref="StructureDraw.Joined"/> piece of <paramref name="tile"/> joined on <paramref name="links"/> (left 8, right 2).</summary>
    public static string JoinedName(string tile, int links) => $"{tile}_{links}";
    private static string[]? _structureTiles;

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

    /// <summary>The pictures made here rather than drawn (the ground's edges, those drawn at 16 px halved), by name, as pixels and as textures.</summary>
    private static readonly Dictionary<string, Image> MadeImages = new();
    private static readonly Dictionary<string, Texture2D> MadeTextures = new();

    private static Image Made(string name, Image image) => MadeImages[name] = image;

    /// <summary>
    /// The picture <paramref name="name"/> of the atlas, before it was padded into it: one drawn
    /// (<see cref="PixelArt.Picture"/>), or one made here, for what draws a plan's pictures alone
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

    /// <summary>
    /// The picture <c>res://art/{name}.png</c> as pixels to work on; one of a cell alone drawn at 16 px (<see cref="Halved"/>) at half
    /// its size, kept as made so every reader of it (a plan, an outline, the light's panels) sees the same.
    /// </summary>
    private static Image Art(string name)
    {
        if (MadeImages.TryGetValue(name, out var made)) return made;
        var image = PixelArt.Picture(name).GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        if (!Halved(name)) return image;
        // A pipe keeps its 16 px thickness: its middle cell of the picture, the arms running on to the edges as they did.
        if (name.StartsWith("pipe_", StringComparison.Ordinal)) image = image.GetRegion(new Rect2I((Drawn - Cell) / 2, (Drawn - Cell) / 2, Cell, Cell));
        else image.Resize(Mathf.Max(1, image.GetWidth() * Cell / Drawn), Mathf.Max(1, image.GetHeight() * Cell / Drawn), Image.Interpolation.Nearest);
        return Made(name, image);
    }

    /// <summary>
    /// Whether <paramref name="name"/> is a picture of one cell drawn at 16 px, now drawn at half its size in the 8 px cell (0104), till
    /// it is drawn anew: the hull's edges, and a pipe's middle cut out at its own thickness. A valve's wheel, a door and a hatch are whole.
    /// </summary>
    public static bool Halved(string name) =>
        name.StartsWith("pipe_", StringComparison.Ordinal)
        || name.StartsWith("wall-edge_", StringComparison.Ordinal);

    /// <summary>
    /// The picture <paramref name="name"/> at the 16 px it was drawn at (0104), for a build card's, which shows it as it was before
    /// the cell was halved: what <see cref="Halved"/> halves at its own size, a ground's edges made at it; every other as it is.
    /// </summary>
    public static Image DrawnImageOf(string name)
    {
        if (!Halved(name)) return ImageOf(name);
        if (name.StartsWith("wall-edge_", StringComparison.Ordinal) && name.Split('_') is [_, var material, var open]
            && Materials.TryGet(material, out var m) && m.Natural)
            return GroundArt.Edge(material, int.Parse(open), Drawn);
        var image = PixelArt.Picture(name).GetImage();
        if (image.IsCompressed()) image.Decompress();
        image.Convert(Image.Format.Rgba8);
        return image;
    }

    /// <summary>The picture of a piece of <paramref name="kind"/> whole (0104): a ladder's or a door segment's.</summary>
    public static string PieceName(string kind) => $"piece_{kind}";

    /// <summary>
    /// A piece of <paramref name="kind"/> whole, its 16 px picture: a ladder's, a door segment's shut, a door one segment long, or
    /// a hatch segment's, a hatch one segment long, its one cell's height (0104).
    /// </summary>
    private static Image Whole(string kind)
    {
        var spec = Structures.ByBuild(kind)!;
        if (spec.Draw != StructureDraw.Door) return (Image)Art(spec.Tile!).Duplicate();
        var build = Builds.Get(kind);
        return Art(DoorShape.Whole(spec.Join == Join.Row, 1)[0]).GetRegion(new Rect2I(0, 0, build.Across * Cell, build.Tall * Cell));
    }

    /// <summary>The back wall of a natural <paramref name="material"/> (0077), a tile cut a cell at a time as scrap's is.</summary>
    public static string BackName(string material) => $"back_{material}";

    /// <summary>
    /// The living edge of a natural <paramref name="material"/>, variant <paramref name="variant"/>, laid from the cell above (0082):
    /// the row behind the heroes, or the one <paramref name="front"/> of them.
    /// </summary>
    public static string TopName(string material, int variant, bool front, int half) =>
        $"top_{material}_{variant}_{(front ? "front" : "back")}_{half}";

    /// <summary>Detail <paramref name="picture"/> of a natural <paramref name="material"/> (<see cref="GroundDetails"/>), a tile cut a cell at a time.</summary>
    public static string DetailName(string material, int picture) => $"detail_{material}_{picture}";

    private static readonly Dictionary<string, int> Rises = new();

    /// <summary>
    /// How many cells machine <paramref name="kind"/>'s picture stands above its footprint (0099): its bottom rows are the
    /// footprint, the rest rises over the cells above, as the arc bench's vise does; none for most, and for no machine.
    /// </summary>
    public static int RiseOf(string kind)
    {
        if (Rises.TryGetValue(kind, out int kept)) return kept;
        int rise = Machines.TryGet(kind, out var spec) ? Mathf.Max(0, (PixelArt.Picture($"machine_{kind}").GetHeight() + Cell - 1) / Cell - spec.Height) : 0;
        return Rises[kind] = rise;
    }

    /// <summary>
    /// How many cells machine <paramref name="kind"/>'s picture reaches out of its footprint's left side: an engine's nozzle through the
    /// stern's wall (<c>MachineSpec.Exhaust</c>); none for most, and for no machine.
    /// </summary>
    public static int ReachOf(string kind) => Machines.TryGet(kind, out var spec) ? spec.Exhaust ?? 0 : 0;

    /// <summary>Whether the atlas holds the picture <paramref name="name"/>: an empty one is left out.</summary>
    public bool Has(string name) => _placed.ContainsKey(name);

    /// <summary>A wall of <paramref name="material"/>'s picture.</summary>
    public static string WallName(string material) => $"wall_{material}";

    /// <summary>The edges of a wall of <paramref name="material"/> on its sides <paramref name="open"/>.</summary>
    public static string EdgeName(string material, int open) => $"wall-edge_{material}_{open}";

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
