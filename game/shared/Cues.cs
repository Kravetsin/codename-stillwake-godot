using Godot;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Shared;

/// <summary>
/// Players for <see cref="S"/>'s cues, made by each feature that plays them and kept as its own children,
/// never in a static: a scene reload frees them with the rest (ADR 0033).
/// </summary>
public static class Cues
{
    /// <summary>A loop's stream, or a one-shot's: a randomizer over its files, its pitch varied.</summary>
    public static AudioStream Of(S.Cue cue)
    {
        if (cue.Loops) return GD.Load<AudioStream>(cue.Files[0]);
        var shots = new AudioStreamRandomizer
        {
            RandomPitch = 1 + cue.Pitch,
            PlaybackMode = cue.Files.Count > 1
                ? AudioStreamRandomizer.PlaybackModeEnum.RandomNoRepeats
                : AudioStreamRandomizer.PlaybackModeEnum.Random,
        };
        foreach (string file in cue.Files) shots.AddStream(-1, GD.Load<AudioStream>(file));
        return shots;
    }

    /// <summary>A player of <paramref name="cue"/> heard the same anywhere, on <paramref name="bus"/>.</summary>
    public static AudioStreamPlayer Player(S.Cue cue, string bus, int voices = 1) =>
        new() { Stream = Of(cue), VolumeDb = cue.VolumeDb, PitchScale = cue.Tune, Bus = bus, MaxPolyphony = voices };

    /// <summary>A player of <paramref name="cue"/> heard from where it is placed, panned from the screen's middle and fading as its <see cref="S.Reach"/> says.</summary>
    public static Heard2D Player2D(S.Cue cue, string bus, int voices = 1) =>
        new Heard2D(cue.Heard ?? throw new System.ArgumentException("a cue placed in the world needs its reach", nameof(cue)), cue.VolumeDb)
        {
            Stream = Of(cue),
            PitchScale = cue.Tune,
            Bus = bus,
            MaxPolyphony = voices,
        };
}
