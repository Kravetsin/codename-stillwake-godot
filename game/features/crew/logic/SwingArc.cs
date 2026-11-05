using System;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A swing's light follows its blade by its angle round the shoulder, which <c>Math.Atan2</c> gives within half a turn either way:
/// a stroke aimed up, in the arms' frame, crosses that seam, and its angles must stay continuous across it.
/// </summary>
public static class SwingArc
{
    /// <summary><paramref name="now"/>, in radians, moved by whole turns to lie within half a turn of <paramref name="last"/>.</summary>
    public static double Near(double last, double now) => last + Math.IEEERemainder(now - last, Math.Tau);
}
