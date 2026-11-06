using System;

namespace Stillwake.Game.Menu;

/// <summary>
/// A bus's volume as Settings' sliders set it (settings-menu): a step from 0 to <see cref="Sound.VolumeSteps"/>, the top one
/// today's mix, each below it <see cref="Sound.VolumeStepDb"/> quieter, 0 silent.
/// </summary>
public static class Volume
{
    /// <summary>A kept step brought into range: a file edited by hand or from another build plays at the nearest step.</summary>
    public static int Step(int kept) => Math.Clamp(kept, 0, Sound.VolumeSteps);

    /// <summary>Whether <paramref name="step"/> silences its bus: a gain in dB cannot.</summary>
    public static bool Muted(int step) => Step(step) == 0;

    /// <summary>The bus's gain at <paramref name="step"/>; at 0 the bus is muted, its gain the quietest step's.</summary>
    public static float Db(int step) => (Math.Max(Step(step), 1) - Sound.VolumeSteps) * Sound.VolumeStepDb;
}
