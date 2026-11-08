using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// When the harpoon's two flourishes of light play (0075): a bead running down the line as it goes taut, and once a reel is
/// over a streak zipping home to the gun. Each runs out on the gun in hand, the rod seated or not, and neither outlives
/// it: left waiting, a streak would play out on the next shot, from where the last reel ended.
/// </summary>
public sealed class RodFlourish
{
    /// <summary>How long the bead takes down the line, and the zip home, in 60 Hz frames.</summary>
    private const double BeadFrames = 10, ZipFrames = 8;

    private Rod _was;
    private double _beadMs = -1, _zipMs = -1;

    /// <summary>What to draw this frame: how far along the bead and the zip are, 0 to 1, none for none.</summary>
    /// <param name="ZipBegan">The zip began this frame, from where the rod is now.</param>
    /// <param name="ZipHome">The zip reached the gun this frame.</param>
    public readonly record struct Shown(double? Bead, double? Zip, bool ZipBegan, bool ZipHome);

    /// <summary>Moves on by <paramref name="elapsedMs"/>, the rod as <paramref name="rod"/> says, with a gun in hand or not.</summary>
    public Shown Step(Rod rod, bool gun, double elapsedMs)
    {
        if (_was == Rod.Hooked && rod is Rod.Reeling or Rod.Pulling) _beadMs = 0;
        bool began = _was == Rod.Reeling && rod == Rod.Returning;
        if (began) _zipMs = 0;
        _was = rod;
        if (!gun) (_beadMs, _zipMs) = (-1, -1);
        // The line it runs down is gone with the rod in the gun.
        if (rod == Rod.Seated) _beadMs = -1;
        double? bead = Along(ref _beadMs, BeadFrames, elapsedMs, out _);
        double? zip = Along(ref _zipMs, ZipFrames, elapsedMs, out bool home);
        return new Shown(bead, zip, began, home);
    }

    private static double? Along(ref double ms, double frames, double elapsedMs, out bool ended)
    {
        ended = false;
        if (ms < 0) return null;
        double q = ms / Glim.SampleMs / frames;
        ms += elapsedMs;
        if (q < 1) return q;
        (ms, ended) = (-1, true);
        return null;
    }
}
