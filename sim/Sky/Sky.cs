using System.Text.Json.Serialization;

namespace Stillwake.Sim.Sky;

/// <summary>The sky's slice. Public so modules can be typed by it, its members internal: others read it through <see cref="ISkyQueries"/>.</summary>
public sealed class SkyState
{
    /// <summary>Ticks since the start of the day, 0 to the cycle's length less one; 0 where the surface has no day.</summary>
    [JsonInclude] internal int Time { get; set; }
}

/// <summary>
/// The hour, from the dev panel: <paramref name="Time"/> ticks into the day, taken modulo its length (a negative one too).
/// Refused <c>sky.noDay</c> where the surface has no day: the ship.
/// </summary>
public sealed record SetTime(int Time) : Command;

/// <summary>
/// The sky now: <paramref name="Time"/> ticks into the day, whether it is <paramref name="Night"/>, and the
/// <paramref name="Chill"/> a beat of it lays on a hero under the open sky: <c>Cycle.Chill</c> at night, else 0. All 0 with no day.
/// </summary>
public readonly record struct SkyNow(int Time, bool Night, int Chill);

/// <summary>What any module may read of the sky.</summary>
public interface ISkyQueries
{
    /// <summary>The sky on this surface now (<see cref="SkyNow"/>).</summary>
    SkyNow Now { get; }
}

/// <summary>A planet's day and night (2026-11-07-night-chills): its hour counted a tick a tick, its night the end of each day.</summary>
public sealed partial class SkyModule : Module<SkyState>, ISkyQueries
{
    public override string Id => "sky";

    public SkyModule()
    {
        Handles<SetTime>(_ => Rules.Cycle == null ? "sky.noDay" : null, c => State.Time = Wrap(c.Time, Rules.Cycle!.Length));
        System(Phase.Simulation, () =>
        {
            if (Rules.Cycle is { } cycle) State.Time = (State.Time + 1) % cycle.Length;
        });
    }

    public SkyNow Now
    {
        get
        {
            if (Rules.Cycle is not { } cycle) return default;
            // Modulo: a save's hour past a shorter day's end is read as the hour it wraps to.
            int time = State.Time % cycle.Length;
            bool night = time >= cycle.Length - cycle.Night;
            return new SkyNow(time, night, night ? cycle.Chill : 0);
        }
    }

    protected override SkyState Init(Layout layout) => new();

    private static int Wrap(int time, int length) => (time % length + length) % length;
}
