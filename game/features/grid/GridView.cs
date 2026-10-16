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
/// plugs in. <see cref="Sync"/> compares the grid's layers with what was drawn and redraws only the
/// cells that changed and their neighbours, whose edges and links reach toward them.
/// </summary>
public partial class GridView : Node2D
{
    private const int Cell = ShipCamera.Cell;

    /// <summary>Which sides of a cell: up, right, down, left, a bit each (SIDE in ship-art.ts).</summary>
    private const int Up = 1, Right = 2, Down = 4, Left = 8;

    private readonly IGridQueries _grid;
    private readonly ShipTiles _tiles = new();
    private readonly TileMapLayer _back, _pipes, _valves, _walls, _edges, _fixtures, _doors, _machines;
    private readonly Node2D _sockets = new() { ZIndex = Layers.Sockets };
    // A door or valve that moves is heard where it stands, by everyone near (0033).
    private readonly Heard2D _opens = Cues.Player2D(S.DoorOpen, S.World, voices: 2),
        _closes = Cues.Player2D(S.DoorClose, S.World, voices: 2), _turns = Cues.Player2D(S.Valve, S.World, voices: 2);
    private readonly Dictionary<int, string> _materials = Materials.All.ToDictionary(m => m.Code, m => m.Id);
    private int[] _structure = [], _material = [], _conduit = [], _backs = [], _backMaterial = [];
    private Machine[] _drawnMachines = [];
    private bool _drawn;

    public GridView(IGridQueries grid)
    {
        _grid = grid;
        // Walls and doors take the share of light a wall keeps of what reaches its face; the rest all of it.
        _back = Layer(Layers.BackWalls);
        _pipes = Layer(Layers.Pipes);
        _valves = Layer(Layers.Valves);
        _walls = Layer(Layers.Walls, L.WallIn);
        _edges = Layer(Layers.Edges, L.WallIn);
        _fixtures = Layer(Layers.Fixtures);
        _doors = Layer(Layers.Doors, L.WallIn);
        _machines = Layer(Layers.Machines);
        AddChild(_sockets);
        foreach (var player in (Node[])[_opens, _closes, _turns]) AddChild(player);
    }

    private TileMapLayer Layer(int z, float share = 1)
    {
        var layer = new TileMapLayer { TileSet = _tiles.Set, ZIndex = z };
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
        var dirty = ChangedCells(shown);
        foreach (int cell in dirty) DrawCell(cell);
        if (!_drawnMachines.SequenceEqual(_grid.Machines)) DrawMachines();
    }

    /// <summary>The cells whose drawing is stale, bringing what was drawn up to date: each changed cell and its neighbours.</summary>
    private List<int> ChangedCells(ShownGrid shown)
    {
        int w = _grid.Width, size = w * _grid.Height;
        var dirty = new List<int>();
        if (!_drawn)
        {
            _drawn = true;
            _structure = (shown.Structure ?? _grid.StructureLayer).ToArray();
            _material = _grid.MaterialLayer.ToArray();
            _conduit = (shown.Conduit ?? _grid.ConduitLayer).ToArray();
            _backs = _grid.BackLayer.ToArray();
            _backMaterial = _grid.BackMaterialLayer.ToArray();
            for (int c = 0; c < size; c++) dirty.Add(c);
            return dirty;
        }
        ReadOnlySpan<int> s = shown.Structure ?? _grid.StructureLayer, m = _grid.MaterialLayer, cd = shown.Conduit ?? _grid.ConduitLayer,
            b = _grid.BackLayer, bm = _grid.BackMaterialLayer;
        if (s.SequenceEqual(_structure) && m.SequenceEqual(_material) && cd.SequenceEqual(_conduit)
            && b.SequenceEqual(_backs) && bm.SequenceEqual(_backMaterial))
            return dirty;
        var marked = new bool[size];
        void Mark(int c)
        {
            if (c < 0 || c >= size || marked[c]) return;
            marked[c] = true;
            dirty.Add(c);
        }
        for (int c = 0; c < size; c++)
        {
            if (s[c] == _structure[c] && m[c] == _material[c] && cd[c] == _conduit[c] && b[c] == _backs[c] && bm[c] == _backMaterial[c])
                continue;
            Hear(c, w, s[c], cd[c]);
            (_structure[c], _material[c], _conduit[c], _backs[c], _backMaterial[c]) = (s[c], m[c], cd[c], b[c], bm[c]);
            int x = c % w;
            Mark(c);
            Mark(c - w);
            Mark(c + w);
            if (x > 0) Mark(c - 1);
            if (x < w - 1) Mark(c + 1);
        }
        return dirty;
    }

    /// <summary>
    /// A door or a valve at <paramref name="cell"/> that moved, heard there: once a door, at its top cell,
    /// since the cells under it moved with it (the one above was brought up to date already).
    /// </summary>
    private void Hear(int cell, int w, int structure, int conduit)
    {
        static bool Door(int s) => s is Structure.DoorClosed or Structure.DoorOpen;
        var at = new Vector2((cell % w + 0.5f) * Cell, (cell / w + 0.5f) * Cell);
        if (Door(structure) && Door(_structure[cell]) && structure != _structure[cell] && !(cell >= w && Door(_structure[cell - w])))
            (structure == Structure.DoorOpen ? _opens : _closes).PlayAt(at);
        if (conduit is Conduit.ValveClosed or Conduit.ValveOpen && _conduit[cell] is Conduit.ValveClosed or Conduit.ValveOpen && conduit != _conduit[cell])
            _turns.PlayAt(at);
    }

    private string MaterialOf(int code) => _materials.GetValueOrDefault(code, Materials.DefaultHull);

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

    private void DrawCell(int cell)
    {
        int x = cell % _grid.Width, y = cell / _grid.Width;
        var at = new Vector2I(x, y);
        foreach (var layer in (TileMapLayer[])[_back, _pipes, _valves, _walls, _edges, _fixtures, _doors]) layer.EraseCell(at);

        if (_backs[cell] == Back.Wall) DrawBack(at, _backMaterial[cell]);

        int conduit = _conduit[cell];
        if (conduit != Conduit.None)
        {
            // A pipe reaches toward every neighbouring conduit, so runs read as lines.
            int linked = SidesOf(cell, _conduit, c => c != Conduit.None);
            Put(_pipes, at, $"pipe_{linked}");
            if (conduit is Conduit.ValveOpen or Conduit.ValveClosed)
            {
                Put(_valves, at, conduit == Conduit.ValveOpen ? "valve_open" : "valve_closed");
            }
        }

        switch (_structure[cell])
        {
            case Structure.Wall:
                {
                    string material = MaterialOf(_material[cell]);
                    _walls.SetCell(at, ShipTiles.Source, _tiles.CutAt($"wall_{material}", x, y));
                    int open = ~SidesOf(cell, _structure, s => s == Structure.Wall) & 0b1111;
                    if (open != 0)
                        Put(_edges, at, $"wall-edge_{material}_{open}");
                    break;
                }
            case Structure.Floor:
                Put(_fixtures, at, "floor");
                break;
            case Structure.Ladder:
                Put(_fixtures, at, "ladder");
                break;
            case Structure.DoorClosed or Structure.DoorOpen:
                {
                    // A door several cells tall is one door: its frame and window at the top, its stripes at the bottom.
                    int linked = SidesOf(cell, _structure, s => s is Structure.DoorClosed or Structure.DoorOpen) & (Up | Down);
                    string look = _structure[cell] == Structure.DoorClosed ? "closed" : "open";
                    Put(_doors, at, $"door_{look}_{linked}");
                    break;
                }
        }
    }

    /// <summary>Lays the whole picture <paramref name="name"/> on cell <paramref name="at"/> of <paramref name="layer"/>.</summary>
    private void Put(TileMapLayer layer, Vector2I at, string name) => layer.SetCell(at, ShipTiles.Source, _tiles.At(name));

    /// <summary>Scrap cut from its picture; iron a panel, a grille in about one cell of seven.</summary>
    private void DrawBack(Vector2I at, int code)
    {
        if (MaterialOf(code) == "scrap")
        {
            _back.SetCell(at, ShipTiles.Source, _tiles.CutAt("back_scrap", at.X, at.Y));
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
