using Stillwake.Content;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;

namespace Stillwake.Sim.Building;

/// <summary>
/// A hit of weapon <c>Tool</c> on the natural wall at <c>Cell</c> (0077): sent as the axe strikes, or at the drill's pace
/// while it works, at a natural wall within the hero's reach. Its damage cracks the wall; the hit that reaches its
/// strength breaks it out, the back wall behind staying, and gives the sender what it is made of.
/// </summary>
public sealed record Dig(int Cell, string Tool) : Command;

/// <summary>Digging a planet's ground (0077): cracks kept by cell, a wall broken out at its strength.</summary>
public sealed partial class BuildingModule
{
    /// <summary>The natural walls dug part way, ascending by cell: live and read-only.</summary>
    public IReadOnlyList<Crack> Cracks => State.Cracks;

    /// <summary>
    /// Why peer <paramref name="peer"/>'s hit of <paramref name="tool"/> on <paramref name="cell"/> would be refused: what the
    /// client shows before it sends, which leaves out whether an electric weapon runs, as a hit on debris does.
    /// </summary>
    public string? DigProblem(int peer, int cell, string tool) => DigProblemOf(peer, cell, tool, running: false);

    /// <summary>
    /// <c>building.nothingToDig</c> for no natural wall there (a hull wall is taken apart with a mark), <c>building.bedrock</c>
    /// for one nothing breaks, then what the weapon says (<see cref="IItemsQueries.WeaponProblem"/>): flat, or, as the
    /// command applies, not running. A tool that deals no damage is our bug: the client sends weapons only.
    /// </summary>
    private string? DigProblemOf(int peer, int cell, string tool, bool running)
    {
        if (Weapons.Of(tool) == null) throw new InvalidOperationException($"building.dig: {tool} deals no damage");
        if (_grid.WallAt(cell) is not { Natural: true } wall) return "building.nothingToDig";
        if (!wall.Breaks) return "building.bedrock";
        return _items.WeaponProblem(peer, tool, running);
    }

    /// <summary>
    /// The weapon's damage added to the wall's crack; reaching its strength, the wall is cleared, its crack with it, and
    /// what it is made of given to the sender, what does not fit falling where it stood.
    /// </summary>
    private void ApplyDig(Dig c)
    {
        var wall = _grid.WallAt(c.Cell)!;
        int at = CrackIndex(c.Cell);
        int damage = (at >= 0 ? State.Cracks[at].Damage : 0) + Weapons.Of(c.Tool)!.Damage;
        if (damage < wall.Strength)
        {
            if (at >= 0) State.Cracks[at] = new Crack(c.Cell, damage);
            else State.Cracks.Insert(~at, new Crack(c.Cell, damage));
            return;
        }
        if (at >= 0) State.Cracks.RemoveAt(at);
        _gridOps.Clear(c.Cell, "structure");
        _itemsOps.GiveOrDrop(c.From, wall.Cost, c.Cell);
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

    /// <summary>A crack whose wall is gone, to a meteor, is dropped: only a natural wall that breaks keeps one.</summary>
    private void SweepCracks() => State.Cracks.RemoveAll(k => _grid.WallAt(k.Cell) is not { Breaks: true });

    /// <summary>Whether what <paramref name="layer"/> of <paramref name="cell"/> holds is natural ground: dug, never marked.</summary>
    private bool IsNatural(int cell, string layer) => layer switch
    {
        "structure" => _grid.WallAt(cell)?.Natural == true,
        "back" => _grid.BackAt(cell)?.Natural == true,
        _ => false,
    };
}
