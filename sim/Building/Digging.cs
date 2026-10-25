using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Building;

/// <summary>
/// A hit of weapon <c>Tool</c> on a planet's ground (the fan of rays): its fan cast from the hand at (<c>X</c>, <c>Y</c>), in
/// thousandths of a cell, aimed at <c>Heading</c> (<see cref="Headings"/>), sent as the axe strikes or at the drill's pace. Each
/// ray strikes the first wall it meets and stops there, a natural one cracked, a ship's or bedrock untouched; it passes through
/// the plants it crosses, cracking each by its own cell. Each wall and plant takes the damage once, however many rays meet it.
/// It takes the weapon's block damage (<c>BlockDamage</c>). <c>Back</c> (0107, the right button) strikes instead every natural back wall behind the open cells a ray crosses, until a
/// wall stops it (0108). A wall broken gives the sender what it is made of and what grew on, in or from it.
/// </summary>
public sealed record Hew(string Tool, int X, int Y, int Heading, bool Back = false) : Command;

/// <summary>What a <see cref="Hew"/> strikes: the cells of its walls, ascending, and its plants, by their own cells.</summary>
public sealed record Hewn(IReadOnlyList<int> Cells, IReadOnlyList<Plant> Plants)
{
    public bool IsEmpty => Cells.Count == 0 && Plants.Count == 0;
}

/// <summary>Digging a planet's ground (0077): cracks kept by cell, a wall broken out at its strength.</summary>
public sealed partial class BuildingModule
{
    /// <summary>The natural walls and the plants (by their own cells) hit part way, ascending by cell: live and read-only.</summary>
    public IReadOnlyList<Crack> Cracks => State.Cracks;

    /// <summary>
    /// What a hit of <paramref name="tool"/> from (<paramref name="x"/>, <paramref name="y"/>) toward <paramref name="heading"/>
    /// would strike (<see cref="Hew"/>), in the front layer or the <paramref name="back"/> one: what the command strikes, and the
    /// client's preview, so the two never differ.
    /// </summary>
    public Hewn Struck(string tool, int x, int y, int heading, bool back = false)
    {
        var weapon = Weapons.Of(tool) ?? throw new InvalidOperationException($"building.hew: {tool} deals no damage");
        var cells = new List<int>();
        var plants = new List<Plant>();
        Rays.Fan(_grid.Width, _grid.Height, x, y, heading, weapon, back ? cell => StopsBehind(cell, cells) : cell => Stops(cell, cells, plants));
        cells.Sort();
        for (int i = cells.Count - 1; i > 0; i--)
            if (cells[i] == cells[i - 1]) cells.RemoveAt(i);
        plants.Sort(ByCell);
        return new Hewn(cells, plants);
    }

    /// <summary>
    /// Whether a ray in the front layer stops at <paramref name="cell"/>: at any wall, a natural one that breaks added to
    /// <paramref name="cells"/>, and at whatever else stops a body, a shut door; a plant in an open cell added to
    /// <paramref name="plants"/>, once, and passed.
    /// </summary>
    private bool Stops(int cell, List<int> cells, List<Plant> plants)
    {
        if (_grid.WallAt(cell) is { } wall)
        {
            if (wall.Breaks) cells.Add(cell);
            return true;
        }
        if (_grid.Blocks(cell)) return true;
        if (_grid.StructureAt(cell) == Structure.Empty && PlantAt(cell) is { } plant && !plants.Contains(plant)) plants.Add(plant);
        return false;
    }

    /// <summary>
    /// Whether a ray in the back layer stops at <paramref name="cell"/>: at a wall or whatever stops a body; else its back wall,
    /// natural and breakable, added to <paramref name="cells"/>, a ship's passed by (0108).
    /// </summary>
    private bool StopsBehind(int cell, List<int> cells)
    {
        if (_grid.WallAt(cell) != null || _grid.Blocks(cell)) return true;
        if (_grid.BackAt(cell) is { Natural: true } behind && behind.Strength != Material.Unbreakable) cells.Add(cell);
        return false;
    }

    /// <summary>
    /// Why peer <paramref name="peer"/>'s hit of <paramref name="tool"/> (<see cref="Hew"/>) would be refused: what the client
    /// shows before it sends, which leaves out whether an electric weapon runs, as a hit on debris does.
    /// </summary>
    public string? HewProblem(int peer, string tool, int x, int y, int heading, bool back = false) =>
        HewProblemOf(peer, Struck(tool, x, y, heading, back), tool, running: false);

    /// <summary>
    /// <c>building.nothingToDig</c> for a fan that strikes nothing (bedrock and a ship's walls only stop it), then what the
    /// weapon says (<see cref="IItemsQueries.WeaponProblem"/>): flat, or, as the command applies, not running.
    /// </summary>
    private string? HewProblemOf(int peer, Hewn hewn, string tool, bool running) =>
        hewn.IsEmpty ? "building.nothingToDig" : _items.WeaponProblem(peer, tool, running);

    /// <summary>
    /// The weapon's damage added once to each wall and plant the fan strikes, walls first: a wall at its strength is cleared,
    /// what it is made of given to the sender, then the yields of the growths it held; a plant at its strength is felled. A
    /// relic's cradle among them discharges into the sender once (0096). A back wall broken fells what hangs on it.
    /// </summary>
    private void ApplyHew(Hew c)
    {
        var hewn = Struck(c.Tool, c.X, c.Y, c.Heading, c.Back);
        int hit = _items.BlockDamage(c.From, c.Tool);
        if (c.Back)
        {
            foreach (int cell in hewn.Cells) HitBack(c.From, cell, hit);
            return;
        }
        // A relic's cradle discharges into whoever strikes it (0096), the felling hit too, before it gives its shards.
        if (hewn.Plants.Any(p => p.Kind == Ruins.Cradle)) _crewOps.Shock(c.From, ShockOf(c.Tool));
        foreach (int cell in hewn.Cells) HitWall(c.From, cell, hit);
        // A plant its wall took with it is gone; one of no strength falls to any hit.
        foreach (var plant in hewn.Plants)
            if (_plantOwn.GetValueOrDefault(plant.Cell) == plant && Cracked(plant.Cell, Kinds[plant.Kind].Strength, hit)) Fell(plant, c.From);
    }

    /// <summary>A hit of <paramref name="hit"/> on <paramref name="cell"/>'s natural wall: broken at its strength, for <paramref name="peer"/>.</summary>
    private void HitWall(int peer, int cell, int hit)
    {
        var wall = _grid.WallAt(cell)!;
        if (!Cracked(cell, wall.Strength, hit)) return;
        _gridOps.Clear(cell, "structure");
        _itemsOps.GiveOrDrop(peer, wall.Cost, cell);
        // What the wall held goes to its digger now; the sweep would fell it the same tick, giving nothing.
        foreach (var plant in HeldBy(cell))
            if (_plantOwn.GetValueOrDefault(plant.Cell) == plant) Fell(plant, peer);
    }

    /// <summary>A hit of <paramref name="hit"/> on <paramref name="cell"/>'s natural back wall: broken at its strength, for <paramref name="peer"/>.</summary>
    private void HitBack(int peer, int cell, int hit)
    {
        var back = _grid.BackAt(cell)!;
        if (!Cracked(cell, back.Strength, hit, back: true)) return;
        // What hangs on it goes to its digger now; the sweep would fell it for nothing.
        var hung = HungOn(cell);
        _gridOps.Clear(cell, "back");
        _itemsOps.GiveOrDrop(peer, back.Cost, cell);
        foreach (var plant in hung)
            if (_plantOwn.GetValueOrDefault(plant.Cell) == plant) Fell(plant, peer);
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

    /// <summary>The growths hung on <paramref name="cell"/>'s back wall (<see cref="Flora.Anchor.Back"/>), in cell order: felled with it.</summary>
    private List<Plant> HungOn(int cell)
    {
        var hung = new List<Plant>();
        foreach (var layer in (Dictionary<int, Plant>[])[_plantIn, _plantBehind])
            if (layer.TryGetValue(cell, out var plant) && !hung.Contains(plant) && Kinds[plant.Kind].On == Flora.Anchor.Back && HoldsOf(plant).Contains(cell))
                hung.Add(plant);
        hung.Sort(ByCell);
        return hung;
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
    /// Whether <paramref name="cell"/> holds what a crack is kept for: a natural wall that breaks, or the own cell of a plant with
    /// strength; in the <paramref name="back"/> layer, a back wall that breaks.
    /// </summary>
    private bool Crackable(int cell, bool back) => back ? _grid.BackAt(cell) is { } behind && behind.Strength != Material.Unbreakable
        : _grid.WallAt(cell) is { } wall
        ? wall.Breaks
        : _plantOwn.TryGetValue(cell, out var plant) && Kinds[plant.Kind].Strength > 0;

    /// <summary>
    /// Whether what <paramref name="layer"/> of <paramref name="cell"/> holds is natural ground, never marked: a natural wall,
    /// dug, or the ground's own back wall; one a hero built of soil or stone is taken apart as any back wall (0084).
    /// </summary>
    private bool IsNatural(int cell, string layer) => layer switch
    {
        "structure" => _grid.WallAt(cell)?.Natural == true,
        "back" => _grid.BackAt(cell)?.Natural == true && _grid.GrownBack(cell),
        _ => false,
    };
}
