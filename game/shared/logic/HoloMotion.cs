using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// How far a machine window's hologram has come, frame by frame (machine-holograms). Opened, the beam reaches the window
/// in <see cref="HoloLook.BeamS"/>, then the window grows over <see cref="HoloLook.GrowS"/>, its contents rising with it.
/// Closed, the contents go at once, the window shrinks over <see cref="HoloLook.ShrinkS"/>, then the beam pulls back over
/// <see cref="HoloLook.RetractS"/>. The same machine opened again while it closes goes on from where it is; another machine
/// starts from nothing, the old hologram gone at once.
/// </summary>
public sealed class HoloMotion
{
    /// <summary>The machine shown, opening, open or closing; null once the beam is back.</summary>
    public int? Machine { get; private set; }

    /// <summary>How far the beam reaches from the machine to the window: 0 none, 1 touching.</summary>
    public double Beam { get; private set; }

    /// <summary>How far the window has grown from where the beam touches: 0 none, 1 whole.</summary>
    public double Window { get; private set; }

    /// <summary>Whether it was open on the last step.</summary>
    public bool Opening { get; private set; }

    /// <summary>The contents' alpha: rising with the window while it opens, none while it closes.</summary>
    public double Contents => Opening ? Window : 0;

    /// <summary>Whether any of it is drawn.</summary>
    public bool Shown => Beam > 0;

    /// <summary>One frame of <paramref name="seconds"/>, <paramref name="open"/> the machine whose window is open, or null.</summary>
    public void Step(int? open, double seconds)
    {
        if (open is int id && id != Machine) (Machine, Beam, Window) = (id, 0, 0);
        Opening = open != null;
        if (Opening)
        {
            (Beam, seconds) = Toward(Beam, 1, seconds, HoloLook.BeamS);
            if (Beam >= 1) (Window, _) = Toward(Window, 1, seconds, HoloLook.GrowS);
            return;
        }
        (Window, seconds) = Toward(Window, 0, seconds, HoloLook.ShrinkS);
        if (Window <= 0) (Beam, _) = Toward(Beam, 0, seconds, HoloLook.RetractS);
        if (Beam <= 0) Machine = null;
    }

    /// <summary>Gone at once: another owner's machine opened, or a load or resync.</summary>
    public void Snap() => (Machine, Beam, Window, Opening) = (null, 0, 0, false);

    /// <summary><paramref name="from"/> moved toward <paramref name="to"/> at a whole in <paramref name="whole"/> seconds, and the seconds left once there.</summary>
    private static (double At, double Left) Toward(double from, double to, double seconds, double whole)
    {
        double need = Math.Abs(to - from) * whole;
        if (seconds >= need) return (to, seconds - need);
        return (from + Math.Sign(to - from) * seconds / whole, 0);
    }
}
