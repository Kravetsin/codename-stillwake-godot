namespace Stillwake.Sim.Sky;

public sealed partial class SkyModule
{
    /// <summary>Rarely, an hour anywhere in a day and past it either way, as the dev panel sends it; refused on the ship.</summary>
    public override Command? Fuzz(Rng rng, int from) =>
        rng.Below(16) == 0 ? new SetTime((int)rng.Below(60_000) - 10_000) { From = from } : null;
}
