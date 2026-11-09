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
    /// the canister, and the whole canisters it is (liquid-volumes); an air source's rate, <paramref name="makesAt"/> thousandths of a unit a tick, a minute's to a tenth,
    /// rounded down, by the oxygen sign, as a room's share reads (tip-signs); null for any other machine.
    /// </summary>
    public static TipLine[]? Says(Spec.MachineSpec spec, int held, int makesAt)
    {
        if (spec.Is(Spec.MachineTrait.Gas))
            return [new TipLine(En.T("air.tank", ("held", Litres.Of(held)), ("of", Litres.Of(Spec.Air.TankCapacity)), ("whole", held / Spec.Air.Canister)), Icon: "canister")];
        if (!spec.Is(Spec.MachineTrait.AirSource)) return null;
        // Thousandths of a unit a tick are units a period over a thousand.
        return [new TipLine(En.T("air.made", ("n", Litres.PerMinute((long)makesAt * Spec.Air.LeakEvery / 1000))), Sign: Sign.Oxygen)];
    }
}
