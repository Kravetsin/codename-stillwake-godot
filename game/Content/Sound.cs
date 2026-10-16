using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game;

/// <summary>
/// What is heard (ADR 0033): each cue's files under <c>game/audio</c>, one-shots normalised to −3 dBFS
/// peak and loops to about −20 LUFS, and how loud each plays in the mix; the buses by name, the contract
/// settings will set volumes by; and how the world dulls round a hero out of air. Swapping a placeholder
/// for another file is a change here only.
/// </summary>
public static class Sound
{
    /// <summary>
    /// A cue: its files, any one of them each time a one-shot plays, its pitch varied by up to
    /// <see cref="Pitch"/> either way; a loop is one file, looped on import. <see cref="VolumeDb"/> is its
    /// place in the mix.
    /// </summary>
    public sealed record Cue(IReadOnlyList<string> Files, float VolumeDb, float Pitch = 0.06f, bool Loops = false);

    /// <summary>
    /// The buses (<c>game/default_bus_layout.tres</c>): UI, Ambient and Effects into Master; World (every
    /// hero's steps, machines, air: silenced out of air) and Suit (what this hero's hands touch and its
    /// suit's alarm: heard through the suit in vacuum too) into Effects.
    /// Settings will set Master, Effects, Ambient and UI; World and Suit are the hearing's.
    /// </summary>
    public const string Master = "Master", Ui = "UI", Ambient = "Ambient", Effects = "Effects", World = "World", Suit = "Suit";
    public static readonly IReadOnlyList<string> Buses = [Master, Ui, Ambient, Effects, World, Suit];

    public static readonly Cue Step = new(Shots("crew/step", 4), -12);
    public static readonly Cue Land = new(Shots("crew/land", 2), -8);
    public static readonly Cue Alarm = new(Shots("crew/alarm", 1), -14, Pitch: 0);
    public static readonly Cue Screwdriver = new(Shots("building/screwdriver", 3), -16);
    public static readonly Cue Hiss = Looped("atmosphere/hiss", -8);
    public static readonly Cue Click = new(Shots("ui/click", 2), -12);
    public static readonly Cue RoomTone = Looped("ambient/room-tone", -16);
    public static readonly Cue Use = new(Shots("hands/use", 2), -14);
    public static readonly Cue Catch = new(Shots("hands/catch", 3), -10);
    public static readonly Cue Pick = new(Shots("hands/pick", 2), -14);

    public static readonly IReadOnlyList<Cue> All = [Step, Land, Alarm, Screwdriver, Hiss, Click, RoomTone, Use, Catch, Pick];

    /// <summary>The suit's alarm beeps this often, in seconds, while it runs low out of air.</summary>
    public const double AlarmEveryS = 1.6;

    /// <summary>
    /// Out of air the world goes silent over <see cref="MuffleMs"/>: the World bus's low-pass falls from
    /// <see cref="ClearHz"/> to <see cref="MuffledHz"/> as the bus and the room tone fade out.
    /// </summary>
    public const float ClearHz = 20000, MuffledHz = 300, MuffleMs = 600;

    /// <summary>A sound in the world is heard up to this far from the middle of the screen, in screen pixels.</summary>
    public const float HearPx = 900;

    private static string[] Shots(string cue, int count) =>
        Enumerable.Range(1, count).Select(n => $"res://audio/{cue}-{n}.wav").ToArray();

    private static Cue Looped(string cue, float volumeDb) => new([$"res://audio/{cue}-1.ogg"], volumeDb, Loops: true);
}
