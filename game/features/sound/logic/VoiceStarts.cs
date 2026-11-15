using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Sound;

/// <summary>
/// Which voice of a placed player starts a sound. A voice is one sound at one place: a second sound on a voice still sounding
/// moves the first with it and sets its volume from there, so a far burst once silenced a near one still ringing. Godot also
/// starts a sound only on its next physics tick, and a second play before then replaces the first (heard-every-other). So a play
/// takes the first voice neither sounding nor started this tick; with none free, the one started longest ago, cut short.
/// </summary>
public sealed class VoiceStarts
{
    /// <summary>The physics tick each voice last started on, plus one; 0 for never.</summary>
    private ulong[] _started = [];

    /// <summary>The voice, 0 to <paramref name="sounding"/>'s count − 1, that starts a sound played in physics tick <paramref name="tick"/>, given which voices still sound.</summary>
    public int Take(ulong tick, IReadOnlyList<bool> sounding)
    {
        if (_started.Length < sounding.Count) Array.Resize(ref _started, sounding.Count);
        int pick = -1;
        for (int i = 0; i < sounding.Count && pick < 0; i++)
            if (!sounding[i] && _started[i] != tick + 1) pick = i;
        if (pick < 0)
        {
            pick = 0;
            for (int i = 1; i < sounding.Count; i++)
                if (_started[i] < _started[pick]) pick = i;
        }
        _started[pick] = tick + 1;
        return pick;
    }
}
