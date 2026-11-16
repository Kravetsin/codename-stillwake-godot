using System.Text.Json.Serialization;
using Stillwake.Sim.Knowledge;

namespace Stillwake.Sim.Sky;

/// <summary>The sky's slice. Public so modules can be typed by it, its members internal: others read it through <see cref="ISkyQueries"/>.</summary>
public sealed class SkyState
{
    /// <summary>Ticks since the start of the day, 0 to the cycle's length less one; 0 where the surface has no day.</summary>
    [JsonInclude] internal int Time { get; set; }

    /// <summary>The stretches opened, by id, in content order: each once, never closed again.</summary>
    [JsonIgnore] internal List<string> Opened { get; set; } = [];
    /// <summary>Opened as saved: none written while none is.</summary>
    [JsonInclude, JsonPropertyName("opened"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<string>? SavedOpened { get => Opened.Count > 0 ? Opened : null; set => Opened = value ?? []; }

    /// <summary>The stretches that have come at least once, by id, in content order: each within <see cref="Opened"/>.</summary>
    [JsonIgnore] internal List<string> Came { get; set; } = [];
    /// <summary>Came as saved: none written while none has.</summary>
    [JsonInclude, JsonPropertyName("came"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    internal List<string>? SavedCame { get => Came.Count > 0 ? Came : null; set => Came = value ?? []; }

    /// <summary>The stretch chosen to come next, or null for none.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal string? Next { get; set; }
    /// <summary>Ticks till <see cref="Next"/> starts, its last <c>Stretch.Warning</c> its warning; 0 with none chosen.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal int NextIn { get; set; }
    /// <summary>The stretch running, or null for none.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal string? Active { get; set; }
    /// <summary>Ticks of <see cref="Active"/> left; 0 with none running.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal int Left { get; set; }
    /// <summary>Ticks <see cref="Active"/> has run; 0 with none running, and in a save made before they were counted.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal int Ran { get; set; }

    /// <summary>The forks of the world's Rng the stretches have drawn here, each salted by the count before it; 0 in a save made before.</summary>
    [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] internal int Drawn { get; set; }
}

/// <summary>
/// The hour, from the dev panel: <paramref name="Time"/> ticks into the day, taken modulo its length (a negative one too).
/// Refused <c>sky.noDay</c> where the surface has no day: the ship.
/// </summary>
public sealed record SetTime(int Time) : Command;

/// <summary>
/// Stretch <paramref name="Id"/> comes now, from the dev panel: opened, chosen next in place of any chosen, its warning starting.
/// Refused <c>sky.noStretch</c> for one this surface's kind does not have, <c>sky.stretchRunning</c> while one runs.
/// </summary>
public sealed record StretchNow(string Id) : Command;

/// <summary>
/// The sky now: <paramref name="Time"/> ticks into the day, whether it is <paramref name="Night"/>, and the <paramref name="Chill"/>,
/// the degrees colder it makes the open sky (2026-11-14-temperature-in-degrees): <c>Cycle.Chill</c> at night and the running
/// stretch's. <paramref name="Stretch"/> is the stretch running, <paramref name="Coming"/> the one in its warning, with
/// <paramref name="ComingIn"/> ticks of it left, and the running one <paramref name="Ran"/> ticks on: when each began, so its far
/// picture is the same for every peer and after a load. Null and 0 for none. Time and night are 0 and off with no day.
/// <paramref name="Pace"/>, the running stretch's: thousandths of its speed a hero walks at under the open sky; whole with none.
/// </summary>
public readonly record struct SkyNow(int Time, bool Night, int Chill, string? Stretch, string? Coming, int ComingIn, int Ran = 0, int Pace = 1000);

/// <summary>What any module may read of the sky.</summary>
public interface ISkyQueries
{
    /// <summary>The sky on this surface now (<see cref="SkyNow"/>).</summary>
    SkyNow Now { get; }
}

/// <summary>
/// A planet's day and night (2026-11-07-night-chills): its hour counted a tick a tick, its night the end of each day. And the
/// stretches of its kind (docs/decisions/2026-11-15-stretches-of-space.md), the ship's or a planet's weather: opened, chosen, seen coming, run.
/// </summary>
public sealed partial class SkyModule : Module<SkyState>, ISkyQueries
{
    private readonly IKnowledgeQueries _knowledge;

    public override string Id => "sky";

    public SkyModule(IKnowledgeQueries knowledge)
    {
        _knowledge = knowledge;
        Handles<SetTime>(_ => Rules.Cycle == null ? "sky.noDay" : null, c => State.Time = Wrap(c.Time, Rules.Cycle!.Length));
        Handles<StretchNow>(StretchNowProblem, c => Bring(StretchOf(c.Id)));
        System(Phase.Construction, Schedule);
        System(Phase.Simulation, () =>
        {
            if (Rules.Cycle is { } cycle) State.Time = (State.Time + 1) % cycle.Length;
        });
    }

    public SkyNow Now
    {
        get
        {
            var s = State;
            var running = s.Active is { } active ? StretchOf(active) : null;
            int chill = running?.Chill ?? 0, pace = running?.Pace ?? 1000;
            string? coming = Coming ? s.Next : null;
            int comingIn = coming != null ? s.NextIn : 0;
            if (Rules.Cycle is not { } cycle) return new SkyNow(0, false, chill, s.Active, coming, comingIn, s.Ran, pace);
            // Modulo: a save's hour past a shorter day's end is read as the hour it wraps to.
            int time = s.Time % cycle.Length;
            bool night = time >= cycle.Length - cycle.Night;
            return new SkyNow(time, night, chill + (night ? cycle.Chill : 0), s.Active, coming, comingIn, s.Ran, pace);
        }
    }

    protected override SkyState Init(Layout layout) => new();

    private static int Wrap(int time, int length) => (time % length + length) % length;
}
