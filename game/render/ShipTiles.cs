using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Sim;

namespace Stillwake.Game.Render;

/// <summary>
/// Every picture of the ship's cells packed into one atlas, with one normal map beside it, as the single
/// source of one <see cref="TileSet"/>: a layer's piece of the screen is then one draw, whatever
/// pictures it holds. A picture is cut into cells (walls, scrap back walls) or laid whole from a cell's
/// top-left (doors, pipes, machines); a valve, smaller than a cell, sits in the middle of one. Its specular map is black but where a
/// vein's ore is (<see cref="OreMask"/>), for <c>render/Lit.cs</c>'s shine.
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

    /// <summary>The one atlas every view draws cells from, built and planned alike: made on first use, never changed after.</summary>
    public static ShipTiles Shared => _shared ??= new();

    private ShipTiles()
    {
        var pictures = new List<(string Name, Image Diffuse, Image Normal)>();
        // A planet's ground (0079): its walls and back walls drawn by name as the hull's are, a vein's its own; their edges made here.
        foreach (var natural in Materials.All.Where(m => m.Natural).Select(m => m.Id))
        {
            Made($"wall_{natural}", Art($"wall_{natural}"));
            for (int open = 1; open < 16; open++) Made($"wall-edge_{natural}_{open}", GroundArt.Edge(natural, open));
            Made(BackName(natural), Art(BackName(natural)));
        }
        foreach (var natural in Materials.All.Where(m => m.Natural).Select(m => m.Id))
        {
            // Its living edge, up into the cell above (0082): two 16 px cells tall, behind the heroes and before them; each 16 px
            // column in two halves, a cell each (0104), a cell showing the half its column's parity picks. One not drawn (bedrock
            // has no tufts before the heroes) is no tile.
            for (int v = 0; v < GroundArt.TopVariants; v++)
                foreach (bool front in (bool[])[false, true])
                    for (int half = 0; half < 2; half++)
                        if (TopName(natural, v, front, half) is var name && HiRes.Has(name) && !HiRes.Load(name)!.IsInvisible())
                            pictures.Add(Picture(name, Relief.None));
            // Its details over its whole 16 px cells, each cut into the cells it covers: singles a 16 px cell, features over several. A
            // material with none drawn (plaster) has them clear.
            for (int k = 0; k < GroundDetails.Singles + GroundDetails.Features.Length; k++)
            {
                string name = DetailName(natural, k);
                int side = (k < GroundDetails.Singles ? 1 : GroundDetails.Features[k - GroundDetails.Singles]) * Drawn;
                pictures.Add(Picture(name, HiRes.Has(name) ? Art(name) : Image.CreateEmpty(side, side, false, Image.Format.Rgba8), Relief.None));
            }
        }
        foreach (var material in Materials.All.Where(m => m.As == null).Select(m => m.Id))
        {
            // Walls are no things standing in the light: flat.
            pictures.Add(Picture($"wall_{material}", Relief.None));
            for (int open = 1; open < 16; open++) pictures.Add(Picture($"wall-edge_{material}_{open}", Relief.None));
        }
        var fineMade = new Dictionary<string, Image>();
        AddWreckDress(pictures, fineMade);
        foreach (var name in (string[])[.. HullBacks, "back_iron_plain", "back_iron_grille", .. StructureTiles])
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
            if (FineSlid(p) is { } fine) fineMade[p.Name] = fine;
        }
        // A piece whole (0104), laid from its top-left as a machine is: what a plan, a ghost or a mark of one draws.
        foreach (var kind in Builds.All.Where(b => Builds.PieceOf(b.Id) != null).Select(b => b.Id))
        {
            pictures.Add(Picture(PieceName(kind), Made(PieceName(kind), Whole(kind)), Relief.None));
            if (FineWhole(kind) is { } fine) fineMade[PieceName(kind)] = fine;
        }
        pictures.Add(Picture("valve_open", Relief.None, centred: true));
        pictures.Add(Picture("valve_closed", Relief.None, centred: true));
        foreach (var kind in Machines.All.Select(m => m.Kind)) pictures.Add(Picture($"machine_{kind}", Relief.Round));

        // Shelves of pictures, tallest first, a cell at a time.
        var order = pictures.OrderByDescending(p => p.Diffuse.GetHeight()).ThenBy(p => p.Name, System.StringComparer.Ordinal).ToList();
        // A cell clear round each picture: smooth sampling and mipmaps reach past a picture's edge.
        const int clear = 1;
        int x = clear, y = clear, shelf = 0;
        foreach (var (name, diffuse, _) in order)
        {
            var size = new Vector2I(diffuse.GetWidth() / Cell, diffuse.GetHeight() / Cell);
            if (x + size.X + clear > Across) (x, y, shelf) = (clear, y + shelf, 0);
            _placed[name] = (new Vector2I(x, y), size);
            x += size.X + clear;
            shelf = Mathf.Max(shelf, size.Y + clear);
        }
        int down = y + shelf;
        const int S = HiRes.Scale;
        var atlas = Image.CreateEmpty(Across * Cell * S, down * Cell * S, false, Image.Format.Rgba8);
        var normals = Image.CreateEmpty(Across * Cell, down * Cell, false, Image.Format.Rgba8);
        normals.Fill(Flat);
        // Black but where a vein's ore lies: what its picture adds to its host's sheet, in its fleck's colour.
        var ores = Image.CreateEmpty(atlas.GetWidth(), atlas.GetHeight(), false, Image.Format.Rgba8);
        ores.Fill(Colors.Black);
        foreach (var (name, diffuse, normal) in pictures)
        {
            var at = _placed[name].At * Cell;
            normals.BlitRect(normal, new Rect2I(Vector2I.Zero, normal.GetSize()), at);
            var fine = fineMade.GetValueOrDefault(name) ?? HiRes.Load(name);
            if (fine != null && fine.GetSize() != diffuse.GetSize() * S && fine.GetSize().X % (Cell * S) != 0 && !name.StartsWith("valve_", StringComparison.Ordinal)) GD.PushError($"hi-res {name}: {fine.GetSize()} for {diffuse.GetSize()}");
            if (fine == null)
            {
                fine = (Image)diffuse.Duplicate();
                fine.Resize(diffuse.GetWidth() * S, diffuse.GetHeight() * S, Image.Interpolation.Nearest);
            }
            fine.FixAlphaEdges();
            var box = new Rect2I(at * S, diffuse.GetSize() * S);
            // A valve is centred in its padded cells (see Picture), so its smaller drawing is too; every other lies from the top-left
            // as its padded picture does, or a floor 4 px thick would sink 2 px below where heroes and machines stand on it.
            var inset = name.StartsWith("valve_", StringComparison.Ordinal) ? (box.Size - fine.GetSize()) / 2 : Vector2I.Zero;
            atlas.BlitRect(fine, new Rect2I(Vector2I.Zero, fine.GetSize()), box.Position + inset);
            Extrude(atlas, box, Cell * S / 2);
            if (OreOf(name, fine) is { } ore)
            {
                ores.BlitRect(ore, new Rect2I(Vector2I.Zero, ore.GetSize()), box.Position);
                Extrude(ores, box, Cell * S / 2);
            }
        }
        atlas.GenerateMipmaps();
        ores.GenerateMipmaps();

        Texture = HiRes.Sampled(ImageTexture.CreateFromImage(atlas), new Vector2I(Across * Cell, down * Cell), ImageTexture.CreateFromImage(normals), ImageTexture.CreateFromImage(ores));
        var source = new TileSetAtlasSource
        {
            Texture = Texture,
            TextureRegionSize = new Vector2I(Cell, Cell),
            // Godot's padding copies pictures by the size the atlas reports, not its pixels: Extrude does this instead.
            UseTexturePadding = false,
        };
        Set.AddSource(source, 0);
        foreach (var (name, (at, size)) in _placed)
        {
            // Cut a cell at a time, a cell drawing the piece where it lies: sheets laid over the world, and the 16 px pictures of what
            // fills a map's character's cells, two by two or two across (0104): a ladder, a floor, iron's panels.
            bool cut = name.StartsWith("wall_") || name.StartsWith("detail_") || HullBacks.Contains(name) || name.StartsWith("back_") && MadeImages.ContainsKey(name)
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

    /// <summary>
    /// The ore of picture <paramref name="name"/>, drawn <paramref name="fine"/>, if it is a vein's wall (any <c>Material.In</c>): what it adds
    /// to its host's wall, in its fleck; none for every other picture, or a vein whose sheet is not its host's size.
    /// </summary>
    private static Image? OreOf(string name, Image fine)
    {
        if (!name.StartsWith("wall_", StringComparison.Ordinal) || !Materials.TryGet(name["wall_".Length..], out var vein) || vein.In == null) return null;
        if (HiRes.Load(WallName(vein.In)) is not { } host || host.GetSize() != fine.GetSize()) return null;
        var mask = OreMask.Of(fine.GetData(), host.GetData(), GroundLooks.Of[vein.Id].Fleck);
        return Image.CreateFromData(fine.GetWidth(), fine.GetHeight(), false, Image.Format.Rgba8, mask);
    }

    /// <summary>A door's panel part way open from the hi-res pictures, its panel told by what differs between shut and open; none unless all of them are drawn.</summary>
    private static Image? FineSlid(DoorShape.SlidPicture p)
    {
        byte[]? Fine(string? name) => name != null && HiRes.Load(name) is { } image ? image.GetData() : null;
        int Side = Drawn * HiRes.Scale;
        if (Fine(p.Closed) is not { } closed || Fine(p.Open) is not { } open || Fine(p.OverClosed) is not { } overClosed || Fine(p.OverOpen) is not { } overOpen) return null;
        var slid = DoorShape.Slid(closed, open, overClosed, overOpen, Fine(p.NextClosed), Fine(p.NextOpen), Side, p.Shift * HiRes.Scale, p.Hatch);
        return Image.CreateFromData(Side, Side, false, Image.Format.Rgba8, slid);
    }

    /// <summary>Copies the edges of <paramref name="box"/> outward <paramref name="by"/> pixels, so a tile sampled past its edge meets itself, not clear.</summary>
    private static void Extrude(Image atlas, Rect2I box, int by)
    {
        var (x, y, w, h) = (box.Position.X, box.Position.Y, box.Size.X, box.Size.Y);
        for (int i = 1; i <= by; i++)
        {
            atlas.BlitRect(atlas, new Rect2I(x, y, 1, h), new Vector2I(x - i, y));
            atlas.BlitRect(atlas, new Rect2I(x + w - 1, y, 1, h), new Vector2I(x + w - 1 + i, y));
        }
        for (int i = 1; i <= by; i++)
        {
            atlas.BlitRect(atlas, new Rect2I(x - by, y, w + 2 * by, 1), new Vector2I(x - by, y - i));
            atlas.BlitRect(atlas, new Rect2I(x - by, y + h - 1, w + 2 * by, 1), new Vector2I(x - by, y + h - 1 + i));
        }
    }

    /// <summary>The wreck's dress (<see cref="WreckDress"/>): a picture a cell, a torn one the piece of scrap's wall that lies there in a new world, cut by its mask.</summary>
    private static void AddWreckDress(List<(string Name, Image Diffuse, Image Normal)> pictures, Dictionary<string, Image> fine)
    {
        Image over = Art("wreck_dress"), masks = Art("wreck_tears"), scrap = Art("wall_scrap");
        Image? fineOver = HiRes.Load("wreck_dress"), fineMasks = HiRes.Load("wreck_tears"), fineScrap = HiRes.Load("wall_scrap");
        int side = Cell * HiRes.Scale;
        var corner = Layout.OffsetInWorld(Wreck.Width, Wreck.Height);
        var (across, down) = (scrap.GetWidth() / Cell, scrap.GetHeight() / Cell);
        foreach (var (x, y, look) in WreckDress.Cells())
        {
            var inSheet = new Rect2I(x * Cell, y * Cell, Cell, Cell);
            if (look != 'T')
            {
                pictures.Add(Picture(WreckDress.OverName(x, y), over.GetRegion(inSheet), Relief.None));
                if (fineOver != null) fine[WreckDress.OverName(x, y)] = fineOver.GetRegion(new Rect2I(inSheet.Position * HiRes.Scale, side, side));
                continue;
            }
            var inWall = new Vector2I((x + Wreck.Left + corner.X) % across * Cell, (y + Wreck.Top + corner.Y) % down * Cell);
            var wall = scrap.GetRegion(new Rect2I(inWall, Cell, Cell));
            var torn = WreckDress.Torn(wall.GetData(), masks.GetRegion(inSheet).GetData());
            pictures.Add(Picture(WreckDress.TornName(x, y), Image.CreateFromData(Cell, Cell, false, Image.Format.Rgba8, torn), Relief.None));
            if (fineMasks == null || fineScrap == null) continue;
            var fineTorn = WreckDress.Torn(fineScrap.GetRegion(new Rect2I(inWall * HiRes.Scale, side, side)).GetData(), fineMasks.GetRegion(new Rect2I(inSheet.Position * HiRes.Scale, side, side)).GetData());
            fine[WreckDress.TornName(x, y)] = Image.CreateFromData(side, side, false, Image.Format.Rgba8, fineTorn);
        }
    }

    /// <summary>The hull's back walls laid over the world, cut a cell at a time: scrap's and the seals', every built material drawn cut.</summary>
    private static readonly string[] HullBacks =
        [.. Materials.All.Where(m => !m.Natural && m.Look == MaterialLook.Cut && m.Built).Select(m => BackName(m.Id))];

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
    /// (a valve's wheel).
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

    /// <summary>The picture <paramref name="name"/> at the world's pixels (<see cref="HiRes.Logical"/>), or one made here, to work on.</summary>
    private static Image Art(string name) => MadeImages.TryGetValue(name, out var made) ? made : HiRes.Logical(name);

    /// <summary>
    /// The picture <paramref name="name"/> at the 16 px a cell was drawn at (0104), for a build card's, which shows a pipe and a wall's
    /// edges as they were before the cell was halved: a pipe's cell run across it, its middle; an edge at twice its size. Every other as it is.
    /// </summary>
    public static Image DrawnImageOf(string name)
    {
        if (name.StartsWith("wall-edge_", StringComparison.Ordinal) && name.Split('_') is [_, var material, var open])
            return Materials.TryGet(material, out var m) && m.Natural ? GroundArt.Edge(material, int.Parse(open), Drawn) : HiRes.Logical(name, HiRes.Scale * Cell / Drawn);
        if (!name.StartsWith("pipe_", StringComparison.Ordinal)) return ImageOf(name);
        var cell = Art(name);
        var run = Image.CreateEmpty(Drawn, Drawn, false, Image.Format.Rgba8);
        for (int x = 0; x < Drawn; x += Cell) run.BlitRect(cell, new Rect2I(Vector2I.Zero, cell.GetSize()), new Vector2I(x, (Drawn - Cell) / 2));
        return run;
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

    /// <summary><see cref="Whole"/> in hi-res; null with no such picture.</summary>
    private static Image? FineWhole(string kind)
    {
        var spec = Structures.ByBuild(kind)!;
        if (spec.Draw != StructureDraw.Door) return HiRes.Load(spec.Tile!);
        var build = Builds.Get(kind);
        return HiRes.Load(DoorShape.Whole(spec.Join == Join.Row, 1)[0])?.GetRegion(new Rect2I(0, 0, build.Across * Cell * HiRes.Scale, build.Tall * Cell * HiRes.Scale));
    }

    /// <summary>
    /// The picture <paramref name="name"/>, whose pixels are <paramref name="logical"/>, for what lays pictures together itself (a build
    /// card): at <see cref="HiRes.Scale"/> times those pixels, its hi-res one or them grown.
    /// </summary>
    public static Image FineImageOf(string name, Image logical)
    {
        var fine = name.StartsWith("piece_", StringComparison.Ordinal) ? FineWhole(name["piece_".Length..]) : HiRes.Load(name);
        var want = logical.GetSize() * HiRes.Scale;
        if (fine == null)
        {
            fine = (Image)logical.Duplicate();
            fine.Resize(want.X, want.Y, Image.Interpolation.Nearest);
            return fine;
        }
        if (!name.StartsWith("pipe_", StringComparison.Ordinal) || fine.GetSize() == want) return fine;
        // A card's pipe runs across its 16 px, as DrawnImageOf's does; the hi-res one is its middle cell alone, so that cell repeats.
        var run = Image.CreateEmpty(want.X, want.Y, false, Image.Format.Rgba8);
        for (int x = 0; x < want.X; x += fine.GetWidth()) run.BlitRect(fine, new Rect2I(Vector2I.Zero, fine.GetSize()), new Vector2I(x, (want.Y - fine.GetHeight()) / 2));
        return run;
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
        int rise = Machines.TryGet(kind, out var spec) ? Mathf.Max(0, (HiRes.Load($"machine_{kind}")!.GetHeight() / HiRes.Scale + Cell - 1) / Cell - spec.Height) : 0;
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
