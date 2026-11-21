namespace Stillwake.Content;

/// <summary>Where a lesson is learned: a deed done often enough (<see cref="Deed"/>), or a machine passed (<see cref="Near"/>).</summary>
public abstract record LessonSource;

/// <summary>A deed done <paramref name="Count"/> times by one player, counted by <c>Knowledge</c>: <paramref name="Id"/> names it.</summary>
public sealed record Deed(string Id, int Count) : LessonSource;

/// <summary>A machine of kind <paramref name="Machine"/> passed within <see cref="Lessons.Reach"/> by a hero with a drone: the drone does the looking.</summary>
public sealed record Near(string Machine) : LessonSource;

/// <summary>A form of the light drone and where it is learned (drone-lessons): each player's own, for good.</summary>
public sealed record Lesson(string Form, LessonSource Source);

/// <summary>
/// How the light drone learns its forms (drone-lessons): by watching its player, each player's drone its own. A new source is a row of
/// <see cref="All"/>, not code per form.
/// </summary>
public static class Lessons
{
    /// <summary>The deed of breaking a wreck's piece, a delivery's crate too: counted for whoever broke it.</summary>
    public static readonly string WreckBroken = "wreckBroken";

    /// <summary>
    /// How near a hero's reported cell must come to a teaching machine's footprint, cells, as the crow flies: about what the screen shows
    /// round the hero at the usual zoom.
    /// </summary>
    public const int Reach = 24;

    /// <summary>Every lesson: the scan from ten pieces broken, the lamp from a lamp a player built, the alarms from the wreck's emergency lamps.</summary>
    public static readonly IReadOnlyList<Lesson> All =
    [
        new(LightDrone.ScannerMode, new Deed(WreckBroken, 10)),
        new(LightDrone.LampMode, new Near("lamp")),
        new(LightDrone.AlarmsMode, new Near("emergencyLamp")),
    ];

    /// <summary>The forms in <see cref="All"/>'s order: what the board's companion district holds.</summary>
    public static readonly IReadOnlyList<string> Forms = [.. All.Select(l => l.Form)];
}
