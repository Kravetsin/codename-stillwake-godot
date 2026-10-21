using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Grid;
using L = Stillwake.Game.Light;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Grid;

/// <summary>
/// The ship in pixel art (features/grid in TypeScript), a <see cref="TileMapLayer"/> per kind of
/// picture: back walls behind everything, pipes and valves in front of them, then walls with their
/// edges, floors and ladders, doors, and machines over their footprints with a socket where power
/// plugs in. <see cref="Sync"/> compares what was drawn (<see cref="DrawnLayers"/>) with the cells the grid's
/// log names (0051) and redraws only the cells that changed and their neighbours, whose edges and
/// links reach toward them.
/// </summary>
public partial class GridView : Node2D
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>Which sides of a cell: up, right, down, left, a bit each (SIDE in ship-art.ts).</summary>
    private const int Up = 1, Right = 2, Down = 4, Left = 8;

    private readonly IGridQueries _grid;
    private readonly ShipTiles _tiles = new();
    private readonly TileMapLayer _back, _pipes, _valves, _walls, _details, _edges, _tops, _topsFront, _fixtures, _doors, _machines;
    /// <summary>The layers a cell is drawn in; machines are drawn apart, over their footprints.</summary>
    private readonly TileMapLayer[] _cellLayers;
    private readonly Node2D _sockets = new() { ZIndex = Layers.Sockets };
    // A door or valve that moves is heard where it stands, by everyone near (0033).
    private readonly Heard2D _opens = Cues.Player2D(S.DoorOpen, S.World, voices: 2),
        _closes = Cues.Player2D(S.DoorClose, S.World, voices: 2), _turns = Cues.Player2D(S.Valve, S.World, voices: 2);
    private readonly Dictionary<int, string> _materials = Materials.All.ToDictionary(m => m.Code, m => m.Id);
    private readonly DrawnLayers _drawn;
    private Machine[] _drawnMachines = [];
    private bool _blank = true;
    /// <summary>The door cells whose panels are sliding, redrawn a step at a time; the rest stand still.</summary>
    private readonly DoorSlides _slides = new();

    public GridView(IGridQueries grid)
    {
        _grid = grid;
        _drawn = new DrawnLayers(grid.Width, grid.Height);
        _reveal = new ChunkReveal(grid.Width, grid.Height);
        _drawRow = DrawRow;
        _groundAt = GroundAt;
        // Walls and doors take the share of light a wall keeps of what reaches its face; the rest all of it.
        _back = Layer(Layers.BackWalls);
        _pipes = Layer(Layers.Pipes);
        _valves = Layer(Layers.Valves);
        _walls = Layer(Layers.Walls, L.WallIn);
        // A planet's pebbles and roots over its ground, under its edges: added after the walls, before them (0082).
        _details = Layer(Layers.Walls, L.WallIn);
        _edges = Layer(Layers.Edges, L.WallIn);
        // The living edge over its ground, lit as the open air its top faces; over the edges, as added after them; its tufts before the heroes.
        _tops = Layer(Layers.Edges);
        _topsFront = Layer(Layers.GroundFront);
        _fixtures = Layer(Layers.Fixtures);
        _doors = Layer(Layers.Doors, L.WallIn);
        _machines = Layer(Layers.Machines);
        _cellLayers = [_back, _pipes, _valves, _walls, _details, _edges, _fixtures, _doors];
        AddChild(_sockets);
        foreach (var player in (Node[])[_opens, _closes, _turns]) AddChild(player);
    }

    private TileMapLayer Layer(int z, float share = 1)
    {
        // Cells have no bodies or paths, but Godot would still walk every cell for them each time a layer is built.
        var layer = new TileMapLayer { TileSet = _tiles.Set, ZIndex = z, CollisionEnabled = false, NavigationEnabled = false };
        Lit.Set(layer, share);
        AddChild(layer);
        return layer;
    }

    /// <summary>
    /// Brings the drawing up to the grid as this peer shows it (<paramref name="shown"/>: its own door and
    /// valve toggles in flight already made, 0014): called when that changes, cheap when nothing did.
    /// </summary>
    public void Sync(ShownGrid shown)
    {
        int version = _grid.Version;
        var changes = _drawn.Follow(_grid, shown);
        // The first look: nothing is drawn yet, and nothing moved; the chunks round the camera are drawn as it comes (Reveal).
        if (!_blank)
        {
            foreach (var change in changes)
            {
                Hear(change);
                if (IsDoor(change.Structure) && IsDoor(_drawn.Structure[change.Cell]) && change.Structure != _drawn.Structure[change.Cell])
                    _slides.Moved(change.Cell, _drawn.Structure[change.Cell] == Structure.DoorOpen);
            }
            foreach (int cell in _drawn.Stale(changes))
                if (_reveal.Drawn(cell)) DrawCell(cell, blank: false);
            // A feature over several cells hangs on every cell of its block: one dug, the rest of it goes too.
            foreach (var change in changes)
                if (_blocks.Add(BlockOf(change.Cell))) DrawDetails(change.Cell);
            _blocks.Clear();
        }
        _blank = false;
        if (version != _machinesVersion && MachinesMoved() && !_drawnMachines.SequenceEqual(_grid.Machines)) DrawMachines();
        _machinesVersion = version;
    }

    /// <summary>
    /// Slides the panels of doors that moved on a step, redrawing only the cells whose step changed; each doorway's slide
    /// lit from its leading cell (<see cref="DoorLight"/>).
    /// </summary>
    public override void _Process(double delta)
    {
        long revealed = Game.Dev.FrameLog.Begin();
        Reveal();
        Game.Dev.FrameLog.End("reveal", revealed);
        if (!_slides.Any) return;
        foreach (int cell in _slides.Advance(delta))
        {
            if (!IsDoor(_drawn.Structure[cell])) continue;
            DrawDoor(cell);
            Light(cell);
        }
    }

    /// <summary>
    /// What of the grid is drawn, a chunk at a time as the camera comes near it, ahead of it within a budget a frame (<see cref="ChunkReveal"/>):
    /// a planet is built in nearly every cell, and drawing all of them at once froze its first look for a second (0079).
    /// </summary>
    private readonly ChunkReveal _reveal;
    private readonly Func<int, int, int, int> _drawRow;

    /// <summary>Cells of view round the screen drawn at once: the camera may move after this reads it, a frame's run at most.</summary>
    private const int Pad = 2;

    /// <summary>Draws what the view needs: the chunks on screen whole, those round it ahead a little a frame.</summary>
    private void Reveal()
    {
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewport().GetVisibleRect();
        _reveal.Reveal(new Box((int)Math.Floor(view.Position.X / Cell) - Pad, (int)Math.Floor(view.Position.Y / Cell) - Pad,
            (int)Math.Floor(view.End.X / Cell) + Pad, (int)Math.Floor(view.End.Y / Cell) + Pad), _drawRow);
    }

    /// <summary>Draws the cells of row <paramref name="y"/> from <paramref name="x0"/> to before <paramref name="x1"/> that hold anything; answers how many.</summary>
    private int DrawRow(int y, int x0, int x1)
    {
        int drawn = 0;
        for (int cell = y * _grid.Width + x0, end = cell + x1 - x0; cell < end; cell++)
            if (_drawn.Structure[cell] != Structure.Empty || _drawn.Back[cell] != Back.None || _drawn.Conduit[cell] != Conduit.None)
            {
                DrawCell(cell, blank: true);
                drawn++;
            }
        return drawn;
    }

    /// <summary>A doorway's slide in light, told once from the cell it leads from: its edge while it moves, the slot's or the seal's moment at rest.</summary>
    private void Light(int cell)
    {
        int links = DoorLinksOf(cell);
        if (!DoorShape.Leads(links)) return;
        bool hatch = Doorways.IsHatch(links), opening = _drawn.Structure[cell] == Structure.DoorOpen;
        int length = 1, step = hatch ? 1 : _grid.Width, ahead = hatch ? Doorways.Right : Doorways.Down;
        for (int c = cell; (DoorLinksOf(c) & ahead) != 0; c += step) length++;
        var corner = new Vector2(cell % _grid.Width, cell / _grid.Width) * Cell;
        if (_slides.OpenOf(cell) is double open) DoorLight.Sliding(corner, length, hatch, DoorLight.Ease(open), opening ? open : 1 - open);
        else DoorLight.Rested(corner, length, hatch, opening);
    }

    /// <summary>The grid's version when its machines were last compared: the log since then says whether one came or went.</summary>
    private int _machinesVersion = -1;

    private bool MachinesMoved()
    {
        if (_grid.ChangedSince(_machinesVersion) is not { } changes) return true;
        foreach (var c in changes)
            if ((c.Layers & GridLayers.Machine) != 0) return true;
        return false;
    }

    /// <summary>
    /// A door or a valve that moved, heard there: once a doorway, at the cell it leads from
    /// (<see cref="DoorShape.Leads"/>), since the rest of it moved with it.
    /// </summary>
    private void Hear(DrawnLayers.Change change)
    {
        int cell = change.Cell, w = _grid.Width, structure = _drawn.Structure[cell], conduit = _drawn.Conduit[cell];
        var at = new Vector2((cell % w + 0.5f) * Cell, (cell / w + 0.5f) * Cell);
        if (IsDoor(structure) && IsDoor(change.Structure) && structure != change.Structure && DoorShape.Leads(DoorLinksOf(cell)))
            (structure == Structure.DoorOpen ? _opens : _closes).PlayAt(at);
        if (conduit is Conduit.ValveClosed or Conduit.ValveOpen && change.Conduit is Conduit.ValveClosed or Conduit.ValveOpen && conduit != change.Conduit)
            _turns.PlayAt(at);
    }

    private static bool IsDoor(int s) => Doorways.IsDoor(s);

    /// <summary>The sides door cell <paramref name="cell"/> joins its doorway on, as drawn (<see cref="Doorways.LinksOf"/>).</summary>
    private int DoorLinksOf(int cell) => Doorways.LinksOf(cell, _grid.Width, _drawn.Structure.Length, _drawnAt ??= c => _drawn.Structure[c]);

    private Func<int, int>? _drawnAt;

    private string MaterialOf(int code) => _materials.GetValueOrDefault(code, Materials.DefaultHull);

    // The pictures' names made once, not a string a cell: a big ship draws half a million cells as it loads.
    private static readonly string[] PipeNames = Names("pipe_");
    private readonly Dictionary<(int, int), (string Wall, string[] Edges)> _wallNames = new();

    private static string[] Names(string prefix) => Enumerable.Range(0, 16).Select(sides => $"{prefix}{sides}").ToArray();

    /// <summary>A wall of material <paramref name="code"/> cut to <paramref name="shape"/>: its picture, and its edges' by the sides open.</summary>
    private (string Wall, string[] Edges) WallNamesOf(int code, int shape)
    {
        if (_wallNames.TryGetValue((code, shape), out var names)) return names;
        string material = MaterialOf(code);
        var edges = Enumerable.Range(0, 16).Select(open => ShipTiles.EdgeName(material, open, shape)).ToArray();
        return _wallNames[(code, shape)] = (ShipTiles.WallName(material, shape), edges);
    }

    /// <summary>The sides of <paramref name="cell"/> a wall beside it closes: one whose own side facing it is whole (0069).</summary>
    private int WalledSides(int cell)
    {
        int w = _grid.Width, x = cell % w, y = cell / w, sides = 0;
        bool Closes(int n, int facing) => _drawn.Structure[n] == Structure.Wall && (Shape.ClosedSides(_drawn.Shape[n]) & facing) != 0;
        if (y > 0 && Closes(cell - w, Down)) sides |= Up;
        if (x < w - 1 && Closes(cell + 1, Left)) sides |= Right;
        if (y < _grid.Height - 1 && Closes(cell + w, Up)) sides |= Down;
        if (x > 0 && Closes(cell - 1, Right)) sides |= Left;
        return sides;
    }

    /// <summary>The sides of <paramref name="cell"/> whose neighbour passes <paramref name="isOne"/>; past the grid's edge none does.</summary>
    private int SidesOf(int cell, int[] layer, Func<int, bool> isOne)
    {
        int w = _grid.Width, x = cell % w, y = cell / w, sides = 0;
        if (y > 0 && isOne(layer[cell - w])) sides |= Up;
        if (x < w - 1 && isOne(layer[cell + 1])) sides |= Right;
        if (y < _grid.Height - 1 && isOne(layer[cell + w])) sides |= Down;
        if (x > 0 && isOne(layer[cell - 1])) sides |= Left;
        return sides;
    }

    /// <summary>Draws <paramref name="cell"/> anew; <paramref name="blank"/> when nothing was drawn there yet, so nothing need be erased.</summary>
    private void DrawCell(int cell, bool blank)
    {
        int x = cell % _grid.Width, y = cell / _grid.Width;
        var at = new Vector2I(x, y);
        if (!blank)
        {
            foreach (var layer in _cellLayers) layer.EraseCell(at);
            // The living edge is laid from the cell above its ground, and only this cell's drawing lays it there.
            if (y > 0)
            {
                _tops.EraseCell(new Vector2I(x, y - 1));
                _topsFront.EraseCell(new Vector2I(x, y - 1));
            }
        }

        if (_drawn.Back[cell] == Back.Wall) DrawBack(at, _drawn.BackMaterial[cell]);

        int conduit = _drawn.Conduit[cell];
        if (conduit != Conduit.None)
        {
            // A pipe reaches toward every neighbouring conduit, so runs read as lines.
            int linked = SidesOf(cell, _drawn.Conduit, c => c != Conduit.None);
            Put(_pipes, at, PipeNames[linked]);
            if (conduit is Conduit.ValveOpen or Conduit.ValveClosed)
            {
                Put(_valves, at, conduit == Conduit.ValveOpen ? "valve_open" : "valve_closed");
            }
        }

        switch (_drawn.Structure[cell])
        {
            case Structure.Wall:
                {
                    int shape = _drawn.Shape[cell];
                    var (wall, edges) = WallNamesOf(_drawn.Material[cell], shape);
                    _walls.SetCell(at, ShipTiles.Source, _tiles.CutAt(wall, x, y));
                    // Edged where it faces no wall; a shape only on its whole sides, its diagonal edged in its picture.
                    int open = ~WalledSides(cell) & Shape.ClosedSides(shape);
                    if (open != 0)
                        Put(_edges, at, edges[open]);
                    DrawDetail(cell);
                    DrawTop(cell);
                    break;
                }
            case Structure.Floor:
                Put(_fixtures, at, "floor");
                break;
            case Structure.Ladder:
                Put(_fixtures, at, "ladder");
                break;
            case Structure.DoorClosed or Structure.DoorOpen:
                DrawDoor(cell);
                break;
        }
    }

    private static readonly int SoilCode = Materials.Get("soil").Code, LavaCode = Materials.Get(Stillwake.Content.Lava.Material).Code;

    /// <summary>
    /// The living edge on the wall at <paramref name="cell"/> where it shows (<see cref="Grass.Edged"/>), laid from the cell above, up
    /// into it. Lava has none: its surface, its haze and its embers are the lava view's.
    /// </summary>
    private void DrawTop(int cell)
    {
        int above = cell - _grid.Width, material = _drawn.Material[cell], shape = _drawn.Shape[cell];
        if (above < 0 || material == LavaCode || !Grass.Edged(_drawn.Structure[cell], material, IsNatural(material), shape, _drawn.Structure[above], _drawn.Back[above], SoilCode))
            return;
        var (back, front) = TopNamesOf(material, shape, Grass.VariantOf(cell, GroundArt.TopVariants(shape)));
        Put(_tops, CellOf(above), back);
        if (front != null) Put(_topsFront, CellOf(above), front);
    }

    /// <summary>The detail <paramref name="cell"/> shows over its ground (<see cref="GroundDetails"/>), if any.</summary>
    private void DrawDetail(int cell)
    {
        int x = cell % _grid.Width, y = cell / _grid.Width;
        var detail = GroundDetails.At(x, y, _groundAt);
        if (detail.Picture >= 0) _details.SetCell(new Vector2I(x, y), ShipTiles.Source, _tiles.CutAt(DetailNameOf(_drawn.Material[cell], detail.Picture), detail.Dx, detail.Dy));
    }

    /// <summary>Draws the details of the drawn cells of <paramref name="cell"/>'s block anew, which its own change may have changed.</summary>
    private void DrawDetails(int cell)
    {
        int w = _grid.Width, x0 = cell % w / GroundDetails.Block * GroundDetails.Block, y0 = cell / w / GroundDetails.Block * GroundDetails.Block;
        for (int y = y0; y < Math.Min(y0 + GroundDetails.Block, _grid.Height); y++)
            for (int x = x0; x < Math.Min(x0 + GroundDetails.Block, w); x++)
            {
                int c = y * w + x;
                if (!_reveal.Drawn(c)) continue;
                _details.EraseCell(new Vector2I(x, y));
                DrawDetail(c);
            }
    }

    private readonly Func<int, int, int> _groundAt;
    /// <summary>Scratch for <see cref="Sync"/>: the blocks of details drawn anew, each once.</summary>
    private readonly HashSet<int> _blocks = [];

    private int BlockOf(int cell) => cell / _grid.Width / GroundDetails.Block * (_grid.Width / GroundDetails.Block + 1) + cell % _grid.Width / GroundDetails.Block;

    /// <summary>The material of the whole wall of natural ground at (<paramref name="x"/>, <paramref name="y"/>) as drawn; -1 for any other cell or past the grid.</summary>
    private int GroundAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _grid.Width || y >= _grid.Height) return -1;
        int cell = y * _grid.Width + x, material = _drawn.Material[cell];
        return _drawn.Structure[cell] == Structure.Wall && _drawn.Shape[cell] == Shape.None && IsNatural(material) ? material : -1;
    }

    /// <summary>
    /// A door cell, shut, open or part way while its panel slides: a doorway is one door or hatch, by its shape (0067),
    /// its pictures joined where its cells meet, its panel sliding as one across all of them.
    /// </summary>
    private void DrawDoor(int cell)
    {
        int links = DoorLinksOf(cell);
        if (_slides.OpenOf(cell) is not double open)
        {
            Put(_doors, CellOf(cell), DoorShape.Picture(_drawn.Structure[cell] == Structure.DoorClosed, links));
            return;
        }
        // Where the cell lies in its straight run, counted from the top or left end the panel slides into.
        bool hatch = Doorways.IsHatch(links);
        var (back, ahead, step) = hatch ? (Doorways.Left, Doorways.Right, 1) : (Doorways.Up, Doorways.Down, _grid.Width);
        int index = 0, length = 1;
        for (int c = cell; (DoorLinksOf(c) & back) != 0; c -= step) index++;
        for (int c = cell; (DoorLinksOf(c) & ahead) != 0; c += step) length++;
        length += index;
        // Eased (0075): slow off, quick through the middle, slow into its slot.
        Put(_doors, CellOf(cell), DoorShape.Picture(hatch, index, length, (int)Math.Round(DoorLight.Ease(open) * length * Cell)));
    }

    /// <summary>Lays the whole picture <paramref name="name"/> on cell <paramref name="at"/> of <paramref name="layer"/>.</summary>
    private void Put(TileMapLayer layer, Vector2I at, string name) => layer.SetCell(at, ShipTiles.Source, _tiles.At(name));

    /// <summary>Scrap or ground cut from its picture; iron a panel, a grille in about one cell of seven.</summary>
    private void DrawBack(Vector2I at, int code)
    {
        if (BackCutOf(code) is { } cut)
        {
            _back.SetCell(at, ShipTiles.Source, _tiles.CutAt(cut, at.X, at.Y));
            return;
        }
        bool grille = HashOf((at.Y << 16) | at.X) % 7 == 0;
        Put(_back, at, grille ? "back_iron_grille" : "back_iron_plain");
    }

    // Names made once, not a string a cell: a planet's ground is back wall and moss over nearly every column.
    private readonly Dictionary<int, string?> _backCuts = new();
    private readonly Dictionary<(int, int, int), (string Back, string? Front)> _topNames = new();
    private readonly Dictionary<(int, int), string> _detailNames = new();
    private readonly Dictionary<int, bool> _natural = new();

    /// <summary>The picture a back wall of material <paramref name="code"/> is cut from over the world; null for iron's panels.</summary>
    private string? BackCutOf(int code)
    {
        if (_backCuts.TryGetValue(code, out var cut)) return cut;
        string material = MaterialOf(code);
        return _backCuts[code] = material == "scrap" ? "back_scrap" : Materials.Get(material).Natural ? ShipTiles.BackName(material) : null;
    }

    private (string Back, string? Front) TopNamesOf(int code, int shape, int variant)
    {
        if (_topNames.TryGetValue((code, shape, variant), out var names)) return names;
        string material = MaterialOf(code), front = ShipTiles.TopName(material, shape, variant, front: true);
        return _topNames[(code, shape, variant)] = (ShipTiles.TopName(material, shape, variant, front: false), _tiles.Has(front) ? front : null);
    }

    private string DetailNameOf(int code, int picture)
    {
        if (!_detailNames.TryGetValue((code, picture), out var name)) _detailNames[(code, picture)] = name = ShipTiles.DetailName(MaterialOf(code), picture);
        return name;
    }

    private bool IsNatural(int code)
    {
        if (!_natural.TryGetValue(code, out bool natural)) _natural[code] = natural = Materials.Get(MaterialOf(code)).Natural;
        return natural;
    }

    /// <summary>A number for <paramref name="cell"/> that looks random, the same every time (hashOf in ship-art.ts).</summary>
    private static uint HashOf(int cell)
    {
        uint h = unchecked((uint)cell ^ 0x9e3779b9u) * 0x85ebca6bu;
        h ^= h >> 13;
        h *= 0xc2b2ae35u;
        return h ^ (h >> 16);
    }

    private void DrawMachines()
    {
        foreach (var m in _drawnMachines) _machines.EraseCell(CellOf(m.Cell));
        foreach (var socket in _sockets.GetChildren()) socket.QueueFree();
        _drawnMachines = _grid.Machines.ToArray();
        foreach (var m in _drawnMachines)
        {
            Put(_machines, CellOf(m.Cell), $"machine_{m.Kind}");
            // A socket where power plugs in, on every machine that makes or draws it.
            if (Machines.TryGet(m.Kind, out var spec) && spec.Plug is { } plug)
            {
                var socket = new Sprite2D
                {
                    Texture = PixelArt.Lit("power-socket"),
                    Position = (Vector2)CellOf(m.Cell) * Cell + new Vector2(plug.X, plug.Y),
                };
                Lit.Set(socket);
                _sockets.AddChild(socket);
            }
        }
    }

    private Vector2I CellOf(int cell) => new(cell % _grid.Width, cell / _grid.Width);
}
