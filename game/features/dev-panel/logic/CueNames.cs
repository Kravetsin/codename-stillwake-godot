using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Dev;

/// <summary>
/// What the sound board calls a cue, which has no name of its own: its first file under <c>res://audio/</c>,
/// and its tune when it plays another cue's files lower or higher, as the landing plays the steps'.
/// </summary>
public static class CueNames
{
    private const string Audio = "res://audio/";

    /// <summary><c>res://audio/crew/step-1.wav</c> is <c>crew/step</c>; tuned to 0.85, <c>crew/step ×0.85</c>.</summary>
    public static string Of(S.Cue cue)
    {
        string file = cue.Files[0];
        if (file.StartsWith(Audio, System.StringComparison.Ordinal)) file = file[Audio.Length..];
        int dot = file.LastIndexOf('.');
        if (dot > file.LastIndexOf('/')) file = file[..dot];
        string name = file.EndsWith("-1", System.StringComparison.Ordinal) ? file[..^2] : file;
        return cue.Tune == 1 ? name : $"{name} ×{cue.Tune.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }
}
