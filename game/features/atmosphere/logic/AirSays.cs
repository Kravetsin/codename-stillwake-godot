using System.Globalization;
using Stillwake.Game.I18n;
using Spec = Stillwake.Content;
using World = Stillwake.Sim.World;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>What the pointer's tip says of the air's machines: a tank its liquid, a vaporizer or the pod the air it makes.</summary>
public static class AirSays
{
    /// <summary>
    /// The tip's lines for a machine of <paramref name="spec"/>: a tank's <paramref name="held"/> liquid of its capacity; an air
    /// source's rate, <paramref name="makesAt"/> thousandths of a unit a tick, as units a second to a tenth, rounded down; null
    /// for any other machine.
    /// </summary>
    public static string[]? Says(Spec.MachineSpec spec, int held, int makesAt)
    {
        if (spec.Is(Spec.MachineTrait.Gas)) return [En.T("air.tank", ("held", held), ("of", Spec.Air.TankCapacity))];
        if (!spec.Is(Spec.MachineTrait.AirSource)) return null;
        string perSecond = ((long)makesAt * World.TicksPerSecond / 100 / 10.0).ToString("0.#", CultureInfo.InvariantCulture);
        return [En.T("air.makes", ("n", perSecond))];
    }
}
