using System.Linq;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Dev;

/// <summary>
/// What the sound board calls a cue, which has no name of its own: its first file under <c>res://audio/</c>,
/// and its tune when it plays another cue's files lower or higher, as the landing plays the steps'; its volume when it plays them
/// at their tune but quieter, as the salvage drone plays the jetpack's.
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
        if (cue.Tune != 1) return $"{name} ×{cue.Tune.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        bool quieter = S.All.Any(o => o.Files[0] == cue.Files[0] && o.Tune == cue.Tune && o.VolumeDb > cue.VolumeDb);
        return quieter ? $"{name} {cue.VolumeDb.ToString(System.Globalization.CultureInfo.InvariantCulture)} dB" : name;
    }
}
