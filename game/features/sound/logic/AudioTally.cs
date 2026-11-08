using System;
using System.Collections.Generic;
using System.Linq;

namespace Stillwake.Game.Features.Sound;

/// <summary>
/// A count of the sound players in the tree, for the log (heard-every-other): how many there are, how many play, and which
/// play most, by where they hang and what they play. A leak shows as a count growing from one surface to the next.
/// </summary>
public sealed class AudioTally
{
    /// <summary>How many of the commonest playing kinds a line names.</summary>
    public const int Named = 8;

    private readonly Dictionary<string, int> _playing = new(StringComparer.Ordinal);
    private int _players, _sounding;

    /// <summary>One player, of kind <paramref name="key"/>, playing or not.</summary>
    public void Add(string key, bool playing)
    {
        _players++;
        if (!playing) return;
        _sounding++;
        _playing[key] = _playing.GetValueOrDefault(key) + 1;
    }

    /// <summary>The count as one line: players, playing, and the commonest playing kinds.</summary>
    public string Line()
    {
        var most = _playing.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).Take(Named)
            .Select(p => p.Value > 1 ? $"{p.Key}×{p.Value}" : p.Key);
        return $"players {_players}, playing {_sounding}" + (_sounding > 0 ? ": " + string.Join(", ", most) : "");
    }
}
