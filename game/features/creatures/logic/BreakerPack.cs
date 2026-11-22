using System;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// A nest as drawn (breakers-look, the user's): a pair at work on its piece's rim and the rest unseen under it; as the pack goes the
/// pair leaves the rim and the rest fly out from under the piece one after another. Plain C#, tested.
/// </summary>
public static class BreakerPack
{
    public const int AtWork = 2;

    /// <summary>How far off its piece's box a body at work has its middle, how long one takes out to where the sim woke it, and one after another.</summary>
    public const double RimPixels = 5, OutS = 0.38, EachS = 0.07;

    /// <summary>The share of its way out it is still under the piece.</summary>
    public const double UnderTill = 0.9;

    // The rim's eight places going round.
    private static readonly (double X, double Y)[] Rim = [(1, 0), (0.8, 0.88), (0, 1), (-0.8, 0.88), (-1, 0), (-0.8, -0.88), (0, -1), (0.8, -0.88)];

    /// <summary>One at work: its middle from its piece's, turned and facing the piece, where its clock is and how fast that goes.</summary>
    public readonly record struct Worker(double X, double Y, double Angle, int Face, double Phase, double Pace)
    {
        /// <summary>Its clock <paramref name="seconds"/> on: none of a pair in step.</summary>
        public double At(double seconds) => seconds * Pace + Phase;
    }

    /// <summary>The <paramref name="i"/>th at work on piece <paramref name="piece"/> of half box <paramref name="halfW"/> by <paramref name="halfH"/> pixels, the same on every peer.</summary>
    public static Worker WorkerOf(int piece, int i, double halfW, double halfH)
    {
        var (ux, uy) = Rim[(int)(((long)piece * 5 + i * 3) % Rim.Length)];
        double far = Math.Sqrt(ux * ux + uy * uy), slip = (Own(piece, i, 0) - 0.5) * 9;
        double x = ux * (halfW + RimPixels) - uy / far * slip, y = uy * (halfH + RimPixels) + ux / far * slip;
        double angle = Math.Atan2(-y, -x) + (Own(piece, i, 1) - 0.5) * 0.7;
        int face = Math.Cos(angle) < 0 ? -1 : 1;
        // Mirrored rather than upside down.
        if (face < 0) angle = Math.IEEERemainder(angle + Math.PI, Math.Tau);
        return new Worker(x, y, angle, face, Own(piece, i, 3) * KnotMoves.BiteS, 0.8 + Own(piece, i, 2) * 0.4);
    }

    /// <summary>How much of the way out one is <paramref name="seconds"/> after its pack went, <paramref name="after"/> others out before it: 0 not yet, 1 out.</summary>
    public static double Out(double seconds, int after) => Math.Clamp((seconds - after * EachS) / OutS, 0, 1);

    /// <summary>The share of its offset gone at <paramref name="u"/> of its way: quick off, easing in.</summary>
    public static double Gone(double u) => 1 - (1 - u) * (1 - u);

    private static double Own(int piece, int i, int n)
    {
        double v = Math.Sin(piece * 12.9898 + i * 78.233 + n * 37.719) * 43758.5453;
        return v - Math.Floor(v);
    }
}
