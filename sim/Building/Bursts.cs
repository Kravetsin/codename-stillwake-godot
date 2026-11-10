using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Building;

/// <summary>What becomes of what a burst breaks (unified-damage).</summary>
public enum Spoils
{
    /// <summary>Falls at its cell for anyone, crushed where its material says (the blasting charge); back walls stand.</summary>
    Drop,
    /// <summary>
    /// Lost (a meteor): nothing of a wall, piece or machine's cost is left, though what a machine held still spills; and the back
    /// wall behind each cell entered breaks where what is left there pays for it.
    /// </summary>
    Lost,
}

/// <summary>
/// Bursts (unified-damage, damage-scale): the one rule for what a charge or a meteor breaks, as a tool's or a shot's hit, in every
/// direction through the shared <see cref="Wave"/>. A step costs <see cref="Damage.Step"/>, a cell what is left of its strength; a
/// player's own burst aboard their ship that over <see cref="Damage.OwnShipTakes"/>, so a charge thrown aboard only cracks it.
/// </summary>
public sealed partial class BuildingModule
{
    private readonly Wave _wave = new();
    private readonly List<int> _burstBroken = [], _burstPaid = [], _burstBacks = [];
    private readonly List<(int Key, int Left)> _burstCracked = [];
    private bool _burstOwn;
    private int _burstPeer = FloorStack.Nobody;

    /// <inheritdoc/>
    public bool Burst(int peer, int cell, int power, int reach, Spoils spoils)
    {
        if (cell < 0 || cell >= _grid.Width * _grid.Height) return false;
        // Cleared before the first cell is priced: what the last burst paid for must not make this one's first cell free.
        _burstBroken.Clear();
        _burstPaid.Clear();
        _burstBacks.Clear();
        _burstCracked.Clear();
        _burstOwn = peer != FloorStack.Nobody;
        _burstPeer = peer;
        if (BurstEnter(cell) is not int first) return false;
        _wave.Spread(_grid.Width, _grid.Height, cell, first, power, reach, BurstEnter, (at, cost) =>
        {
            // A wall by its cell; a machine or piece once, by its key, whichever of its cells is paid for first.
            switch (TargetAt(at))
            {
                case { Kind: TargetKind.Wall }:
                    _burstBroken.Add(at);
                    break;
                case { Kind: TargetKind.Machine or TargetKind.Piece } t when !_burstPaid.Contains(t.Key):
                    _burstPaid.Add(t.Key);
                    _burstBroken.Add(t.Key);
                    break;
            }
            if (spoils == Spoils.Lost && BackCost(at) is int back && power - cost >= back) _burstBacks.Add(at);
        }, (at, left) =>
        {
            int crack = Softened(at) ? left * Damage.OwnShipTakes / 1000 : left;
            if (TargetAt(at) is { Kind: not TargetKind.Plant } t) _burstCracked.Add((t.Key, crack));
        });
        _burstBroken.Sort();
        bool shocked = false;
        foreach (int at in _burstBroken)
            foreach (var plant in Break(at, spoils))
            {
                // A relic taken from its cradle discharges into whoever threw the burst, wherever they stand, once (0096).
                if (_plantOwn.GetValueOrDefault(plant.Cell) != plant) continue;
                shocked |= plant.Kind == Ruins.Cradle;
                Fell(plant, null);
                if (peer != FloorStack.Nobody && Kinds[plant.Kind].Yields is { Count: > 0 } yields) _crewOps.Loot(peer, yields);
            }
        if (shocked && peer != FloorStack.Nobody) _crewOps.Shock(peer, Ruins.CradleShock, Harm.Shock);
        _burstBacks.Sort();
        foreach (int at in _burstBacks)
        {
            if (CrackIndex(at, back: true) is >= 0 and var crack) State.Cracks.RemoveAt(crack);
            _gridOps.Clear(at, "back");
        }
        // A piece two cells of reached cracks once, at its head, by the most either was left.
        _burstCracked.Sort((a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : b.Left.CompareTo(a.Left));
        for (int i = 0; i < _burstCracked.Count; i++)
        {
            var (key, left) = _burstCracked[i];
            if (left <= 0 || i > 0 && _burstCracked[i - 1].Key == key) continue;
            if (TargetAt(key) is { Strength: int strength } t && t.Key == key) Cracked(key, strength, left);
        }
        return true;
    }

    /// <summary>
    /// What a burst pays to enter <paramref name="cell"/> besides its step (<see cref="TargetAt"/>): nothing open or for a plant; a wall,
    /// piece or machine what is left of its strength (a pile's read as tenths), over <see cref="Damage.OwnShipTakes"/> where
    /// <see cref="Softened"/>, a piece or machine once, nothing more once one cell of it is entered; null where it stops: the fog,
    /// what nothing breaks (bedrock, lava, the lifepod), a structure that is no target (a grate).
    /// </summary>
    private int? BurstEnter(int cell)
    {
        if (!_grid.Zone.Contains(cell % _grid.Width, cell / _grid.Width)) return null;
        if (TargetAt(cell) is not { } target) return _grid.StructureAt(cell) != Structure.Empty ? null : 0;
        if (target.Kind == TargetKind.Plant) return 0;
        if (target.Strength is not int strength) return null;
        if (target.Kind != TargetKind.Wall && _burstPaid.Contains(target.Key)) return 0;
        return Share(cell, Remaining(target.Key, strength));
    }

    /// <summary>What breaking <paramref name="wall"/> at <paramref name="cell"/> costs a burst in full: what is left of it; null for one that never breaks.</summary>
    private int? Cost(int cell, Material wall, bool back) =>
        wall.Strength == Material.Unbreakable ? null : Remaining(cell, wall.Strength, back);

    /// <summary>What breaking the back wall at <paramref name="cell"/> costs a burst, as a wall; null for none, or one that never breaks.</summary>
    private int? BackCost(int cell) => _grid.BackAt(cell) is { } back ? Cost(cell, back, back: true) : null;

    /// <summary>What a burst paid for at <paramref name="at"/> broken; nothing for a net a wall broken before took with it.</summary>
    private List<Plant> Break(int at, Spoils spoils) => TargetAt(at) switch
    {
        { Kind: TargetKind.Wall } => BreakWall(at, spoils),
        { Kind: TargetKind.Machine } t when t.Key == at => BreakMachine(at, spoils),
        { Kind: TargetKind.Piece } t when t.Key == at => BreakPiece(at, spoils),
        _ => [],
    };

    /// <summary>What is left of <paramref name="strength"/> at <paramref name="key"/> after its crack, in the <paramref name="back"/> layer or the front.</summary>
    private int Remaining(int key, int strength, bool back = false) =>
        strength - (CrackIndex(key, back) is >= 0 and var at ? State.Cracks[at].Damage : 0);

    /// <summary>Whether this burst takes only its share at <paramref name="cell"/>: a player's own, on their ship (damage-scale).</summary>
    private bool Softened(int cell) => _burstOwn && Aboard(cell);

    /// <summary>
    /// What a burst pays at <paramref name="cell"/> for what is <paramref name="left"/> of its strength: that, or over its share where
    /// <see cref="Softened"/>, rounded up, so the crack an unpaid cell takes always stays below what is left of it.
    /// </summary>
    private int Share(int cell, int left) =>
        Softened(cell) ? (left * 1000 + Damage.OwnShipTakes - 1) / Damage.OwnShipTakes : left;

    /// <summary>
    /// <paramref name="cost"/> fallen at <paramref name="cell"/> for anyone, as <paramref name="spoils"/> says: none when lost. Its worth
    /// fills the thrower's wanted meter (catcher-loot).
    /// </summary>
    private void Leave(Spoils spoils, IReadOnlyList<(string Item, int Count)> cost, int cell)
    {
        if (spoils != Spoils.Drop) return;
        _itemsOps.DropAt(FloorStack.Nobody, cost, cell);
        if (_burstPeer != FloorStack.Nobody) _crewOps.Loot(_burstPeer, cost);
    }

    /// <summary>
    /// The wall at <paramref name="cell"/> broken by a burst: its crack dropped, what it is made of fallen there or lost, crushed
    /// where its material says; a debris net hung on it goes with it, its cost floating there for anyone (2026-11-03-debris-net). The
    /// growths it held, to be felled.
    /// </summary>
    private List<Plant> BreakWall(int cell, Spoils spoils)
    {
        var wall = _grid.WallAt(cell)!;
        if (CrackIndex(cell) is >= 0 and var at) State.Cracks.RemoveAt(at);
        var held = HeldBy(cell);
        _gridOps.Clear(cell, "structure");
        Leave(spoils, wall.Crushed is { } crushed ? [(crushed, 1)] : wall.Cost, cell);
        int next = cell + 1;
        if (next % _grid.Width != 0 && _grid.MachineAt(next) is { } net && Machines.Is(net.Kind, MachineTrait.Catches)) FreeNet(net);
        return held;
    }

    /// <summary>
    /// The machine at <paramref name="head"/>, its top-left, broken by a burst: what it holds spilled and its queue's refund fallen there
    /// for anyone, its cost too or lost; its cables left on a peg (0110). Gas or water it held goes with it, as when taken apart.
    /// </summary>
    private List<Plant> BreakMachine(int head, Spoils spoils)
    {
        var machine = _grid.MachineAt(head)!;
        if (Machines.Is(machine.Kind, MachineTrait.Catches))
        {
            FreeNet(machine);
            return [];
        }
        if (CrackIndex(head) is >= 0 and var at) State.Cracks.RemoveAt(at);
        _itemsOps.Spill(machine.Id, head);
        _itemsOps.DropAt(FloorStack.Nobody, _craftingOps.Scrap(machine.Id), head);
        var cost = Costs.BuiltAt(_grid, head, "machine")!.Cost;
        _powerOps.Unplug(machine.Id);
        _gridOps.Clear(head, "machine");
        Leave(spoils, cost, head);
        return [];
    }

    /// <summary>
    /// A debris net gone to a burst, with its wall or by itself: its cost floating at its cell for anyone, lost to no burst
    /// (2026-11-03-debris-net); what it held floats out by Debris' rule.
    /// </summary>
    private void FreeNet(Machine net)
    {
        if (CrackIndex(net.Cell) is >= 0 and var at) State.Cracks.RemoveAt(at);
        _gridOps.Clear(net.Cell, "machine");
        foreach (var (item, count) in Machines.Get(net.Kind).Cost) _itemsOps.LayDown(new Stack(item, count), net.Cell);
    }

    /// <summary>The built piece kept by <paramref name="head"/> broken by a burst, whole: what it cost fallen there or lost. The growths its cells held, to be felled.</summary>
    private List<Plant> BreakPiece(int head, Spoils spoils)
    {
        if (CrackIndex(head) is >= 0 and var at) State.Cracks.RemoveAt(at);
        var cost = Costs.BuiltAt(_grid, head, "structure")!.Cost;
        var held = new List<Plant>();
        foreach (int cell in _grid.PieceOf(head))
            foreach (var plant in HeldBy(cell))
                if (!held.Contains(plant)) held.Add(plant);
        foreach (int cell in _grid.PieceOf(head)) _gridOps.Clear(cell, "structure");
        Leave(spoils, cost, head);
        held.Sort(ByCell);
        return held;
    }
}
