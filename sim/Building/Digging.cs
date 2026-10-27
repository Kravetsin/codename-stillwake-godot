using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Building;

/// <summary>
/// A hit of weapon <c>Tool</c> on what stands on a planet's grid (the fan of rays): its fan cast from the hand at (<c>X</c>,
/// <c>Y</c>), in thousandths of a cell, aimed at <c>Heading</c> (<see cref="Headings"/>), sent as the axe strikes or at the
/// drill's pace. Each ray strikes the first wall it meets, a closed door or hatch, a floor, and stops there; it passes through
/// the plants, ladders and open doors it crosses, striking them. What stands takes the weapon's block damage
/// (<c>BlockDamage</c>) once, however many rays meet it: a wall at its material's strength, a built piece at its build's, and
/// gives the sender what it is made of. Only where the surface's tools break things (<c>ToolsBreak</c>): on the ship the plan
/// alone takes things apart.
/// </summary>
public sealed record Hew(string Tool, int X, int Y, int Heading) : Command;

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
    /// would strike (<see cref="Hew"/>): what the command strikes, and the client's preview, so the two never differ. Nothing
    /// where the surface's tools break nothing.
    /// </summary>
    public Hewn Struck(string tool, int x, int y, int heading)
    {
        var weapon = Weapons.Of(tool) ?? throw new InvalidOperationException($"building.hew: {tool} deals no damage");
        var cells = new List<int>();
        var plants = new List<Plant>();
        if (Rules.ToolsBreak) Rays.Fan(_grid.Width, _grid.Height, x, y, heading, weapon, cell => Stops(cell, cells, plants));
        cells.Sort();
        for (int i = cells.Count - 1; i > 0; i--)
            if (cells[i] == cells[i - 1]) cells.RemoveAt(i);
        plants.Sort(ByCell);
        return new Hewn(cells, plants);
    }

    /// <summary>
    /// Whether a ray stops at <paramref name="cell"/>: at a wall, one that breaks added to <paramref name="cells"/>; at a built
    /// piece that stops a body, added by its first cell; through a piece that does not (a ladder, an open door) and a plant in an
    /// open cell, each added once.
    /// </summary>
    private bool Stops(int cell, List<int> cells, List<Plant> plants)
    {
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

    /// <summary>
    /// <c>building.notHere</c> where the surface's tools break nothing (the ship: the plan takes things apart),
    /// <c>building.nothingToDig</c> for a fan that strikes nothing (bedrock and lava only stop it), then what the weapon says
    /// (<see cref="IItemsQueries.WeaponProblem"/>): flat, or, as the command applies, not running.
    /// </summary>
    private string? HewProblemOf(int peer, Hewn hewn, string tool, bool running) =>
        !Rules.ToolsBreak ? "building.notHere" : hewn.IsEmpty ? "building.nothingToDig" : _items.WeaponProblem(peer, tool, running);

    /// <summary>
    /// The weapon's block damage added once to each wall, piece and plant the fan strikes, walls and pieces first: one at its
    /// strength goes, what it is made of given to the sender, a wall's held growths' yields too; a plant at its strength is
    /// felled. A relic's cradle among them discharges into the sender once (0096).
    /// </summary>
    private void ApplyHew(Hew c)
    {
        var hewn = Struck(c.Tool, c.X, c.Y, c.Heading);
        int hit = _items.BlockDamage(c.From, c.Tool);
        // A relic's cradle discharges into whoever strikes it (0096), the felling hit too, before it gives its shards.
        if (hewn.Plants.Any(p => p.Kind == Ruins.Cradle)) _crewOps.Shock(c.From, ShockOf(c.Tool));
        foreach (int cell in hewn.Cells)
        {
            if (_grid.WallAt(cell) != null) HitWall(c.From, cell, hit);
            else HitPiece(c.From, cell, hit);
        }
        // A plant its wall took with it is gone; one of no strength falls to any hit.
        foreach (var plant in hewn.Plants)
            if (_plantOwn.GetValueOrDefault(plant.Cell) == plant && Cracked(plant.Cell, Kinds[plant.Kind].Strength, hit)) Fell(plant, c.From);
    }

    /// <summary>
    /// A hit of <paramref name="damage"/> on what stands solid in <paramref name="cell"/> alone, by peer <paramref name="peer"/>'s
    /// shot (shots-are-the-sims): a wall that breaks, or a built piece, a floor too, as a <see cref="Hew"/>'s ray strikes it; what
    /// it gives to the peer, or, its hero not on this surface (gone aboard, left, or none), falling there, anyone's. Nothing where
    /// the surface's tools break nothing, nor on what never breaks (bedrock, lava). A shot stops only where it is solid, so never
    /// here for a ladder, an open door, a back wall or a plant. True when what stood there broke (2026-10-27-shots-pierce).
    /// </summary>
    public bool Hit(int peer, int cell, int damage)
    {
        if (damage <= 0 || !ShotTakes(cell)) return false;
        int? here = _crew.HeroOf(peer) != null ? peer : null;
        return _grid.WallAt(cell) != null ? HitWall(here, cell, damage) : HitPiece(here, PieceAt(cell)!.Value.Head, damage);
    }

    /// <inheritdoc/>
    // Nothing breaks in a planet's fog, where a shot already stops as at a wall.
    public bool ShotTakes(int cell) =>
        Rules.ToolsBreak && _grid.Zone.Contains(cell % _grid.Width, cell / _grid.Width)
        && (_grid.WallAt(cell) is { } wall ? wall.Strength != Material.Unbreakable : PieceAt(cell) != null);

    /// <inheritdoc/>
    public int ShotKey(int cell) => _grid.WallAt(cell) == null && PieceAt(cell) is { } piece ? piece.Head : cell;

    /// <summary>Gives <paramref name="cost"/> to peer <paramref name="peer"/>, what does not fit falling at <paramref name="cell"/>; for none, all of it there, anyone's.</summary>
    private void Yield(int? peer, IReadOnlyList<(string Item, int Count)> cost, int cell)
    {
        if (peer is int p) _itemsOps.GiveOrDrop(p, cost, cell);
        else _itemsOps.DropAt(FloorStack.Nobody, cost, cell);
    }

    /// <summary>A hit of <paramref name="hit"/> on <paramref name="cell"/>'s wall: broken at its strength, for <paramref name="peer"/>: true when it broke.</summary>
    private bool HitWall(int? peer, int cell, int hit)
    {
        var wall = _grid.WallAt(cell)!;
        if (!Cracked(cell, wall.Strength, hit)) return false;
        _gridOps.Clear(cell, "structure");
        Yield(peer, wall.Cost, cell);
        // What the wall held goes to its digger now; the sweep would fell it the same tick, giving nothing.
        foreach (var plant in HeldBy(cell))
            if (_plantOwn.GetValueOrDefault(plant.Cell) == plant) Fell(plant, peer);
        return true;
    }

    /// <summary>
    /// A hit of <paramref name="hit"/> on the built piece kept by <paramref name="head"/>: at its build's strength it comes apart
    /// whole, as a mark takes it (0104), and gives <paramref name="peer"/> what it cost: true when it came apart.
    /// </summary>
    private bool HitPiece(int? peer, int head, int hit)
    {
        if (PieceAt(head) is not { } piece || !Cracked(head, piece.Build.Strength, hit)) return false;
        var cost = Costs.BuiltAt(_grid, head, "structure")!.Cost;
        foreach (int cell in _grid.PieceOf(head)) _gridOps.Clear(cell, "structure");
        Yield(peer, cost, head);
        return true;
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
    /// <paramref name="cell"/>'s crack: true, its crack dropped, once it reaches <paramref name="strength"/>; else false, the
    /// crack kept deeper, its last hit now.
    /// </summary>
    private bool Cracked(int cell, int strength, int hit, bool back = false)
    {
        int at = CrackIndex(cell, back);
        int damage = (at >= 0 ? State.Cracks[at].Damage : 0) + hit;
        if (damage >= strength)
        {
            if (at >= 0) State.Cracks.RemoveAt(at);
            return true;
        }
        if (at >= 0) State.Cracks[at] = new Crack(cell, damage, State.Clock, back);
        else State.Cracks.Insert(~at, new Crack(cell, damage, State.Clock, back));
        return false;
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
    /// is dropped: only a natural wall that breaks, or a plant of some strength in its own open cell, keeps one. The clock
    /// ticks first.
    /// </summary>
    private void SweepCracks()
    {
        int now = ++State.Clock;
        if (State.Cracks.Count > 0) State.Cracks.RemoveAll(k => now - k.At >= Builds.CrackHeals || !Crackable(k.Cell, k.Back));
    }

    /// <summary>
    /// Whether <paramref name="cell"/> holds what a crack is kept for: a wall that breaks, the first cell of a built piece, or the
    /// own cell of a plant with strength; in the <paramref name="back"/> layer, a back wall that breaks (cracked before tools
    /// left the back walls, healing out of old saves).
    /// </summary>
    private bool Crackable(int cell, bool back) => back ? _grid.BackAt(cell) is { } behind && behind.Strength != Material.Unbreakable
        : _grid.WallAt(cell) is { } wall ? wall.Strength != Material.Unbreakable
        : PieceAt(cell) is { } piece ? piece.Head == cell
        : _plantOwn.TryGetValue(cell, out var plant) && Kinds[plant.Kind].Strength > 0;
}
