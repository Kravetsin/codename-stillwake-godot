using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Crew;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/testing/air.ts, and the slices TypeScript's tests read directly.</summary>
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

    /// <summary>The crew's slice, as TypeScript's tests read <c>w.sim.world.slices.crew</c>.</summary>
    internal static CrewState Crew(this Scenario w) => (CrewState)w.M.Crew.Slice;
}
