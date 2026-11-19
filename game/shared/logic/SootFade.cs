using System;

namespace Stillwake.Game.Shared;

/// <summary>
/// How far a drone window has come out (drone-frame, the pick «Угли»): a share that runs to 1 over <see cref="SootRun.Tune.OpenS"/>
/// while it is open and back to 0 over <see cref="SootRun.Tune.CloseS"/>, and the glass, contents and clicks it shows by then.
/// Stepped by each window. Client only.
/// </summary>
public sealed class SootFade
{
    /// <summary>How far out it is, 0 gone, 1 whole.</summary>
    public double P { get; private set; }

    /// <summary>Whether it is open, coming out, rather than closing.</summary>
    public bool Opening { get; private set; }

    /// <summary>Whether anything of it shows.</summary>
    public bool Shown => P > 0;

    /// <summary>The glass's alpha.</summary>
    public float Glass => Opening ? Share(SootRun.Tune.GlassFrom, SootRun.Tune.GlassOpening) : Share(SootRun.Tune.GlassFrom, SootRun.Tune.GlassClosing);

    /// <summary>The contents' alpha: after the glass opening, with it closing.</summary>
    public float Contents => Opening ? Share(SootRun.Tune.ContentsFrom, SootRun.Tune.ContentsFor) : Glass;

    /// <summary>Whether it takes clicks: at once opening, only early in a close.</summary>
    public bool Clicks => Opening ? P > 0 : P > SootRun.Tune.ClicksClosing;

    /// <summary>Steps it <paramref name="seconds"/> on, <paramref name="open"/> or not.</summary>
    public void Step(double seconds, bool open)
    {
        Opening = open;
        P = Math.Clamp(P + (open ? seconds / SootRun.Tune.OpenS : -seconds / SootRun.Tune.CloseS), 0, 1);
        // A run of whole frames ends on its end, not a rounding short of it.
        if (P < 1e-9) P = 0;
        else if (P > 1 - 1e-9) P = 1;
    }

    /// <summary>Back to nothing out, to come out again from the start.</summary>
    public void Restart() => P = 0;

    private float Share(double from, double span) => (float)Math.Clamp((P - from) / span, 0, 1);
}
