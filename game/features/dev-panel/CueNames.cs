using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Dev;

/// <summary>What the sound board calls a cue, which has no name of its own: its first file under <c>res://audio/</c>.</summary>
public static class CueNames
{
    private const string Audio = "res://audio/";

    /// <summary><c>res://audio/crew/step-1.wav</c> is <c>crew/step</c>.</summary>
    public static string Of(S.Cue cue)
    {
        string file = cue.Files[0];
        if (file.StartsWith(Audio, System.StringComparison.Ordinal)) file = file[Audio.Length..];
        int dot = file.LastIndexOf('.');
        if (dot > file.LastIndexOf('/')) file = file[..dot];
        return file.EndsWith("-1", System.StringComparison.Ordinal) ? file[..^2] : file;
    }
}
