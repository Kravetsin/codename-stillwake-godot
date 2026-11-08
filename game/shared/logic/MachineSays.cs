using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stillwake.Content;
using Stillwake.Game.I18n;

namespace Stillwake.Game.Shared;

/// <summary>What the pointer's tip says of a machine out of every overlay (tip-signs): its name, what its feature says, and its power.</summary>
public static class MachineSays
{
    /// <summary>A machine of <paramref name="kind"/>'s name, as the build menu calls it, or, for one never built (the pod), its own.</summary>
    public static string Name(string kind) => En.Table.ContainsKey($"tool.{kind}") ? En.T($"tool.{kind}") : En.T($"machine.{kind}");

    /// <summary>
    /// Its power by the bolt: what a machine of <paramref name="spec"/> draws while it works, <paramref name="draw"/> with its
    /// modules counted, shown whether it works now or not; a maker's rated making, after a plus; null off the power.
    /// </summary>
    public static TipLine? Power(MachineSpec spec, int draw) =>
        spec.Draws != null ? new TipLine(draw.ToString(CultureInfo.InvariantCulture), Sign: Sign.Power)
        : spec.Makes is int makes ? new TipLine($"+{makes.ToString(CultureInfo.InvariantCulture)}", Sign: Sign.Power)
        : null;

    /// <summary>The tip's lines, its name first, for a machine of <paramref name="kind"/>; null when neither its feature nor its power says anything.</summary>
    public static IReadOnlyList<TipLine>? Of(string kind, IReadOnlyList<TipLine>? said, TipLine? power)
    {
        var lines = (said ?? []).Concat(power is { } line ? [line] : []).ToList();
        return lines.Count == 0 ? null : [new TipLine(Name(kind)), .. lines];
    }
}
