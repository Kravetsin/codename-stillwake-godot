using System;
using System.Globalization;
using Stillwake.Game.I18n;
using Stillwake.Game.Shared;
using Spec = Stillwake.Content;
using World = Stillwake.Sim.World;

namespace Stillwake.Game.Features.Atmosphere;

/// <summary>What the pointer's tip says of the air's machines: a tank its liquid, a vaporizer or the pod the air it makes.</summary>
public static class AirSays
{
    /// <summary>
    /// The tip's lines for a machine of <paramref name="spec"/>: a tank's <paramref name="held"/> liquid of its capacity, by
    /// the canister; an air source's rate, <paramref name="makesAt"/> thousandths of a unit a tick, a minute's to a tenth,
    /// rounded down, by the oxygen sign, as a room's share reads (tip-signs); null for any other machine.
    /// </summary>
    public static TipLine[]? Says(Spec.MachineSpec spec, int held, int makesAt)
    {
        if (spec.Is(Spec.MachineTrait.Gas)) return [new TipLine(En.T("air.tank", ("held", held), ("of", Spec.Air.TankCapacity)), Icon: "canister")];
        if (!spec.Is(Spec.MachineTrait.AirSource)) return null;
        string perMinute = (Math.Floor((double)makesAt * World.TicksPerSecond * 60 / 100) / 10).ToString(CultureInfo.InvariantCulture);
        return [new TipLine(En.T("air.made", ("n", perMinute)), Sign: Sign.Oxygen)];
    }
}
