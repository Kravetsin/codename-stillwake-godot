using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Knowledge;
using Stillwake.Sim.Players;

namespace Stillwake.Sim.Lessons;

/// <summary>The lessons' slice: nothing, what is learned is Knowledge's. Public so modules can be typed by it.</summary>
public sealed class LessonsState;

/// <summary>What any module or the client may read of the lessons.</summary>
public interface ILessonsQueries
{
    /// <summary>
    /// The machine of <paramref name="kind"/> nearest <paramref name="cell"/> within <paramref name="reach"/> cells of its footprint;
    /// null for none: what a near lesson is learned from and where the drone flies to look, within <see cref="Content.Lessons.Reach"/>;
    /// what it is curious about, within <see cref="Content.Lessons.Wonder"/> (drone-thoughts).
    /// </summary>
    Machine? Teacher(int cell, string kind, int reach = Content.Lessons.Reach);
}

/// <summary>
/// The light drone learning by looking (drone-lessons): each tick, a hero with a drone within <see cref="Content.Lessons.Reach"/> of a
/// machine a near lesson names learns its form, for good, as Knowledge keeps it. Rules only, on every surface: no commands, nothing saved.
/// Deed lessons are counted where the deed is done (Knowledge's <c>Did</c>).
/// </summary>
public sealed class LessonsModule : Module<LessonsState>, ILessonsQueries
{
    /// <summary>The lessons learned by passing a machine, in table order: the form and the machine's kind.</summary>
    private static readonly (string Form, string Machine)[] Near =
        [.. Content.Lessons.All.Select(l => l.Source is Content.Near near ? (l.Form, near.Machine) : default).Where(n => n.Form != null)];

    private readonly IGridQueries _grid;
    private readonly ICrewQueries _crew;
    private readonly IPlayersQueries _players;
    private readonly IKnowledgeQueries _knowledge;
    private readonly IKnowledgeOps _knowledgeOps;

    public override string Id => "lessons";

    public LessonsModule(IGridQueries grid, ICrewQueries crew, IPlayersQueries players, IKnowledgeQueries knowledge, IKnowledgeOps knowledgeOps)
    {
        (_grid, _crew, _players, _knowledge, _knowledgeOps) = (grid, crew, players, knowledge, knowledgeOps);
        System(Phase.Derivation, Teach);
    }

    protected override LessonsState Init(Layout layout) => new();

    /// <summary>Every hero with a drone, in hero order, learns each near form it does not know whose machine is within reach.</summary>
    private void Teach()
    {
        foreach (var hero in _crew.Heroes)
        {
            if (!_players.HasDrone(hero.Id)) continue;
            foreach (var (form, machine) in Near)
                if (!_knowledge.Knows(hero.Id, form) && Teacher(hero.Cell, machine) != null) _knowledgeOps.Grant(hero.Id, form);
        }
    }

    public Machine? Teacher(int cell, string kind, int reach = Content.Lessons.Reach)
    {
        if (!Machines.TryGet(kind, out var spec)) return null;
        int w = _grid.Width, x = cell % w, y = cell / w;
        Machine? best = null;
        long bestD = (long)reach * reach + 1;
        foreach (var m in _grid.MachinesOf(kind))
        {
            int left = m.Cell % w, top = m.Cell / w;
            long dx = Math.Max(0, Math.Max(left - x, x - (left + spec.Width - 1))), dy = Math.Max(0, Math.Max(top - y, y - (top + spec.Height - 1)));
            if (dx * dx + dy * dy < bestD) (best, bestD) = (m, dx * dx + dy * dy);
        }
        return best;
    }
}
