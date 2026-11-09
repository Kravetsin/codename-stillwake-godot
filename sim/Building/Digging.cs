using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Building;

/// <summary>
/// A hit of weapon <c>Tool</c> on what stands on a planet's grid (the fan of rays): its fan cast from the hand at (<c>X</c>,
/// <c>Y</c>), in thousandths of a cell, aimed at <c>Heading</c> (<see cref="Headings"/>), sent as the axe strikes or at the
/// drill's pace. Each ray strikes the first wall it meets, a closed door or hatch, a floor, and stops there; it passes through
/// the plants, ladders and open doors it crosses, striking them. What stands takes the weapon's block damage
/// (<c>BlockDamage</c>) once, however many rays meet it: a wall at its material's strength, a built piece at its build's, and
/// gives the sender what it is made of. Only where the surface's tools break things (<c>ToolsBreak</c>), and aboard inside a
/// vessel's box (generated-vessels): elsewhere on the ship the plan takes things apart, and only a machine of some strength, a
/// pile, is struck, anywhere.
/// </summary>
public sealed record Hew(string Tool, int X, int Y, int Heading) : Command;

/// <summary>
/// A hit of a running tool (tool-edge) on the cells its drawn edge touches, <c>Cells</c> as the client saw them: what stands in each
/// on the grid, in the planet's known zone, as a <see cref="Hew"/>'s ray would strike it there (a pile, a wall that breaks, a built
/// piece by its first cell, a plant in an open cell), each once, takes the weapon's block damage as a <see cref="Hew"/> gives it.
/// Neither reach nor position is checked: the client's word, among friends (0002).
/// </summary>
public sealed record HewCells(string Tool, IReadOnlyList<int> Cells) : Command;

/// <summary>What a <see cref="Hew"/> strikes: its walls' cells and its built pieces' first cells, ascending, and its plants, by their own cells.</summary>
public sealed record Hewn(IReadOnlyList<int> Cells, IReadOnlyList<Plant> Plants)
{
    public bool IsEmpty => Cells.Count == 0 && Plants.Count == 0;
}

/// <summary>Breaking what stands on a planet's grid with tools (0077, the fan of rays): cracks kept by cell, broken at strength.</summary>
public sealed partial class BuildingModule
{
    /// <summary>The walls, built pieces and plants (by their own cells) hit part way, ascending by cell: live and read-only.</summary>
    public IReadOnlyList<Crack> Cracks => State.Cracks;

    /// <summary>
    /// What a hit of <paramref name="tool"/> from (<paramref name="x"/>, <paramref name="y"/>) toward <paramref name="heading"/>
    /// would strike (<see cref="Hew"/>): what the command strikes, and the client's preview, so the two never differ. Where the
    /// surface's tools break nothing, only piles.
    /// </summary>
    public Hewn Struck(string tool, int x, int y, int heading)
    {
        var weapon = Weapons.Of(tool) ?? throw new InvalidOperationException($"building.hew: {tool} deals no damage");
        var cells = new List<int>();
        var plants = new List<Plant>();
        Rays.Fan(_grid.Width, _grid.Height, x, y, heading, weapon, cell => Stops(cell, cells, plants));
        cells.Sort();
        for (int i = cells.Count - 1; i > 0; i--)
            if (cells[i] == cells[i - 1]) cells.RemoveAt(i);
        plants.Sort(ByCell);
        return new Hewn(cells, plants);
    }

    /// <summary>What a <see cref="HewCells"/> strikes in <paramref name="listed"/>: each cell on the grid, in its known zone or a vessel's box, as a ray would strike it, deduped, ascending.</summary>
    public Hewn Listed(IReadOnlyList<int> listed)
    {
        var cells = new List<int>();
        var plants = new List<Plant>();
        foreach (int cell in listed)
            if (cell >= 0 && cell < _grid.Width * _grid.Height && (_grid.Zone.Contains(cell % _grid.Width, cell / _grid.Width) || InVessel(cell)))
                Stops(cell, cells, plants);
        cells.Sort();
        for (int i = cells.Count - 1; i > 0; i--)
            if (cells[i] == cells[i - 1]) cells.RemoveAt(i);
        plants.Sort(ByCell);
        return new Hewn(cells, plants);
    }

    /// <summary>
    /// Whether a ray stops at <paramref name="cell"/>: at a wall, one that breaks added to <paramref name="cells"/>; at a built
    /// piece that stops a body, added by its first cell; through a piece that does not (a ladder, an open door) and a plant in an
    /// open cell, each added once; at a pile, added by its top-left cell. Where the surface's tools break nothing, a ray only stops.
    /// </summary>
    private bool Stops(int cell, List<int> cells, List<Plant> plants)
    {
        if (PileAt(cell) is { } pile)
        {
            cells.Add(pile.Cell);
            return true;
        }
        // Aboard, tools break only a vessel's things (generated-vessels): a ray elsewhere only stops, the ship untouched.
        if (!Rules.ToolsBreak && !InVessel(cell)) return _grid.WallAt(cell) != null || _grid.Blocks(cell);
        if (_grid.WallAt(cell) is { } wall)
        {
            if (wall.Strength != Material.Unbreakable) cells.Add(cell);
            return true;
        }
        if (PieceAt(cell) is { } piece)
        {
            cells.Add(piece.Head);
            return _grid.Blocks(cell);
        }
        if (_grid.Blocks(cell)) return true;
        if (_grid.StructureAt(cell) == Structure.Empty && PlantAt(cell) is { } plant && !plants.Contains(plant)) plants.Add(plant);
        return false;
    }

    /// <summary>The machine tools break at <paramref name="cell"/>, a pile (the-scoop); none for any other.</summary>
    private Machine? PileAt(int cell) => _grid.MachineAt(cell) is { } machine && Machines.Get(machine.Kind).Hp != null ? machine : null;

    /// <summary>Whether <paramref name="cell"/> is in the box a vessel was written into (generated-vessels).</summary>
    private bool InVessel(int cell) => _grid.Reserved is { } box && box.Contains(cell % _grid.Width, cell / _grid.Width);

    /// <summary>The built piece at <paramref name="cell"/>, a floor, ladder, door or hatch: its first cell, kept by, and its build; none for a wall or nothing.</summary>
    private (int Head, Build Build)? PieceAt(int cell)
    {
        int structure = _grid.StructureAt(cell);
        if (structure == Structure.Empty || _grid.WallAt(cell) != null || Structures.Of(structure).Build is not { } build) return null;
        return (_grid.PieceOf(cell)[0], Builds.Get(build));
    }

    /// <summary>
    /// Why peer <paramref name="peer"/>'s hit of <paramref name="tool"/> (<see cref="Hew"/>) would be refused: what the client
    /// shows before it sends, which leaves out whether an electric weapon runs, as a hit on debris does.
    /// </summary>
    public string? HewProblem(int peer, string tool, int x, int y, int heading) =>
        HewProblemOf(peer, Struck(tool, x, y, heading), tool, running: false);

    /// <summary>Why peer <paramref name="peer"/>'s <see cref="HewCells"/> of <paramref name="tool"/> on <paramref name="cells"/> would be refused, as <see cref="HewProblem"/> asks it.</summary>
    public string? HewCellsProblem(int peer, string tool, IReadOnlyList<int> cells) =>
        HewProblemOf(peer, Listed(cells), tool, running: false);

    /// <summary>
    /// For a fan that strikes nothing, <c>building.notHere</c> where the surface's tools break nothing (the ship: the plan takes
    /// things apart), else <c>building.nothingToDig</c> (bedrock and lava only stop it); then what the weapon says
    /// (<see cref="IItemsQueries.WeaponProblem"/>): flat, or, as the command applies, not running.
    /// </summary>
    private string? HewProblemOf(int peer, Hewn hewn, string tool, bool running) =>
        hewn.IsEmpty ? (Rules.ToolsBreak ? "building.nothingToDig" : "building.notHere") : _items.WeaponProblem(peer, tool, running);

    /// <summary>
    /// The weapon's block damage added once to each wall, piece and plant the fan strikes, its damage to each pile, walls and pieces
    /// first: one at its strength goes, what it is made of given to the sender, a wall's held growths' yields too; a plant at its strength is
    /// felled. A relic's cradle among them discharges into the sender once (0096).
    /// </summary>
    private void ApplyHew(Hew c) => HitAll(Struck(c.Tool, c.X, c.Y, c.Heading), c.Tool, c.From, (c.X, c.Y));

    // A hero not on this surface aims no carry: the cells' own word says nothing of where the blow came from.
    private void ApplyHewCells(HewCells c) =>
        HitAll(Listed(c.Cells), c.Tool, c.From, _crew.HeroOf(c.From) is { } hero ? (hero.Cell % _grid.Width * 1000 + 500, hero.Cell / _grid.Width * 1000 + 500) : null);

    /// <summary>
    /// The fan's hits (<see cref="Hew"/>), then each wall or piece one broke passes its weapon's <c>Carries</c> share of what went
    /// over, rounded down, to the cell one step further along the line from <paramref name="origin"/>, in thousandths of a cell,
    /// through it (unified-damage): a cell two carries reach takes the larger, once; a carry is not carried again.
    /// </summary>
    private void HitAll(Hewn hewn, string tool, int from, (int X, int Y)? origin)
    {
        int hit = _items.BlockDamage(from, tool);
        // A tool that cuts wrecks only (the plasma cutter) touches no block nor pile: no crack, no felling, no cradle's shock.
        if (hit <= 0) return;
        // A relic's cradle discharges into whoever strikes it (0096), the felling hit too, before it gives its shards.
        if (hewn.Plants.Any(p => p.Kind == Ruins.Cradle)) _crewOps.Shock(from, ShockOf(tool), Harm.Shock);
        int share = Weapons.Of(tool)!.Carries;
        var carries = new List<(int Key, int Carry)>();
        foreach (int cell in hewn.Cells)
        {
            int? left;
            if (PileAt(cell) is { } pile)
            {
                HitPile(from, pile, _items.Damage(from, tool));
                continue;
            }
            else if (_grid.WallAt(cell) != null) left = HitWall(from, cell, hit);
            else left = HitPiece(from, cell, hit);
            if (left is int l && origin is { } o && l * share / 100 is var carry and > 0 && Behind(o, cell) is int key)
                carries.Add((key, carry));
        }
        // A plant its wall took with it is gone; one of no strength falls to any hit.
        foreach (var plant in hewn.Plants)
            if (_plantOwn.GetValueOrDefault(plant.Cell) == plant && Cracked(plant.Cell, Kinds[plant.Kind].Strength, hit) != null) Fell(plant, from);
        // What the fan struck took its hit already; what it broke is gone.
        for (int i = carries.Count - 1; i >= 0; i--)
            if (Carried(carries[i].Key) is int key && !hewn.Cells.Contains(key)) carries[i] = (key, carries[i].Carry);
            else carries.RemoveAt(i);
        carries.Sort((a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : b.Carry.CompareTo(a.Carry));
        for (int i = 0; i < carries.Count; i++)
        {
            if (i > 0 && carries[i].Key == carries[i - 1].Key) continue;
            if (_grid.WallAt(carries[i].Key) != null) HitWall(from, carries[i].Key, carries[i].Carry);
            else HitPiece(from, carries[i].Key, carries[i].Carry);
        }
    }

    /// <summary>
    /// The cell one step further than <paramref name="cell"/> along the line from <paramref name="origin"/> through its centre,
    /// in eight directions (x steps when 5·|dx| ≥ 2·|dy|, likewise y); none from its very centre or off the grid.
    /// </summary>
    private int? Behind((int X, int Y) origin, int cell)
    {
        int x = cell % _grid.Width, y = cell / _grid.Width;
        int dx = x * 1000 + 500 - origin.X, dy = y * 1000 + 500 - origin.Y;
        int sx = 5 * Math.Abs(dx) >= 2 * Math.Abs(dy) ? Math.Sign(dx) : 0, sy = 5 * Math.Abs(dy) >= 2 * Math.Abs(dx) ? Math.Sign(dy) : 0;
        if (sx == 0 && sy == 0) return null;
        int bx = x + sx, by = y + sy;
        return bx < 0 || by < 0 || bx >= _grid.Width || by >= _grid.Height ? null : by * _grid.Width + bx;
    }

    /// <summary>
    /// What a carry strikes at <paramref name="cell"/>: a wall that breaks, by its cell, or a built piece, by its first cell,
    /// where the surface's tools break things in its known zone, or in a vessel's box; never a pile, a plant or the fog.
    /// </summary>
    private int? Carried(int cell)
    {
        bool open = Rules.ToolsBreak ? _grid.Zone.Contains(cell % _grid.Width, cell / _grid.Width) : InVessel(cell);
        if (!open || PileAt(cell) != null) return null;
        if (_grid.WallAt(cell) is { } wall) return wall.Strength != Material.Unbreakable ? cell : null;
        return PieceAt(cell)?.Head;
    }

    /// <summary>
    /// A hit of <paramref name="damage"/> on what stands solid in <paramref name="cell"/> alone, by peer <paramref name="peer"/>'s
    /// shot (shots-are-the-sims): a wall that breaks, or a built piece, a floor too, as a <see cref="Hew"/>'s ray strikes it; what
    /// it gives to the peer, or, its hero not on this surface (gone aboard, left, or none), falling there, anyone's. Nothing where
    /// the surface's tools break nothing, nor on what never breaks (bedrock, lava). A shot stops only where it is solid, so never
    /// here for a ladder, an open door, a back wall or a plant. What was left over once it broke; null when it held (unified-damage).
    /// </summary>
    public int? Hit(int peer, int cell, int damage)
    {
        if (damage <= 0 || !ShotTakes(cell)) return null;
        int? here = _crew.HeroOf(peer) != null ? peer : null;
        return _grid.WallAt(cell) != null ? HitWall(here, cell, damage) : HitPiece(here, PieceAt(cell)!.Value.Head, damage);
    }

    /// <inheritdoc/>
    // Nothing breaks in a planet's fog, where a shot already stops as at a wall.
    public bool ShotTakes(int cell) =>
        Rules.ToolsBreak && _grid.Zone.Contains(cell % _grid.Width, cell / _grid.Width)
        && (_grid.WallAt(cell) is { } wall ? wall.Strength != Material.Unbreakable : PieceAt(cell) != null);

    /// <inheritdoc/>
    public bool Wears(int cell) =>
        Rules.ToolsBreak && _grid.Zone.Contains(cell % _grid.Width, cell / _grid.Width)
        && _grid.WallAt(cell) is { Breaks: true } && _grid.GrownWall(cell);

    /// <inheritdoc/>
    // Cut rock gives nothing: a hunter is no mine. What the wall held the sweep fells, giving nothing too.
    public bool Wear(int cell, int damage)
    {
        if (damage <= 0 || !Wears(cell) || Cracked(cell, _grid.WallAt(cell)!.Strength, damage) == null) return false;
        _gridOps.Clear(cell, "structure");
        return true;
    }

    /// <inheritdoc/>
    public int ShotKey(int cell) => _grid.WallAt(cell) == null && PieceAt(cell) is { } piece ? piece.Head : cell;

    /// <inheritdoc/>
    public int? Surplus(int cell, int damage)
    {
        if (damage <= 0 || !ShotTakes(cell)) return null;
        int key = ShotKey(cell), strength = _grid.WallAt(cell) is { } wall ? wall.Strength : PieceAt(key)!.Value.Build.Strength;
        int at = CrackIndex(key), taken = (at >= 0 ? State.Cracks[at].Damage : 0) + damage;
        return taken >= strength ? taken - strength : null;
    }

    /// <summary>Gives <paramref name="cost"/> to peer <paramref name="peer"/>, what does not fit falling at <paramref name="cell"/>; for none, all of it there, anyone's.</summary>
    private void Yield(int? peer, IReadOnlyList<(string Item, int Count)> cost, int cell)
    {
        if (peer is int p) _itemsOps.GiveOrDrop(p, cost, cell);
        else _itemsOps.DropAt(FloorStack.Nobody, cost, cell);
    }

    /// <summary>A hit of <paramref name="hit"/> on <paramref name="cell"/>'s wall: broken at its strength, for <paramref name="peer"/>: what was left over, null when it held.</summary>
    private int? HitWall(int? peer, int cell, int hit)
    {
        var wall = _grid.WallAt(cell)!;
        if (Cracked(cell, wall.Strength, hit) is not int surplus) return null;
        _gridOps.Clear(cell, "structure");
        Yield(peer, wall.Cost, cell);
        // What the wall held goes to its digger now; the sweep would fell it the same tick, giving nothing.
        foreach (var plant in HeldBy(cell))
            if (_plantOwn.GetValueOrDefault(plant.Cell) == plant) Fell(plant, peer);
        return surplus;
    }

    /// <summary>
    /// A hit of <paramref name="hit"/> on the built piece kept by <paramref name="head"/>: at its build's strength it comes apart
    /// whole, as a mark takes it (0104), and gives <paramref name="peer"/> what it cost: what was left over, null when it held.
    /// </summary>
    private int? HitPiece(int? peer, int head, int hit)
    {
        if (PieceAt(head) is not { } piece || Cracked(head, piece.Build.Strength, hit) is not int surplus) return null;
        var cost = Costs.BuiltAt(_grid, head, "structure")!.Cost;
        foreach (int cell in _grid.PieceOf(head)) _gridOps.Clear(cell, "structure");
        Yield(peer, cost, head);
        return surplus;
    }

    /// <summary>
    /// A hit of <paramref name="hit"/>, the weapon's damage as on a wreck, on <paramref name="pile"/> (2026-11-07-pile-hit-points): at its hit
    /// points it goes, and gives <paramref name="peer"/> what it holds.
    /// </summary>
    private void HitPile(int peer, Machine pile, int hit)
    {
        var spec = Machines.Get(pile.Kind);
        if (Cracked(pile.Cell, spec.Hp!.Value, hit) == null) return;
        _gridOps.Clear(pile.Cell, "machine");
        Yield(peer, spec.Cost, pile.Cell);
    }

    /// <summary>
    /// The units of a suit a hit of <paramref name="tool"/> on a cradle costs (0096): <see cref="Ruins.CradleShock"/> for the axe's,
    /// a faster tool's its pace's share of the axe's, rounded up (a cost): the drill's 1, 10 a second, a module in it or not.
    /// </summary>
    public static int ShockOf(string tool)
    {
        int pace = Weapons.Of(tool)!.Pace, axe = Weapons.Axe.Pace;
        return (Ruins.CradleShock * pace + axe - 1) / axe;
    }

    /// <summary>
    /// The growths <paramref name="cell"/>'s wall holds (<see cref="HoldsOf"/>), in cell order: one standing on it, one in
    /// its own stone, one hanging from it, in front or behind; a piece over several columns by any of them (0091).
    /// </summary>
    private List<Plant> HeldBy(int cell)
    {
        var held = new List<Plant>();
        foreach (int at in (int[])[cell - _grid.Width, cell, cell + _grid.Width])
            foreach (var layer in (Dictionary<int, Plant>[])[_plantIn, _plantBehind])
                if (layer.TryGetValue(at, out var plant) && !held.Contains(plant) && Kinds[plant.Kind].On != Flora.Anchor.Back && HoldsOf(plant).Contains(cell))
                    held.Add(plant);
        held.Sort(ByCell);
        return held;
    }

    /// <summary>
    /// Adds a hit's <paramref name="hit"/> damage, its sender's weapon's on blocks (<see cref="IItemsQueries.BlockDamage"/>), to
    /// <paramref name="cell"/>'s crack: once it reaches <paramref name="strength"/>, its crack dropped, what went over it (0 for
    /// none); else null, the crack kept deeper, its last hit now.
    /// </summary>
    private int? Cracked(int cell, int strength, int hit, bool back = false)
    {
        int at = CrackIndex(cell, back);
        int damage = (at >= 0 ? State.Cracks[at].Damage : 0) + hit;
        if (damage >= strength)
        {
            if (at >= 0) State.Cracks.RemoveAt(at);
            return damage - strength;
        }
        if (at >= 0) State.Cracks[at] = new Crack(cell, damage, State.Clock, back);
        else State.Cracks.Insert(~at, new Crack(cell, damage, State.Clock, back));
        return null;
    }

    /// <summary>
    /// Where <paramref name="cell"/>'s crack in the front or the <paramref name="back"/> layer is in <see cref="BuildingState.Cracks"/>
    /// (by cell, the front's first); the complement of where it would go when it has none.
    /// </summary>
    private int CrackIndex(int cell, bool back = false)
    {
        var cracks = State.Cracks;
        long key = cell * 2L + (back ? 1 : 0);
        int lo = 0, hi = cracks.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            long at = cracks[mid].Cell * 2L + (cracks[mid].Back ? 1 : 0);
            if (at == key) return mid;
            if (at < key) lo = mid + 1;
            else hi = mid - 1;
        }
        return ~lo;
    }

    /// <summary>
    /// A crack heals whole <c>Builds.CrackHeals</c> ticks after its last hit, as Terraria's; one whose wall is gone, to a meteor,
    /// is dropped: only a natural wall that breaks, a pile, or a plant of some strength in its own open cell, keeps one. The clock
    /// ticks first.
    /// </summary>
    private void SweepCracks()
    {
        int now = ++State.Clock;
        if (State.Cracks.Count > 0) State.Cracks.RemoveAll(k => now - k.At >= Builds.CrackHeals || !Crackable(k.Cell, k.Back));
    }

    /// <summary>
    /// Whether <paramref name="cell"/> holds what a crack is kept for: a wall that breaks, the first cell of a built piece or of a machine
    /// that breaks (a pile, a built one: unified-damage), or the
    /// own cell of a plant with strength; in the <paramref name="back"/> layer, a back wall that breaks (cracked before tools
    /// left the back walls, healing out of old saves).
    /// </summary>
    private bool Crackable(int cell, bool back) => back ? _grid.BackAt(cell) is { } behind && behind.Strength != Material.Unbreakable
        : _grid.WallAt(cell) is { } wall ? wall.Strength != Material.Unbreakable
        : PieceAt(cell) is { } piece ? piece.Head == cell
        : _grid.MachineAt(cell) is { } machine ? machine.Cell == cell && Machines.Get(machine.Kind).HitPoints != null
        : _plantOwn.TryGetValue(cell, out var plant) && Kinds[plant.Kind].Strength > 0;
}
