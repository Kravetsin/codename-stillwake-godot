using System;

namespace Stillwake.Game;

/// <summary>
/// How a running electric tool is seen to work (tool-contact): what it throws where it touches, a steady stream rather than a
/// burst a hit, <see cref="ThrowsPerSecond"/> throws a second of as much in all as its hits threw one by one; and the plasma
/// cutter's jet from its nozzle to where it works, or its whole reach. A placeholder look till the user picks one.
/// </summary>
public static class WorkLooks
{
    /// <summary>How many throws a second a touching tool makes, every one a share of a whole hit (<see cref="ShareOf"/>).</summary>
    public const double ThrowsPerSecond = 30;

    /// <summary>
    /// The share of the axe's whole hit each of <paramref name="tool"/>'s throws is: its hits' each (their pace over the axe's) times
    /// its hits a second, spread over <see cref="ThrowsPerSecond"/>; none for no weapon.
    /// </summary>
    public static double ShareOf(string tool)
    {
        if (Content.Weapons.Of(tool) is not { } weapon) return 0;
        double each = Math.Min(1, (double)weapon.Pace / Content.Weapons.Axe.Pace), perSecond = (double)Sim.World.TicksPerSecond / weapon.Pace;
        return each * perSecond / ThrowsPerSecond;
    }

    /// <summary>The cutter's jet: its glow and its core, their widths in pixels, and how much its light flickers frame to frame.</summary>
    public static readonly (float R, float G, float B, float A) JetGlow = (0.55f, 0.35f, 1f, 0.35f), JetCore = (0.92f, 0.88f, 1f, 0.9f);
    public const float JetGlowWidth = 3.5f, JetCoreWidth = 1f, JetFlicker = 0.25f;
}
