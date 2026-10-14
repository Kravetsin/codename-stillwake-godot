using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim;
using Stillwake.Sim.Building;

namespace Stillwake.Game.Features.Building;

/// <summary>A plan the left button works on, why the sim would refuse it, and whether it still lacks something (delivers rather than builds).</summary>
public sealed record PlanAim(int Plan, string? Problem, bool Delivers = false);

/// <summary>What the hands send this frame, the blueprint they delivered to if any, and the tick work was last sent on: -1 when none is under way.</summary>
public sealed record WorkUses(IReadOnlyList<Command> Commands, int? Served, int WorkedOnTick);

/// <summary>
/// The left button held on a plan with an empty hand (0022): the plan parts of the hero's aim and uses
/// (features/crew/aim.ts, <c>aimAt</c> and <c>usesOf</c>). Doors, valves, tanks and crates stay E's, in
/// the crew feature.
/// </summary>
public static class PlanWork
{
    /// <summary>How many ticks of work one frame sends at most, after a stall.</summary>
    private const int MaxWorkPerFrame = 5;

    /// <summary>
    /// The plan in <paramref name="cell"/> peer <paramref name="peer"/> works: of several, the oldest that
    /// can be worked, so one the hero cannot pay for does not block the mark beside it; when none can, the
    /// oldest, to show why; none in <paramref name="served"/>, delivered to in this hold already.
    /// </summary>
    public static PlanAim? AimAt(BuildingModule building, int peer, int cell, IReadOnlySet<int> served)
    {
        bool Delivers(int id) => building.Lacking(id).Count > 0;
        var here = building.PlansAt(cell)
            // A sweep delivers to each blueprint once; one with all it needs is built while held on.
            .Where(p => !(served.Contains(p.Id) && Delivers(p.Id)))
            .Select(p => new PlanAim(p.Id, building.WorkProblem(p.Id, peer), Delivers(p.Id)))
            .ToList();
        return here.FirstOrDefault(w => w.Problem == null) ?? here.FirstOrDefault();
    }

    /// <summary>
    /// What the left button, <paramref name="held"/>, sends on <paramref name="work"/> this frame. Held on
    /// a blueprint still lacking something within reach, it delivers once: the caller keeps the blueprints
    /// served in this hold and passes them to <see cref="AimAt"/>, so sweeping the pointer delivers to
    /// each it passes. Held on a plan with all it needs, or a mark, it sends one <see cref="Work"/> for
    /// every tick released since the last it sent for, so a peer drawing fewer frames than the ticks it
    /// runs builds as fast as any other; a hold just begun sends one. <paramref name="tick"/> is the
    /// client's world tick.
    /// </summary>
    public static WorkUses UsesOf(PlanAim? work, bool inReach, bool held, int tick, int workedOnTick)
    {
        if (!inReach || work == null || work.Problem != null || !held) return new([], null, -1);
        var send = new Work(work.Plan);
        if (work.Delivers) return new([send], work.Plan, -1);
        // A hold just begun sends one; so does a load that took the world's tick back.
        bool fresh = workedOnTick < 0 || tick < workedOnTick;
        int due = fresh ? 1 : Math.Min(tick - workedOnTick, MaxWorkPerFrame);
        return new(Enumerable.Range(0, due).Select(_ => (Command)new Work(work.Plan)).ToList(), null, due > 0 ? tick : workedOnTick);
    }
}
