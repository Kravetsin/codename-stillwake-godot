using Stillwake.Sim.Players;

namespace Stillwake.Sim.Crew;

public sealed partial class CrewModule
{
    /// <summary>
    /// Moves heroes about: mostly near the spawn, where the rooms are, one time in four anywhere on the
    /// grid or a little off it, so the field, the band past it and the edge are all breathed in. The desync
    /// fuzz sends from several clients, so several heroes come and go. Now and then a hero picks a suit
    /// colour, one past the last now and then, so taken and missing colours are refused; chooses a look; thrusts or stops,
    /// mostly with no jetpack worn, so <c>crew.noJetpack</c> is drawn; breathes a canister, refused with a
    /// full suit or none carried; and rarely one leaves.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(200) == 0) return new Leave { From = from };
        if (rng.Below(6) == 0) return new Exert(rng.Below(2) == 0) { From = from };
        if (rng.Below(8) == 0) return new Thrust(rng.Below(2) == 0) { From = from };
        if (rng.Below(12) == 0) return new Refill { From = from };
        if (rng.Below(5) == 0) return new PickColour((int)rng.Below(Content.Crew.Colours + 1)) { From = from };
        if (rng.Below(10) == 0) return new ChooseLook(new Look(Pick(), Pick(), Pick(), Pick(), Pick())) { From = from };
        int cells = _grid.Width * _grid.Height;
        int cell = rng.Below(4) != 0 ? State.Spawn + (int)rng.Below(9) - 4 : (int)rng.Below((ulong)cells + 100) - 50;
        return new Enter(cell) { From = from };

        // Any index, a negative or a large one now and then: the sim takes them as sent.
        int Pick() => (int)rng.Below(12) - 2;
    }
}
