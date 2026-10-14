using System.Text.Json.Serialization;
using Stillwake.Sim.Atmosphere;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/testing/air.ts, and a stand-in for crew's breathing until crew is ported.</summary>
public static class AirTesting
{
    /// <summary>
    /// What a room leaking <paramref name="leak"/> every <c>Air.LeakEvery</c> ticks uses on the air ticks
    /// after clock <paramref name="from"/>, up to <paramref name="to"/>: the leak spread over the period,
    /// what is due by the end of a tick less what was by its start.
    /// </summary>
    public static int Leaked(int leak, int from, int to)
    {
        long sum = 0;
        for (long clock = from + 1; clock <= to; clock++)
        {
            long beat = clock % Content.Air.LeakEvery;
            sum += leak * (beat + 1) / Content.Air.LeakEvery - leak * beat / Content.Air.LeakEvery;
        }
        return (int)sum;
    }

    /// <summary>The atmosphere's slice, to read what TypeScript's tests read of <c>w.sim.world.slices.atmosphere</c>.</summary>
    internal static AtmosphereState Air(this Scenario w) => (AtmosphereState)w.M.Atmosphere.Slice;
}

/// <summary>The sender's hero is at <c>Cell</c>: crew's <c>enter</c>, for the breather.</summary>
public sealed record Enter(int Cell) : Command;

/// <summary>
/// Crew's breathing (packages/sim/src/modules/crew), as far as atmosphere's tests lean on it, until crew
/// is ported: each hero, every <c>Crew.BreathEvery</c> ticks, breathes one unit and tops up its suit by up
/// to <c>Crew.Refill</c> from the air of the cell it stands in, else from its suit; an empty suit passes
/// it out for <c>Crew.PassOut</c> ticks, and it wakes at cell 0 with a full one.
/// </summary>
public sealed class Breather : Module<Breather.Heroes>
{
    public sealed class Hero
    {
        public int Id { get; set; }
        public int Cell { get; set; }
        public int Suit { get; set; }
        public int Out { get; set; }
    }

    public sealed class Heroes
    {
        [JsonInclude] internal int Clock { get; set; }
        [JsonInclude] internal List<Hero> All { get; set; } = [];
    }

    public override string Id => "breather";

    public Breather(IAtmosphereOps atmosphere)
    {
        Handles<Enter>(_ => null, c =>
        {
            var hero = State.All.Find(h => h.Id == c.From);
            if (hero != null) { hero.Cell = c.Cell; return; }
            State.All.Add(new Hero { Id = c.From, Cell = c.Cell, Suit = Content.Crew.Suit });
            State.All.Sort((a, b) => a.Id.CompareTo(b.Id));
        });
        System(Phase.Simulation, () =>
        {
            State.Clock += 1;
            bool breath = State.Clock % Content.Crew.BreathEvery == 0;
            foreach (var hero in State.All)
            {
                if (hero.Out > 0)
                {
                    hero.Out -= 1;
                    if (hero.Out == 0) { hero.Cell = 0; hero.Suit = Content.Crew.Suit; }
                    continue;
                }
                if (!breath) continue;
                int taken = atmosphere.Breathe(hero.Cell, 1 + Math.Min(Content.Crew.Refill, Content.Crew.Suit - hero.Suit));
                if (taken > 0) { hero.Suit += taken - 1; continue; }
                hero.Suit -= 1;
                if (hero.Suit == 0) hero.Out = Content.Crew.PassOut;
            }
        });
    }

    protected override Heroes Init(Layout layout) => new();
}
