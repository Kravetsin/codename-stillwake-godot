namespace Stillwake.Sim.Sky;

public sealed partial class SkyModule
{
    /// <summary>
    /// Rarely, an hour anywhere in a day and past it either way, as the dev panel sends it, refused on the ship; as rarely, a stretch
    /// brought now, one of this surface's or now and then one unknown, refused while one runs.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        uint roll = rng.Below(32);
        if (roll < 2) return new SetTime((int)rng.Below(60_000) - 10_000) { From = from };
        if (roll != 2) return null;
        var all = Rules.Stretches;
        int at = (int)rng.Below((uint)all.Count + 1);
        return new StretchNow(at < all.Count ? all[at].Id : "noSuchStretch") { From = from };
    }
}
