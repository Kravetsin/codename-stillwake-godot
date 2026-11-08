namespace Stillwake.Game.Features.Sound;

/// <summary>
/// Which voice of a placed player starts a sound. Godot starts an <c>AudioStreamPlayer2D</c>'s sound only on its next physics
/// tick (20 a second here), and a second play before then replaces the first: so the first play in a tick takes the player
/// itself (voice 0), each further one a spare voice (1, 2, …) up to its <c>voices</c>, and past them voice 0 again, the latest
/// replacing the one waiting there (heard-every-other).
/// </summary>
public struct VoiceStarts
{
    private ulong? _tick;
    private int _taken;

    /// <summary>The voice, 0 to <paramref name="voices"/> − 1, that starts a sound played in physics tick <paramref name="tick"/>.</summary>
    public int Take(ulong tick, int voices)
    {
        if (_tick != tick) (_tick, _taken) = (tick, 0);
        return _taken < voices ? _taken++ : 0;
    }
}
