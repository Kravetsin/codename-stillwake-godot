using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Building;

/// <summary>
/// A hit of weapon <c>Tool</c> on the natural wall at <c>Cell</c> (0077): sent as the axe strikes, or at the drill's pace
/// while it works, at a natural wall within the hero's reach. Its damage cracks the wall; the hit that reaches its
/// strength breaks it out, the back wall behind staying, and gives the sender what it is made of and what grew on, in or
/// from it. On a plant standing in an open cell (0079), it cracks the plant by its own cell, felling it at its strength.
/// </summary>
public sealed record Dig(int Cell, string Tool) : Command;

/// <summary>Digging a planet's ground (0077): cracks kept by cell, a wall broken out at its strength.</summary>
public sealed partial class BuildingModule
{
    /// <summary>The natural walls and the plants (by their own cells) hit part way, ascending by cell: live and read-only.</summary>
    public IReadOnlyList<Crack> Cracks => State.Cracks;

    /// <summary>
    /// Why peer <paramref name="peer"/>'s hit of <paramref name="tool"/> on <paramref name="cell"/> would be refused: what the
    /// client shows before it sends, which leaves out whether an electric weapon runs, as a hit on debris does.
    /// </summary>
    public string? DigProblem(int peer, int cell, string tool) => DigProblemOf(peer, cell, tool, running: false);

    /// <summary>
    /// <c>building.nothingToDig</c> for no natural wall there (a hull wall is taken apart with a mark) nor a plant in an open
    /// cell (0079), <c>building.bedrock</c>
    /// for one nothing breaks, then what the weapon says (<see cref="IItemsQueries.WeaponProblem"/>): flat, or, as the
    /// command applies, not running. A tool that deals no damage is our bug: the client sends weapons only.
    /// </summary>
    private string? DigProblemOf(int peer, int cell, string tool, bool running)
    {
        if (Weapons.Of(tool) == null) throw new InvalidOperationException($"building.dig: {tool} deals no damage");
        if (_grid.WallAt(cell) is not { Natural: true } wall)
            return PlantAt(cell) != null && _grid.StructureAt(cell) == Structure.Empty ? _items.WeaponProblem(peer, tool, running) : "building.nothingToDig";
        if (!wall.Breaks) return "building.bedrock";
        return _items.WeaponProblem(peer, tool, running);
    }

    /// <summary>
    /// The weapon's damage added to the wall's crack; reaching its strength, the wall is cleared, its crack with it, and
    /// what it is made of given to the sender, then the yields of the growths it held, what does not fit falling where it stood.
    /// </summary>
    private void ApplyDig(Dig c)
    {
        // A plant is cracked by its own cell, whichever of its cells is hit; one of no strength falls to any hit.
        if (_grid.WallAt(c.Cell) is not { } wall)
        {
            var plant = PlantAt(c.Cell)!;
            if (Cracked(plant.Cell, Kinds[plant.Kind].Strength, c.Tool)) Fell(plant, c.From);
            return;
        }
        if (!Cracked(c.Cell, wall.Strength, c.Tool)) return;
        _gridOps.Clear(c.Cell, "structure");
        _itemsOps.GiveOrDrop(c.From, wall.Cost, c.Cell);
        // What the wall held goes to its digger now; the sweep would fell it the same tick, giving nothing.
        foreach (var plant in HeldBy(c.Cell))
            if (_plantOwn.GetValueOrDefault(plant.Cell) == plant) Fell(plant, c.From);
    }

    /// <summary>
    /// The growths <paramref name="cell"/>'s wall holds (<see cref="HoldsOf"/>), in cell order: one standing on it, one in
    /// its own stone, one hanging from it, in front or behind; a piece over several columns by any of them (0090).
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
    /// Adds <paramref name="tool"/>'s damage to <paramref name="cell"/>'s crack: true, its crack dropped, once it reaches
    /// <paramref name="strength"/>; else false, the crack kept deeper, its last hit now.
    /// </summary>
    private bool Cracked(int cell, int strength, string tool)
    {
        int at = CrackIndex(cell);
        int damage = (at >= 0 ? State.Cracks[at].Damage : 0) + Weapons.Of(tool)!.Damage;
        if (damage >= strength)
        {
            if (at >= 0) State.Cracks.RemoveAt(at);
            return true;
        }
        if (at >= 0) State.Cracks[at] = new Crack(cell, damage, State.Clock);
        else State.Cracks.Insert(~at, new Crack(cell, damage, State.Clock));
        return false;
    }

    /// <summary>Where <paramref name="cell"/>'s crack is in <see cref="BuildingState.Cracks"/>; the complement of where it would go when it has none.</summary>
    private int CrackIndex(int cell)
    {
        var cracks = State.Cracks;
        int lo = 0, hi = cracks.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int at = cracks[mid].Cell;
            if (at == cell) return mid;
            if (at < cell) lo = mid + 1;
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
        if (State.Cracks.Count > 0) State.Cracks.RemoveAll(k => now - k.At >= Builds.CrackHeals || !Crackable(k.Cell));
    }

    /// <summary>Whether <paramref name="cell"/> holds what a crack is kept for: a natural wall that breaks, or the own cell of a plant with strength.</summary>
    private bool Crackable(int cell) => _grid.WallAt(cell) is { } wall
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
