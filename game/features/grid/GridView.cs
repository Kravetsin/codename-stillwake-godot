using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
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
    private readonly TileMapLayer _back, _pipes, _valves, _walls, _edges, _grass, _fixtures, _doors, _machines;
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
        _chunksAcross = (grid.Width + Chunk - 1) / Chunk;
        _chunks = new bool[_chunksAcross * ((grid.Height + Chunk - 1) / Chunk)];
        // Walls and doors take the share of light a wall keeps of what reaches its face; the rest all of it.
        _back = Layer(Layers.BackWalls);
        _pipes = Layer(Layers.Pipes);
        _valves = Layer(Layers.Valves);
        _walls = Layer(Layers.Walls, L.WallIn);
        _edges = Layer(Layers.Edges, L.WallIn);
        // Moss over its soil, lit as the open air its top faces; over the edges, as added after them.
        _grass = Layer(Layers.Edges);
        _fixtures = Layer(Layers.Fixtures);
        _doors = Layer(Layers.Doors, L.WallIn);
        _machines = Layer(Layers.Machines);
        _cellLayers = [_back, _pipes, _valves, _walls, _edges, _fixtures, _doors];
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
                if (_chunks[ChunkOf(cell)]) DrawCell(cell, blank: false);
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
        Reveal();
        if (!_slides.Any) return;
        foreach (int cell in _slides.Advance(delta))
        {
            if (!IsDoor(_drawn.Structure[cell])) continue;
            DrawDoor(cell);
            Light(cell);
        }
    }

    /// <summary>
    /// How many cells square the grid is drawn in, a chunk at a time, as the camera comes near it: a planet is built in nearly
    /// every cell, and drawing all of them at once froze its first look for a second (0078).
    /// </summary>
    private const int Chunk = 32;

    /// <summary>Which chunks are drawn, row by row; a cell changed in one not drawn is drawn with it, from <see cref="_drawn"/>.</summary>
    private readonly bool[] _chunks;
    private readonly int _chunksAcross;

    private int ChunkOf(int cell) => cell / _grid.Width / Chunk * _chunksAcross + cell % _grid.Width / Chunk;

    /// <summary>Draws every chunk in the view or a chunk round it not drawn yet: once each, all a look needs at once.</summary>
    private void Reveal()
    {
        var view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewport().GetVisibleRect();
        int down = _chunks.Length / _chunksAcross, span = Chunk * Cell;
        int left = Math.Max(0, (int)Math.Floor(view.Position.X / span) - 1), right = Math.Min(_chunksAcross - 1, (int)Math.Floor(view.End.X / span) + 1);
        int top = Math.Max(0, (int)Math.Floor(view.Position.Y / span) - 1), bottom = Math.Min(down - 1, (int)Math.Floor(view.End.Y / span) + 1);
        for (int cy = top; cy <= bottom; cy++)
            for (int cx = left; cx <= right; cx++)
            {
                if (_chunks[cy * _chunksAcross + cx]) continue;
                _chunks[cy * _chunksAcross + cx] = true;
                int w = _grid.Width;
                for (int y = cy * Chunk; y < Math.Min(_grid.Height, (cy + 1) * Chunk); y++)
                    for (int x = cx * Chunk; x < Math.Min(w, (cx + 1) * Chunk); x++)
                    {
                        int cell = y * w + x;
                        if (_drawn.Structure[cell] != Structure.Empty || _drawn.Back[cell] != Back.None || _drawn.Conduit[cell] != Conduit.None) DrawCell(cell, blank: true);
                    }
            }
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
            // Moss is laid from the cell above its soil, and only this cell's drawing lays it there.
            if (y > 0) _grass.EraseCell(new Vector2I(x, y - 1));
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
                    DrawGrass(cell);
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

    private static readonly int SoilCode = Materials.Get("soil").Code;

    /// <summary>Moss on the wall at <paramref name="cell"/> where it grows (<see cref="Grass"/>), laid from the cell above, its stems up into it.</summary>
    private void DrawGrass(int cell)
    {
        int above = cell - _grid.Width;
        if (above < 0 || !Grass.Grows(_drawn.Structure[cell], _drawn.Material[cell], _drawn.Shape[cell], _drawn.Structure[above], _drawn.Back[above], SoilCode))
            return;
        Put(_grass, CellOf(above), ShipTiles.GrassName(_drawn.Shape[cell], Grass.VariantOf(cell, GroundArt.GrassVariants)));
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

    /// <summary>Scrap cut from its picture; iron a panel, a grille in about one cell of seven.</summary>
    private void DrawBack(Vector2I at, int code)
    {
        string material = MaterialOf(code);
        if (material == "scrap" || Materials.Get(material).Natural)
        {
            _back.SetCell(at, ShipTiles.Source, _tiles.CutAt(material == "scrap" ? "back_scrap" : ShipTiles.BackName(material), at.X, at.Y));
            return;
        }
        bool grille = HashOf((at.Y << 16) | at.X) % 7 == 0;
        Put(_back, at, grille ? "back_iron_grille" : "back_iron_plain");
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
