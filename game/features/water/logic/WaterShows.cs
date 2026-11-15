using System;
using System.Globalization;
using Stillwake.Game.I18n;
using Stillwake.Game.Shared;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Water;

/// <summary>
/// How water shows without a word, and what the pointer says of it (water): a cistern's water up its sight glass by any level
/// (<c>art/cistern_level.png</c>, <c>GlassLevel</c>), an electrolyser's bubbles rising while it works (<c>art/electrolyser_work_*.png</c>) and its
/// lamp green (<c>art/electrolyser_working.png</c>) or red while it stands (<c>art/electrolyser_stalled.png</c>). No rule of its own: the sim says what each holds and does.
/// </summary>
public static class WaterShows
{
    /// <summary>The bubbles' frames, and how long each shows.</summary>
    public const int Frames = 4;
    public const double FrameMs = 150;

    /// <summary>The bubbles' frame <paramref name="ms"/> into the game.</summary>
    public static int WorkFrame(double ms) => (int)(ms / FrameMs) % Frames;

    /// <summary>
    /// What the pointer's tip says of a machine of <paramref name="spec"/>: a cistern its water of what it holds; an
    /// electrolyser the water it turns into oxygen a minute at its power's <paramref name="satisfaction"/>, or why it stands
    /// (<paramref name="stalled"/>); null for any other. In litres, as oxygen reads (industrial-oxygen).
    /// </summary>
    public static string[]? Says(Spec.MachineSpec spec, int water, string? stalled, int satisfaction = 1000)
    {
        if (spec.Is(Spec.MachineTrait.HoldsWater))
            return [En.T("water.cistern", ("held", Litres.Of(water)), ("of", Litres.Of(Spec.Water.CisternCapacity)), ("whole", water / Spec.Water.Bottle))];
        if (!spec.Is(Spec.MachineTrait.Electrolyses)) return null;
        // A unit of water a unit of liquid oxygen: its rate a period, at its power.
        return stalled != null
            ? [En.T(stalled)]
            : [En.T("water.splitting", ("n", Litres.PerMinute((long)Spec.Water.ElectrolyserRate * satisfaction / 1000)))];
    }
}
