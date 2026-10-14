using Stillwake.Sim.Atmosphere;

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

/// <summary>What any module or the client may read of the heroes.</summary>
public interface ICrewQueries
{
    /// <summary>Where heroes wake: the map's first S, or cell 0 on a map without one.</summary>
    int Spawn { get; }
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

    public CrewModule(IAtmosphereOps atmosphere)
    {
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
                // One draw for the breath and the refill: what comes beyond the breath tops up the suit.
                int want = 1 + Math.Min(Content.Crew.Refill, Content.Crew.Suit - hero.Suit);
                int taken = atmosphere.Breathe(hero.Cell, want);
                if (taken > 0)
                {
                    hero.Suit += taken - 1;
                    continue;
                }
                hero.Suit -= 1;
                if (hero.Suit == 0) hero.Out = Content.Crew.PassOut;
            }
        });
    }

    protected override CrewState Init(Layout layout) =>
        new() { Spawn = layout.CellsOf('S').DefaultIfEmpty(0).First() };

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
