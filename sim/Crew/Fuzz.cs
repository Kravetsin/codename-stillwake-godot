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
    /// full suit or none carried; switches its suit's mode, refused (<c>crew.noSuitMode</c>) unless the items fuzz spawned and
    /// put on a regen suit; eats a food or a canister, so every refusal of <c>Eat</c> is drawn; purges, refused with nothing eaten; pairs
    /// with a drone (<c>PairDrone</c>); brings a hunt near
    /// (<c>Track</c>); and rarely one leaves.
    /// </summary>
    public override Command? Fuzz(Rng rng, int from)
    {
        if (rng.Below(200) == 0) return new Leave { From = from };
        if (rng.Below(6) == 0) return new Exert(rng.Below(2) == 0) { From = from };
        if (rng.Below(8) == 0) return new Thrust(rng.Below(2) == 0) { From = from };
        if (rng.Below(12) == 0) return new Refill { From = from };
        // One draw past the foods purges instead: refused with nothing eaten; two past, pairs with a drone at the map's alien core,
        // refused paired already or on a map with none (machine 0).
        if (rng.Below(12) == 0)
            return rng.Below((ulong)Edible.Length + 2) is var e && e < Edible.Length ? new Eat(Edible[e]) { From = from }
                : e == (ulong)Edible.Length ? new Purge { From = from }
                : new PairDrone(_grid.MachinesWith(Content.MachineTrait.Wakes) is [var core, ..] ? core.Id : 0) { From = from };
        if (rng.Below(40) == 0) return new Track((int)rng.Below(400)) { From = from };
        if (rng.Below(60) == 0) return new Succumb { From = from };
        if (rng.Below(10) == 0) return new SuitMode(rng.Below(2) == 0) { From = from };
        if (rng.Below(5) == 0) return new PickColour((int)rng.Below(Content.Crew.Colours + 1)) { From = from };
        if (rng.Below(10) == 0)
        {
            var look = new Look(Pick(), Pick(), Pick(), Pick(), Pick(), Pick());
            // Skins half the time, drawn from the look's own numbers: no draw more, so every other fuzz plays as before.
            return look.Skin % 2 == 0 ? new ChooseLook(look) { From = from }
                : new ChooseSkins(SkinSets[(look.Hair + 2) % SkinSets.Length]) { From = from };
        }
        int cells = _grid.Width * _grid.Height;
        int cell = rng.Below(4) != 0 ? State.Spawn + (int)rng.Below(9) - 4 : (int)rng.Below((ulong)cells + 100) - 50;
        return new Enter(cell) { From = from };

        // Any index, a negative or a large one now and then: the sim takes them as sent.
        int Pick() => (int)rng.Below(12) - 2;
    }

    /// <summary>What a client may send as skins, the sim knowing none: none, one, an unknown id, two of one kind, one of another kind.</summary>
    private static readonly string[][] SkinSets = [[], ["suit_a"], ["nobody's"], ["suit_a", "suit_b"], ["suit_a", "effect_a"]];

    /// <summary>What a hero tries to eat: the foods, a fruit drawing its seed, a seed, a dish and a canister, no food.</summary>
    private static readonly string[] Edible = [Content.Foods.Ration, Content.Foods.CannedMeat, Content.Foods.Fruit, Content.Foods.Seed, Content.Foods.Porridge, Content.Crew.Canister];
}
