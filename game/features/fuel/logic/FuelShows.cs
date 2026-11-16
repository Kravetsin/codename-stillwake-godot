using Stillwake.Game.I18n;
using Stillwake.Game.Shared;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Fuel;

/// <summary>What the pointer says of a fuel store (fuel-liquid): its biofuel of what it holds, in litres and whole cans. No rule of its own.</summary>
public static class FuelShows
{
    /// <summary>
    /// What the pointer's tip says of a machine of <paramref name="spec"/> holding <paramref name="fuel"/>, lasting <paramref name="lasts"/>
    /// ticks at its outflow: its biofuel and how long it lasts; null for one with no store.
    /// </summary>
    public static string[]? Says(Spec.MachineSpec spec, int fuel, int? lasts = null) =>
        spec.Is(Spec.MachineTrait.HoldsFuel)
            ? [En.T("fuel.held", ("held", Litres.Of(fuel)), ("of", Litres.Of(spec.FuelCapacity)), ("whole", fuel / Spec.Fuel.Can)), Lasting(lasts)]
            : null;

    /// <summary>
    /// How long a store's fuel lasts, <paramref name="ticks"/> at its outflow, as a player reads a span: seconds under a minute, whole
    /// minutes under an hour, hours and minutes past; that it gives nothing out while it gives none.
    /// </summary>
    public static string Lasting(int? ticks)
    {
        if (ticks is not int t) return En.T("fuel.idle");
        int seconds = t / Stillwake.Sim.World.TicksPerSecond;
        string span = seconds < 60 ? En.T("time.seconds", ("n", seconds))
            : seconds < 3600 ? En.T("time.minutes", ("n", seconds / 60))
            : En.T("fuel.hours", ("h", seconds / 3600), ("m", seconds / 60 % 60));
        return En.T("fuel.lasts", ("time", span));
    }
}
