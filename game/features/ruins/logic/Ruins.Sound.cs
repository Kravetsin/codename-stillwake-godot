namespace Stillwake.Game;

public static partial class Sound
{
    /// <summary>
    /// The arc makers' music box hit (0091): a plucked steel tine, synthesised, played at its tune's pitches (<c>HomesLooks.Tune</c>),
    /// so no pitch of its own. A placeholder till the user picks.
    /// </summary>
    public static readonly Cue MusicBox = new(Shots("ruins/music-box", 1), -12, Pitch: 0, Heard: new(8, 32));

    /// <summary>The resonator zapping a hero beside it (0091): a short crackle of noise, synthesised. A placeholder till the user picks.</summary>
    public static readonly Cue Zap = new(Shots("ruins/zap", 1), -10, Pitch: 0.08f, Heard: new(8, 32));

    /// <summary>
    /// A relic humming in its cradle (0096): the charging station's synthesised hum played half again as high, heard round its
    /// chamber; a placeholder till the user picks, as its discharges are the resonator's <see cref="Zap"/>.
    /// </summary>
    public static readonly Cue RelicHum = new(["res://audio/power/charger-hum-1.ogg"], -14, Pitch: 0, Loops: true, Heard: new(6, 28), Tune: 1.5f);
}
