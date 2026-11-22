using System;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Sound;

/// <summary>
/// When a track of the ship's music begins (ship-music): the first after <see cref="S.ShipFirstS"/> off a planet, then each after a
/// silence of <see cref="S.ShipGapLeastS"/> to <see cref="S.ShipGapMostS"/> once the last has ended, the tracks in turn. The
/// wait counts only off a planet (<see cref="HeardOn"/>); a track begun plays to its end, unheard on a planet. Plain .NET.
/// </summary>
public sealed class ShipMusic(Random rng, int tracks)
{
    private double _wait = S.ShipFirstS;
    private int _next = rng.Next(tracks);

    /// <summary>Whether the music is heard on a surface of <paramref name="kind"/>: space, the ship's and a vessel's alike, never a planet.</summary>
    public static bool HeardOn(Content.Surfaces.Kind kind) => kind.Floats;

    /// <summary>
    /// The track to begin now, if any, after <paramref name="seconds"/> more; <paramref name="offPlanet"/> whether this player's
    /// hero is off a planet, aboard or out in space, <paramref name="playing"/> whether a track is still playing.
    /// </summary>
    public int? Step(double seconds, bool offPlanet, bool playing)
    {
        if (playing || !offPlanet) return null;
        if ((_wait -= seconds) > 0) return null;
        int track = _next;
        _next = (_next + 1) % tracks;
        _wait = S.ShipGapLeastS + rng.NextDouble() * (S.ShipGapMostS - S.ShipGapLeastS);
        return track;
    }
}
