using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// How far a hologram window has opened, frame by frame: a share that runs to 1 over <see cref="HoloLook.OpenS"/> while it is
/// open and back to 0 over <see cref="HoloLook.CloseS"/> once it is shut, so closing plays the opening back; where the pens and
/// the rest are at a share is <see cref="HoloPen.At"/>. The same machine opened again while it closes goes on from where it
/// is; another machine starts from nothing, the old hologram gone at once.
/// </summary>
public sealed class HoloMotion
{
    /// <summary>The machine shown, opening, open or closing; null once it has closed.</summary>
    public int? Machine { get; private set; }

    /// <summary>How far it has opened: 0 nothing drawn, 1 open.</summary>
    public double Progress { get; private set; }

    /// <summary>Whether it was open on the last step.</summary>
    public bool Opening { get; private set; }

    /// <summary>Whether any of it is drawn.</summary>
    public bool Shown => Progress > 0;

    /// <summary>Where the pens, the ground, the grid and the contents are now.</summary>
    public PenFrame Frame => HoloPen.At(Progress);

    /// <summary>One frame of <paramref name="seconds"/>, <paramref name="open"/> the machine whose window is open, or null.</summary>
    public void Step(int? open, double seconds)
    {
        if (open is int id && id != Machine) (Machine, Progress) = (id, 0);
        Opening = open != null;
        Progress = Math.Clamp(Progress + (Opening ? seconds / HoloLook.OpenS : -seconds / HoloLook.CloseS), 0, 1);
        if (!Opening && Progress <= 0) Machine = null;
    }

    /// <summary>Gone at once: another owner's machine opened, or a load or resync.</summary>
    public void Snap() => (Machine, Progress, Opening) = (null, 0, false);
}
