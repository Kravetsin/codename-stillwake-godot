using Stillwake.Sim.Crew;
using Stillwake.Sim.Fauna;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Creatures on a map drawn by hand (0102): the ship's fauna told it runs on a planet of a type, so a scenario's ASCII map is
/// walked under a planet's gravity and spawned on by its habitats; every other module keeps the ship's rules.
/// </summary>
public static class FaunaTesting
{
    /// <summary>A surface of <paramref name="kind"/> as fauna sees it: the one a test puts it on.</summary>
    private sealed class On(Spec.Surfaces.Kind kind) : ISurfaces
    {
        public int Here => 1;
        public Spec.Surfaces.Kind Kind => kind;
        public void Send(int peer, int surface) => throw new NotSupportedException("fauna sends no one anywhere");
    }

    /// <summary><paramref name="map"/> with its fauna on a planet of <paramref name="kind"/>, the first planet's by default: lopers on its open ground.</summary>
    public static Scenario Planet(string map, Spec.Surfaces.Kind? kind = null) => Planet(Layout.Parse(map), kind);

    /// <summary><paramref name="map"/> with its fauna on a planet of <paramref name="kind"/>.</summary>
    public static Scenario Planet(Layout map, Spec.Surfaces.Kind? kind = null)
    {
        var w = new Scenario(map);
        w.M.Fauna.Where = new On(kind ?? Spec.PlanetTypes.First);
        return w;
    }

    /// <summary>The first planet with nothing living on it: gravity, no spawns.</summary>
    public static readonly Spec.PlanetType Lifeless = Spec.PlanetTypes.First with { Fauna = [] };

    /// <summary>A loper let out with its bottom left in (<paramref name="x"/>, <paramref name="y"/>), and the creature it is.</summary>
    public static Creature Loper(this Scenario w, int x, int y) => w.Let(Spec.Fauna.Loper, x, y);

    /// <summary>A creature of <paramref name="species"/> let out with its bottom left in (<paramref name="x"/>, <paramref name="y"/>), and the creature it is.</summary>
    public static Creature Let(this Scenario w, Spec.Fauna.Species species, int x, int y)
    {
        w.Do(new Release(species.Id, w.At(x, y))).Run();
        return w.M.Fauna.Creatures[^1];
    }

    /// <summary><paramref name="c"/> set charging <paramref name="way"/> (+1 right, -1 left), its charge just begun (0103).</summary>
    public static Creature Charging(this Creature c, int way)
    {
        c.Mode = Mode.Charge;
        c.ModeLeft = Spec.Fauna.ChargeTicks;
        c.VX = way * Spec.Fauna.Charge;
        return c;
    }

    /// <summary><paramref name="c"/> set walking <paramref name="way"/> (+1 right, -1 left) a long while.</summary>
    public static Creature Walking(this Creature c, int way, Mode mode = Mode.Wander)
    {
        var spec = Spec.Fauna.Get(c.Species);
        c.Mode = mode;
        c.ModeLeft = 100_000;
        c.VX = way * (mode == Mode.Flee ? spec.Flee : spec.Wander);
        return c;
    }

    /// <summary><paramref name="c"/> set standing still a long while.</summary>
    public static Creature Still(this Creature c)
    {
        c.Mode = Mode.Idle;
        c.ModeLeft = 100_000;
        c.VX = 0;
        return c;
    }

    /// <summary>Creative mode: every hero's suit endless, so heroes out in a long run never pass out and wake elsewhere.</summary>
    public static Scenario Breathing(this Scenario w) => w.Do(new Items.SetCreative(true));

    /// <summary>Hero <paramref name="peer"/> reported at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public static Scenario HeroAt(this Scenario w, int x, int y, int peer = 0) => w.Do(new Enter(w.At(x, y)) { From = peer });
}
