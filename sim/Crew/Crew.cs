using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Grid;

namespace Stillwake.Sim.Crew;

/// <summary>
/// The sender's hero is at <c>Cell</c>: sent when the hero enters another room as its client sees it,
/// and first of all when it wakes, which brings it in with a full suit.
/// </summary>
public sealed record Enter(int Cell) : Command;

/// <summary>
/// The sender's peer has left the session: its hero goes, and its colour with it. The host sends it on
/// the peer's behalf, hearing it go.
/// </summary>
public sealed record Leave : Command;

/// <summary>The sender's hero puts on suit colour <c>Colour</c>, if no other hero wears it.</summary>
public sealed record PickColour(int Colour) : Command;

/// <summary>The sender's hero starts running or swimming fast (<c>On</c>), or stops: sent when it changes.</summary>
public sealed record Exert(bool On) : Command;

/// <summary>What any module or the client may read of the heroes.</summary>
public interface ICrewQueries
{
    /// <summary>Where heroes are made: the lifepod's hatch, else the map's first S, or cell 0 on a map with neither.</summary>
    int Spawn { get; }
    /// <summary>Whether a hero in <paramref name="cell"/> draws the lifepod's air into its suit: within <c>Crew.PodReach</c> of it.</summary>
    bool ByPod(int cell);
    /// <summary>Hero <paramref name="id"/>, played by that peer; null while the sim has none (TypeScript's <c>hero</c>).</summary>
    Hero? HeroOf(int id);
    /// <summary>Every hero, in id order: live and read-only, for counting who breathes where.</summary>
    IReadOnlyList<Hero> Heroes { get; }
}

/// <summary>
/// Heroes inside the sim: where each one is, as its client says, and how it breathes — room air in a
/// cell holding it, the suit anywhere else, passing out when the suit runs dry
/// (packages/sim/src/modules/crew).
/// </summary>
public sealed partial class CrewModule : Module<CrewState>, ICrewQueries
{
    public override string Id => "crew";

    private readonly IGridQueries _grid;

    public CrewModule(IAtmosphereOps atmosphere, IGridQueries grid)
    {
        _grid = grid;
        Handles<Enter>(_ => null, c =>
        {
            var hero = HeroOf(c.From);
            if (hero != null)
            {
                hero.Cell = c.Cell;
                return;
            }
            State.Heroes.Add(new Hero { Id = c.From, Cell = c.Cell, Suit = Content.Crew.Suit, Colour = FreeColour(State.Heroes) });
            State.Heroes.Sort((a, b) => a.Id.CompareTo(b.Id));
        });
        Handles<Leave>(
            c => HeroOf(c.From) == null ? "crew.noHero" : null,
            c => State.Heroes.RemoveAll(h => h.Id == c.From));
        Handles<PickColour>(c => ColourProblem(c.From, c.Colour), c => HeroOf(c.From)!.Colour = c.Colour);
        Handles<Exert>(c => HeroOf(c.From) == null ? "crew.noHero" : null, c => HeroOf(c.From)!.Exerting = c.On);
        // After atmosphere in the simulation phase, so a breath reads this tick's air.
        System(Phase.Simulation, () =>
        {
            State.Clock += 1;
            bool breath = State.Clock % Content.Crew.BreathEvery == 0;
            foreach (var hero in State.Heroes)
            {
                if (hero.Out > 0)
                {
                    hero.Out -= 1;
                    if (hero.Out == 0)
                    {
                        hero.Cell = State.Spawn;
                        hero.Suit = Content.Crew.Suit;
                    }
                    continue;
                }
                if (!breath) continue;
                // The pod's air first, for the breath and the suit alike; then one draw from the room for
                // what is left of both, the room's refill as before. What the air falls short of the breath the suit pays.
                int breathes = hero.Exerting ? Content.Crew.Exerted : 1;
                int lack = Content.Crew.Suit - hero.Suit;
                int pod = ByPod(hero.Cell) ? Math.Min(Content.Crew.PodRefill, breathes + lack) : 0;
                int want = Math.Min(breathes + Math.Min(Content.Crew.Refill, lack), breathes + lack - pod);
                int taken = want > 0 ? atmosphere.Breathe(hero.Cell, want) : 0;
                hero.Suit = Math.Max(0, hero.Suit + pod + taken - breathes);
                if (hero.Suit == 0 && pod + taken < breathes)
                {
                    hero.Out = Content.Crew.PassOut;
                    hero.Exerting = false;
                }
            }
        });
    }

    /// <summary>The map's first lifepod makes heroes at its hatch; a map without one wakes them at its first S.</summary>
    protected override CrewState Init(Layout layout)
    {
        var pods = layout.Machines().Where(m => m.Kind == Pod).Select(m => m.Cell).ToList();
        if (pods.Count == 0) return new() { Spawn = layout.CellsOf('S').DefaultIfEmpty(0).First() };
        var (x, y) = Content.Crew.PodHatch;
        return new() { Pod = pods[0], Spawn = pods[0] + y * layout.Width + x };
    }

    private const string Pod = "lifepod";

    /// <summary>Whether <paramref name="cell"/> is within <c>Crew.PodReach</c> cells of every side of the pod, either way.</summary>
    public bool ByPod(int cell)
    {
        if (State.Pod is not int pod || cell < 0) return false;
        var spec = Content.Machines.Get(Pod);
        int width = _grid.Width;
        int x = cell % width, y = cell / width, left = pod % width, top = pod / width;
        int dx = Math.Max(0, Math.Max(left - x, x - (left + spec.Width - 1)));
        int dy = Math.Max(0, Math.Max(top - y, y - (top + spec.Height - 1)));
        return Math.Max(dx, dy) <= Content.Crew.PodReach;
    }

    /// <summary>
    /// The colour a hero coming in wears: the first no hero wears, or with every one worn, the one worn
    /// least, the first of those on a tie.
    /// </summary>
    private static int FreeColour(List<Hero> heroes)
    {
        var worn = new int[Content.Crew.Colours];
        foreach (var h in heroes) worn[h.Colour] += 1;
        return Array.IndexOf(worn, worn.Min());
    }

    private string? ColourProblem(int peer, int colour)
    {
        if (HeroOf(peer) == null) return "crew.noHero";
        if (colour < 0 || colour >= Content.Crew.Colours) return "crew.noSuchColour";
        if (State.Heroes.Exists(h => h.Id != peer && h.Colour == colour)) return "crew.colourTaken";
        return null;
    }

    // Queries.

    public int Spawn => State.Spawn;
    public Hero? HeroOf(int id) => State.Heroes.Find(h => h.Id == id);
    public IReadOnlyList<Hero> Heroes => State.Heroes;
}
