using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Building;

/// <summary>What becomes of what a burst breaks (unified-damage).</summary>
public enum Spoils
{
    /// <summary>Falls at its cell for anyone, crushed where its material says (the blasting charge); back walls stand.</summary>
    Drop,
}

/// <summary>
/// Bursts (unified-damage): the one rule for what a charge breaks, as a tool's or a shot's hit, in every direction through the
/// shared <see cref="Wave"/>. A step costs <see cref="Damage.Step"/>; natural ground what is left of its strength; a built wall or
/// piece that over <see cref="Damage.BuiltTakes"/>, so a burst cracks what is built and a second breaks it.
/// </summary>
public sealed partial class BuildingModule
{
    private readonly Wave _wave = new();
    private readonly List<int> _burstBroken = [], _burstPaid = [];
    private readonly List<(int Key, int Left)> _burstCracked = [];

    /// <inheritdoc/>
    public bool Burst(int peer, int cell, int power, int reach, Spoils spoils)
    {
        if (cell < 0 || cell >= _grid.Width * _grid.Height || BurstEnter(cell) is not int first) return false;
        _burstBroken.Clear();
        _burstPaid.Clear();
        _burstCracked.Clear();
        _wave.Spread(_grid.Width, _grid.Height, cell, first, power, reach, BurstEnter, (at, _) =>
        {
            if (_grid.WallAt(at) != null) _burstBroken.Add(at);
            else if (HeadAt(at) is int head && !_burstPaid.Contains(head))
            {
                _burstPaid.Add(head);
                _burstBroken.Add(head);
            }
        }, (at, left) =>
        {
            if (_grid.WallAt(at) is { } wall) _burstCracked.Add((at, wall.Natural ? left : left * Damage.BuiltTakes / 1000));
            else if (_grid.MachineAt(at) is { } machine) _burstCracked.Add((machine.Cell, Machines.Get(machine.Kind).Hp != null ? left : left * Damage.BuiltTakes / 1000));
            else if (PieceAt(at) is { } piece) _burstCracked.Add((piece.Head, left * Damage.BuiltTakes / 1000));
        });
        _burstBroken.Sort();
        bool shocked = false;
        foreach (int at in _burstBroken)
            foreach (var plant in Break(at))
            {
                // A relic taken from its cradle discharges into whoever threw the burst, wherever they stand, once (0096).
                if (_plantOwn.GetValueOrDefault(plant.Cell) != plant) continue;
                shocked |= plant.Kind == Ruins.Cradle;
                Fell(plant, null);
            }
        if (shocked) _crewOps.Shock(peer, Ruins.CradleShock, Harm.Shock);
        // A piece two cells of reached cracks once, at its head, by the most either was left.
        _burstCracked.Sort((a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : b.Left.CompareTo(a.Left));
        for (int i = 0; i < _burstCracked.Count; i++)
        {
            var (key, left) = _burstCracked[i];
            if (left <= 0 || i > 0 && _burstCracked[i - 1].Key == key) continue;
            if (_grid.WallAt(key) is { } wall) Cracked(key, wall.Strength, left);
            else if (_grid.MachineAt(key) is { } machine && machine.Cell == key) Cracked(key, Machines.Get(machine.Kind).HitPoints!.Value, left);
            else if (PieceAt(key) is { } piece && piece.Head == key) Cracked(key, piece.Build.Strength, left);
        }
        return true;
    }

    /// <summary>
    /// What a burst pays to enter <paramref name="cell"/> besides its step: nothing open; natural ground what is left of its strength,
    /// a pile its hit points; a built wall, piece or machine that over <see cref="Damage.BuiltTakes"/>, rounded up, a piece or machine
    /// once, nothing more once one cell of it is entered; null where it stops: bedrock, lava, the fog, a <c>Fixed</c> machine.
    /// </summary>
    private int? BurstEnter(int cell)
    {
        if (!_grid.Zone.Contains(cell % _grid.Width, cell / _grid.Width)) return null;
        if (_grid.WallAt(cell) is { } wall)
            return wall.Strength == Material.Unbreakable ? null : wall.Natural ? Remaining(cell, wall.Strength) : Built(Remaining(cell, wall.Strength));
        if (_grid.MachineAt(cell) is { } machine)
        {
            var spec = Machines.Get(machine.Kind);
            if (spec.HitPoints is not int hp) return null;
            if (_burstPaid.Contains(machine.Cell)) return 0;
            return spec.Hp != null ? Remaining(machine.Cell, hp) : Built(Remaining(machine.Cell, hp));
        }
        if (PieceAt(cell) is { } piece) return _burstPaid.Contains(piece.Head) ? 0 : Built(Remaining(piece.Head, piece.Build.Strength));
        return _grid.StructureAt(cell) != Structure.Empty ? null : 0;
    }

    /// <summary>What a burst paid for at <paramref name="at"/> broken; nothing for a net a wall broken before took with it.</summary>
    private List<Plant> Break(int at) =>
        _grid.WallAt(at) != null ? BreakWall(at)
        : _grid.MachineAt(at) is { } machine ? machine.Cell == at ? BreakMachine(at) : []
        : PieceAt(at) is { } piece && piece.Head == at ? BreakPiece(at) : [];

    /// <summary>The cell a burst keeps a machine or built piece at <paramref name="cell"/> by: its top-left, its head; none for anything else.</summary>
    private int? HeadAt(int cell) => _grid.MachineAt(cell)?.Cell ?? PieceAt(cell)?.Head;

    /// <summary>What is left of <paramref name="strength"/> at <paramref name="key"/> after its crack.</summary>
    private int Remaining(int key, int strength) => strength - (CrackIndex(key) is >= 0 and var at ? State.Cracks[at].Damage : 0);

    /// <summary>What a burst pays to break a built thing <paramref name="left"/> short of breaking, taking a share of it: rounded up.</summary>
    private static int Built(int left) => (left * 1000 + Damage.BuiltTakes - 1) / Damage.BuiltTakes;

    /// <summary>
    /// The wall at <paramref name="cell"/> broken by a burst: its crack dropped, what it is made of fallen there for anyone, crushed
    /// where its material says; a debris net hung on it goes with it (2026-11-03-debris-net). The growths it held, to be felled.
    /// </summary>
    private List<Plant> BreakWall(int cell)
    {
        var wall = _grid.WallAt(cell)!;
        if (CrackIndex(cell) is >= 0 and var at) State.Cracks.RemoveAt(at);
        var held = HeldBy(cell);
        _gridOps.Clear(cell, "structure");
        _itemsOps.DropAt(FloorStack.Nobody, wall.Crushed is { } crushed ? [(crushed, 1)] : wall.Cost, cell);
        int next = cell + 1;
        if (next % _grid.Width != 0 && _grid.MachineAt(next) is { } net && Machines.Is(net.Kind, MachineTrait.Catches))
        {
            _gridOps.Clear(next, "machine");
            foreach (var (item, count) in Machines.Get(net.Kind).Cost) _itemsOps.LayDown(new Stack(item, count), net.Cell);
        }
        return held;
    }

    /// <summary>
    /// The machine at <paramref name="head"/>, its top-left, broken by a burst: what it holds spilled, its queue's refund and its cost
    /// fallen there for anyone; its cables left on a peg (0110). Gas or water it held goes with it, as when taken apart. No growths.
    /// </summary>
    private List<Plant> BreakMachine(int head)
    {
        var machine = _grid.MachineAt(head)!;
        if (CrackIndex(head) is >= 0 and var at) State.Cracks.RemoveAt(at);
        _itemsOps.Spill(machine.Id, head);
        _itemsOps.DropAt(FloorStack.Nobody, _craftingOps.Scrap(machine.Id), head);
        var cost = Costs.BuiltAt(_grid, head, "machine")!.Cost;
        _powerOps.Unplug(machine.Id);
        _gridOps.Clear(head, "machine");
        _itemsOps.DropAt(FloorStack.Nobody, cost, head);
        return [];
    }

    /// <summary>The built piece kept by <paramref name="head"/> broken by a burst, whole: what it cost fallen there for anyone. The growths its cells held, to be felled.</summary>
    private List<Plant> BreakPiece(int head)
    {
        if (CrackIndex(head) is >= 0 and var at) State.Cracks.RemoveAt(at);
        var cost = Costs.BuiltAt(_grid, head, "structure")!.Cost;
        var held = new List<Plant>();
        foreach (int cell in _grid.PieceOf(head))
            foreach (var plant in HeldBy(cell))
                if (!held.Contains(plant)) held.Add(plant);
        foreach (int cell in _grid.PieceOf(head)) _gridOps.Clear(cell, "structure");
        _itemsOps.DropAt(FloorStack.Nobody, cost, head);
        held.Sort(ByCell);
        return held;
    }
}
