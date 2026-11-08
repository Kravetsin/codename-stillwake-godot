using System;
using Stillwake.Game.Render;
using static Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The plasma cutter's jet while it runs (damage-looks, "beam"): a straight violet sheath round a thin white heart, two flat
/// tones, its width flickering; at nothing it thins to a point, and where it cuts a wreck a small hard splash, white in a violet ring.
/// </summary>
public static class CutterJet
{
    /// <summary>The jet from <paramref name="from"/>, the nozzle, to <paramref name="to"/>, cutting there if it <paramref name="hits"/>, at <paramref name="clockMs"/>.</summary>
    public static void Draw(Ink ink, (double X, double Y) from, (double X, double Y) to, bool hits, double clockMs)
    {
        double length = Math.Sqrt((to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y));
        double f = 1 + WorkLooks.JetFlicker * (Hash((int)(clockMs * WorkLooks.JetFlickerHz / 1000), 1) - 0.5) * 2;
        // Pressed right on what it cuts, the nozzle shows only the splash.
        if (length > 0.5)
        {
            double end = hits ? 0.02 : Math.Min(1, WorkLooks.JetTaper / length);
            (double X, double Y)[] line = [from, to];
            ink.Pen(line, WorkLooks.JetSheath * f, WorkLooks.JetViolet, 1, 0.03, end);
            ink.Pen(line, WorkLooks.JetHeart * f, WorkLooks.JetWhite, 2, 0.03, end);
        }
        if (!hits) return;
        int n = (int)(clockMs / WorkLooks.SplashMs);
        double splash = WorkLooks.SplashRadius * (1 + WorkLooks.SplashFlicker * (Hash(n, 4) - 0.5));
        ink.Fill(Blob(new Rng(n), to.X, to.Y, splash * 1.9, 8, 0.45), to, WorkLooks.SplashRing);
        ink.Fill(Blob(new Rng(n + 1), to.X, to.Y, splash, 7, 0.35), to, WorkLooks.JetWhite);
    }
}
