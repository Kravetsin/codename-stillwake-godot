namespace Stillwake.Sim.Crew;

public sealed partial class CrewModule
{
    /// <summary>
    /// Moves heroes about: mostly near the spawn, where the rooms are, now and then anywhere or off the
    /// grid. The desync fuzz sends from several clients, so several heroes come and go. Now and then a
    /// hero picks a suit colour, one past the last now and then, so taken and missing colours are
    /// refused, and rarely one leaves.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(200) == 0) return new Leave { From = from };
        if (rng.Below(5) == 0) return new PickColour((int)rng.Below(Content.Crew.Colours + 1)) { From = from };
        int cell = rng.Below(4) != 0 ? State.Spawn + (int)rng.Below(9) - 4 : (int)rng.Below(1100) - 50;
        return new Enter(cell) { From = from };
    }
}
