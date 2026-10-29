using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// When <c>--snap</c> saves the screen: <paramref name="seconds"/> after the first tick it is shown, not at that age of the world,
/// which may start late (the testbed is aired first) and would be saved on a first frame not yet drawn, all black.
/// </summary>
public sealed class SnapClock(int seconds)
{
    private int? _from;

    /// <summary>Whether the picture is due on <paramref name="tick"/>; the first call sets the start.</summary>
    public bool Due(int tick) => tick - (_from ??= tick) >= seconds * World.TicksPerSecond;
}
