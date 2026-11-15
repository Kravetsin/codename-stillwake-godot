using Stillwake.Game.I18n;
using Stillwake.Game.Shared;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Fuel;

/// <summary>What the pointer says of a fuel store (fuel-liquid): its biofuel of what it holds, in litres and whole cans. No rule of its own.</summary>
public static class FuelShows
{
    /// <summary>What the pointer's tip says of a machine of <paramref name="spec"/> holding <paramref name="fuel"/>; null for one with no store.</summary>
    public static string[]? Says(Spec.MachineSpec spec, int fuel) =>
        spec.Is(Spec.MachineTrait.HoldsFuel)
            ? [En.T("fuel.held", ("held", Litres.Of(fuel)), ("of", Litres.Of(spec.FuelCapacity)), ("whole", fuel / Spec.Fuel.Can))]
            : null;
}
