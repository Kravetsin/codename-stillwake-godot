using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Building;

/// <summary>
/// The cost under the pointer while the power driver works a square (power-driver): what its blueprints need together,
/// else what its marks give back together.
/// </summary>
public static class SquareTip
{
    /// <summary>
    /// What every blueprint over <paramref name="cells"/> that still lacks something needs, together, item by item in the
    /// order they first name them, each its cost less what was delivered to it (<see cref="CostLine.Lacking"/>), against what
    /// is <paramref name="carried"/>. While none lacks anything, what every mark there gives back once taken down, together
    /// (<see cref="BuildingModule.RefundOf"/>), different walls and machines alike. Under their name and picture when all
    /// place or take down one thing, else lines alone. None with neither.
    /// </summary>
    public static Tip? Of(BuildingModule building, IGridQueries grid, IReadOnlyList<int> cells, IReadOnlyList<(string Item, int Count)> carried)
    {
        var plans = new List<Sim.Building.Plan>();
        var marks = new List<Sim.Building.Plan>();
        foreach (int cell in cells)
            foreach (var plan in building.PlansAt(cell))
            {
                if (plans.Any(p => p.Id == plan.Id) || marks.Any(p => p.Id == plan.Id)) continue;
                if (plan.Place != null && building.Lacking(plan.Id).Count > 0) plans.Add(plan);
                else if (plan.Clear != null && Tools.MarkedOf(grid, plan) != null) marks.Add(plan);
            }
        if (plans.Count > 0) return Blueprints(building, plans, carried);
        return marks.Count > 0 ? Marks(building, grid, marks) : null;
    }

    private static Tip Blueprints(BuildingModule building, List<Sim.Building.Plan> plans, IReadOnlyList<(string Item, int Count)> carried)
    {
        var need = new List<(string Item, int Count)>();
        foreach (var plan in plans)
            foreach (var line in CostLine.Lacking(building.CostOf(plan.Id)!, plan.Delivered.Select(s => (s.Item, s.Count)).ToList(), carried))
                Add(need, line.Item, line.Need);
        var lines = need.Select(n => new CostLine(n.Item, carried.Where(c => c.Item == n.Item).Sum(c => c.Count), n.Count)).ToList();
        Placement place = plans[0].Place!;
        return plans.All(p => p.Place == place)
            ? new PlanTip(place, lines).Tip
            : new Tip(null, lines.Select(c => new TipLine(c.Count, c.Tone, c.Item, Large: true, Faded: c.Done)).ToList());
    }

    private static Tip Marks(BuildingModule building, IGridQueries grid, List<Sim.Building.Plan> marks)
    {
        var back = new List<(string Item, int Count)>();
        foreach (var mark in marks)
            foreach (var (item, count) in building.RefundOf(mark.Id) ?? []) Add(back, item, count);
        var built = marks.Select(m => Tools.MarkedOf(grid, m)!).ToList();
        var tip = new MarkTip(built[0], back).Tip;
        return built.All(b => b == built[0]) ? tip : tip with { Name = null, Picture = null };
    }

    /// <summary>Adds <paramref name="count"/> of <paramref name="item"/> to its row, or a new row in the order first named.</summary>
    private static void Add(List<(string Item, int Count)> rows, string item, int count)
    {
        int at = rows.FindIndex(n => n.Item == item);
        if (at < 0) rows.Add((item, count));
        else rows[at] = (item, rows[at].Count + count);
    }
}
