using System.Collections.Generic;
using System.Linq;
using Stillwake.Sim;
using L = Stillwake.Game.Light;

namespace Stillwake.Game.Features.Light;

/// <summary>
/// A beacon outside a derelict (derelict-looks): where it shines from, in pixels of its vessel's frame, and its look; what keeps it
/// lit, a machine of <c>Kind</c> whose top-left is <c>Cell</c>, or for none the hull's wall in <c>Cell</c>.
/// </summary>
public readonly record struct Beacon(float X, float Y, L.BeaconLight Look, (int X, int Y) Cell, string? Kind);

/// <summary>
/// A derelict's beacons (derelict-looks): a red one on each mast's top, a white strobe on its boom's tip or, with no boom, at its
/// bow's tip, each from its description, as many as it was made with; and how each blinks on this client's clock.
/// </summary>
public static class Beacons
{
    private const int Cell = Camera.Cell;

    /// <summary>The beacons <paramref name="v"/> was made with, the parts' in their order, then the bow's strobe where no part has one.</summary>
    public static IReadOnlyList<Beacon> Of(Vessel v)
    {
        var beacons = new List<Beacon>();
        foreach (var f in v.Outside)
            if (L.Beacons.TryGetValue(f.Kind, out var b)) beacons.Add(new(f.X * Cell + b.At.X, f.Y * Cell + b.At.Y, b.Look, (f.X, f.Y), f.Kind));
        if (beacons.Any(b => b.Look == L.Strobe)) return beacons;
        // On the tip's middle row, at its outer edge.
        var bow = v.Sections.First(s => s.Kind == Vessel.SectionKind.Bow);
        int x = bow.Left + bow.Width - 1;
        var (top, bottom) = bow.RowsAt(x);
        int y = (top + bottom) / 2;
        beacons.Add(new((x + 1) * Cell - 1.5f, y * Cell + Cell / 2f, L.Strobe, (x, y), null));
        return beacons;
    }

    /// <summary>How brightly a beacon of <paramref name="look"/> shines at <paramref name="ms"/>, its phase <paramref name="phaseMs"/> on: whole for its duty, else dark.</summary>
    public static float Shines(L.BeaconLight look, double ms, double phaseMs)
    {
        double t = (ms + phaseMs) % look.PeriodMs;
        return t >= 0 && t < look.Duty * look.PeriodMs ? 1 : 0;
    }

    /// <summary>Where in its blink beacon <paramref name="index"/> starts, so no two of a vessel flash together.</summary>
    public static double PhaseOf(int index) => index * 377.0;
}
