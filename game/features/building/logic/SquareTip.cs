using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;

namespace Stillwake.Game.Features.Building;

/// <summary>The cost under the pointer while the power driver works a square (power-driver): what its blueprints need together.</summary>
public static class SquareTip
{
    /// <summary>
    /// What every blueprint over <paramref name="cells"/> that still lacks something needs, together, item by item in the
    /// order they first name them, each its cost less what was delivered to it (<see cref="CostLine.Lacking"/>), against what
    /// is <paramref name="carried"/>: under the blueprints' name and picture when all place one thing, else lines alone.
    /// None while no blueprint there lacks anything.
    /// </summary>
    public static Tip? Of(BuildingModule building, IReadOnlyList<int> cells, IReadOnlyList<(string Item, int Count)> carried)
    {
        var plans = new List<Sim.Building.Plan>();
        foreach (int cell in cells)
            foreach (var plan in building.PlansAt(cell))
                if (plan.Place != null && building.Lacking(plan.Id).Count > 0 && !plans.Any(p => p.Id == plan.Id)) plans.Add(plan);
        if (plans.Count == 0) return null;
        var need = new List<(string Item, int Need)>();
        foreach (var plan in plans)
            foreach (var line in CostLine.Lacking(building.CostOf(plan.Id)!, plan.Delivered.Select(s => (s.Item, s.Count)).ToList(), carried))
            {
                int at = need.FindIndex(n => n.Item == line.Item);
                if (at < 0) need.Add((line.Item, line.Need));
                else need[at] = (line.Item, need[at].Need + line.Need);
            }
        var lines = need.Select(n => new CostLine(n.Item, carried.Where(c => c.Item == n.Item).Sum(c => c.Count), n.Need)).ToList();
        Placement place = plans[0].Place!;
        return plans.All(p => p.Place == place)
            ? new PlanTip(place, lines).Tip
            : new Tip(null, lines.Select(c => new TipLine(c.Count, c.Tone, c.Item, Large: true, Faded: c.Done)).ToList());
    }
}
