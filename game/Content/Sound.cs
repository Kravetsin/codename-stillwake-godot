using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game;

/// <summary>
/// What is heard (ADR 0033): each cue's files under <c>game/audio</c>, one-shots normalised to −3 dBFS
/// peak (the chop's and the break's variants then matched by loudness, the louder turned down) and loops
/// to about −20 LUFS, and how loud each plays in the mix; the buses by name, the contract settings will set
/// volumes by; and how the world falls silent round a hero floating in space. Swapping a placeholder
/// for another file is a change here only.
/// </summary>
public static class Sound
{
    /// <summary>
    /// A cue: its files, any one of them each time a one-shot plays, its pitch varied by up to
    /// <see cref="Pitch"/> either way; a loop is one file, looped on import. <see cref="VolumeDb"/> is its
    /// place in the mix. Placed in the world, it is heard at full volume within <see cref="Reach"/>'s
    /// <c>Full</c> cells of the listener, fading to silence at its <c>Silent</c> cells.
    /// </summary>
    public sealed record Cue(IReadOnlyList<string> Files, float VolumeDb, float Pitch = 0.06f, bool Loops = false, Reach? Heard = null);

    /// <summary>How far a sound in the world carries, in cells: full volume up to <paramref name="Full"/>, none from <paramref name="Silent"/>.</summary>
    public sealed record Reach(float Full, float Silent)
    {
        /// <summary>The share of full volume heard <paramref name="cells"/> away: fading as the square of what is left of the way to silence.</summary>
        public float Share(float cells)
        {
            if (cells <= Full) return 1;
            float left = 1 - System.Math.Min(1, (cells - Full) / (Silent - Full));
            return left * left;
        }
    }

    /// <summary>
    /// The buses (<c>game/default_bus_layout.tres</c>): UI, Ambient and Effects into Master; World (every
    /// hero's steps, machines, air, doors: silenced while floating) and Suit (the action click, the
    /// suit's alarm, the axe's chop and a wreck breaking: heard in space too) into Effects.
    /// Settings will set Master, Effects, Ambient and UI; World and Suit are the hearing's. Ambient waits
    /// for the first ambience: the ship's hum comes from its vents and fires, in the world.
    /// </summary>
    public const string Master = "Master", Ui = "UI", Ambient = "Ambient", Effects = "Effects", World = "World", Suit = "Suit", Music = "Music";
    public static readonly IReadOnlyList<string> Buses = [Master, Ui, Ambient, Effects, World, Suit, Music];

    public static readonly Cue Step = new(Shots("crew/step", 4), -12, Heard: new(4, 18));
    public static readonly Cue Land = new(Shots("crew/land", 2), -8, Heard: new(4, 18));
    public static readonly Cue Alarm = new(Shots("crew/alarm", 1), -14, Pitch: 0);
    public static readonly Cue Screwdriver = new(Shots("building/screwdriver", 5), -16, Heard: new(4, 14));
    public static readonly Cue Hiss = Looped("atmosphere/hiss", -8, new(6, 24));
    public static readonly Cue Click = new(Shots("ui/click", 2), -12);
    public static readonly Cue VentHum = Looped("atmosphere/vent-hum", -12, new(3, 10));
    public static readonly Cue DoorOpen = new(Shots("grid/door-open", 1), -8, Heard: new(6, 24));
    public static readonly Cue DoorClose = new(Shots("grid/door-close", 1), -8, Heard: new(6, 24));
    public static readonly Cue Valve = new(Shots("grid/valve", 3), -12, Heard: new(4, 14));
    /// <summary>The one soft click an action makes: a crate opened, a canister loaded, debris caught, an item picked up.</summary>
    public static readonly Cue Act = new(Shots("hands/act", 1), -10);
    public static readonly Cue Fire = Looped("furnace/fire", -10, new(3, 12));
    /// <summary>
    /// An axe biting into a wreck (0034), heard where the wreck is by every hero near: on the Suit bus,
    /// since a wreck is only ever chopped out in space, where the World bus is silent.
    /// </summary>
    public static readonly Cue Chop = new(Shots("debris/chop", 3), -2, Pitch: 0.1f, Heard: new(5, 20));
    /// <summary>A wreck breaking apart under the last hit, in place of its chop: on the Suit bus as the chop is, and heard farther.</summary>
    public static readonly Cue Break = new(Shots("debris/break", 3), -2, Heard: new(6, 28));

    /// <summary>The title's music (0035), on the Music bus, faded in with the title and out on leaving it.</summary>
    public static readonly Cue Title = new(["res://audio/music/title-1.ogg"], -2, Loops: true);

    /// <summary>How long the title's music takes to fade in or out, in seconds.</summary>
    public const double MusicFadeS = 2.5;

    public static readonly IReadOnlyList<Cue> All = [Title, Step, Land, Alarm, Screwdriver, Hiss, Click, VentHum, DoorOpen, DoorClose, Valve, Act, Fire, Chop, Break];

    /// <summary>The suit's alarm beeps this often, in seconds, while it runs low out of air.</summary>
    public const double AlarmEveryS = 1.6;

    /// <summary>
    /// Floating in space the world goes silent over <see cref="MuffleMs"/>: the World bus's low-pass falls from
    /// <see cref="ClearHz"/> to <see cref="MuffledHz"/> as the bus fades out.
    /// </summary>
    public const float ClearHz = 20000, MuffledHz = 300, MuffleMs = 600;

    private static string[] Shots(string cue, int count) =>
        Enumerable.Range(1, count).Select(n => $"res://audio/{cue}-{n}.wav").ToArray();

    private static Cue Looped(string cue, float volumeDb, Reach heard) => new([$"res://audio/{cue}-1.ogg"], volumeDb, Loops: true, Heard: heard);
}
