using Spec = Stillwake.Content;

namespace Stillwake.Game.Shared;

/// <summary>
/// How far up its sight glass a tank shows its oxygen (<c>art/tank_fill_*.png</c>): frame 0 empty, 1 to
/// <see cref="Full"/> by fifths of <see cref="Spec.Air.TankCapacity"/>. No rule of its own: the sim says what it holds.
/// </summary>
public static class TankFill
{
    /// <summary>The frame of a full tank; there are this many and one more, empty.</summary>
    public const int Full = 5;

    /// <summary>
    /// The frame for <paramref name="oxygen"/>, rounded up: any oxygen shows at least frame 1, since a tank
    /// holding any feeds its pipes; only none at all shows empty.
    /// </summary>
    public static int Frame(int oxygen) =>
        oxygen == 0 ? 0 : (oxygen * Full + Spec.Air.TankCapacity - 1) / Spec.Air.TankCapacity;
}
