using System;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Shared;

/// <summary>
/// A drone window's clock (drone-frame, the pick «Нити»): seconds that run up while it is open, to <see cref="DroneInk.InsideTo"/>,
/// and back down while it is shut, so a reopen mid-flight turns every square back where it is. Up, the squares land by
/// <see cref="DroneInk.Fly"/> and the screen comes over <see cref="DroneInk.InsideFrom"/>–<see cref="DroneInk.InsideTo"/>; down, the
/// screen goes in <see cref="DroneInk.Close"/>, then the squares fly home over <see cref="DroneInk.Back"/>, the farthest first.
/// Stepped by the window and by the frame alike. Client only.
/// </summary>
public sealed class SootFade
{
    /// <summary>How far out it is, seconds of opening: 0 gone, <see cref="DroneInk.InsideTo"/> whole.</summary>
    public double U { get; private set; }

    /// <summary>Whether it is open, coming out, rather than closing.</summary>
    public bool Opening { get; private set; }

    /// <summary>Whether anything of it shows.</summary>
    public bool Shown => U > 0;

    /// <summary>How far out it is, 0..1.</summary>
    public double Out => U / I.InsideTo;

    /// <summary>The glass's alpha: once the frame stands.</summary>
    public float Glass => (float)Math.Clamp((U - I.InsideFrom) / (I.InsideTo - I.InsideFrom), 0, 1);

    /// <summary>The contents' alpha: with the glass.</summary>
    public float Contents => Glass;

    /// <summary>Whether it takes clicks: at once opening, only till its screen goes closing.</summary>
    public bool Clicks => Opening ? U > 0 : U > I.InsideFrom;

    /// <summary>How far a square <paramref name="lag"/> late (<see cref="SootSpots.Lag"/>) is along its flight: 0 at the drone, 1 landed.</summary>
    public float Landed(float lag) => (float)Math.Clamp((U - lag * (I.Fly - I.One)) / I.One, 0, 1);

    /// <summary>Steps it <paramref name="seconds"/> on, <paramref name="open"/> or not.</summary>
    public void Step(double seconds, bool open)
    {
        Opening = open;
        U = open ? Math.Min(I.InsideTo, U + seconds) : Up(Math.Max(0, Down(U) - seconds));
        // A run of whole frames ends on its end, not a rounding short of it.
        if (U < 1e-9) U = 0;
        else if (U > I.InsideTo - 1e-9) U = I.InsideTo;
    }

    /// <summary>Back to nothing out, to come out again from the start.</summary>
    public void Restart() => U = 0;

    /// <summary>How long shutting takes from <paramref name="u"/>: the screen's part in Close, the squares' in Back.</summary>
    private static double Down(double u) => u <= I.Fly ? u * I.Back / I.Fly : I.Back + (u - I.Fly) * I.Close / (I.InsideTo - I.Fly);

    private static double Up(double down) => down <= I.Back ? down * I.Fly / I.Back : I.Fly + (down - I.Back) * (I.InsideTo - I.Fly) / I.Close;
}
