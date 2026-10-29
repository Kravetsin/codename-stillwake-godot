using Stillwake.Sim.Creatures;
using Stillwake.Sim.Crew;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Creatures on a map drawn by hand (0102): the ship's creatures told it runs on a planet of a type, so a scenario's ASCII map is
/// walked under a planet's gravity and spawned on by its habitats; every other module keeps the ship's rules.
/// </summary>
public static class CreaturesTesting
{
    /// <summary>A surface of <paramref name="kind"/> as creatures sees it: the one a test puts it on.</summary>
    private sealed class On(Spec.Surfaces.Kind kind) : ISurfaces
    {
        public int Here => 1;
        public Spec.Surfaces.Kind Kind => kind;
        public void Send(int peer, int surface) => throw new NotSupportedException("creatures sends no one anywhere");
        public bool CanOpen => false;
        public bool Landed => false;
        public int OpenPlanet() => throw new NotSupportedException("creatures opens no planet");
    }

    /// <summary>
    /// <paramref name="map"/> with its creatures on a planet of <paramref name="kind"/>, the first planet's by default: lopers on its open
    /// ground. Drawn a character a 16 px cell and doubled (0104), so a drawn 2×2 loper is its 4×4 body, unless <paramref name="cells"/>.
    /// </summary>
    public static Scenario Planet(string map, Spec.Surfaces.Kind? kind = null, uint seed = 1, bool cells = false) =>
        Planet(Layout.Parse(map), kind, seed, cells);

    /// <summary><paramref name="map"/> with its creatures on a planet of <paramref name="kind"/>, its world made from <paramref name="seed"/>; doubled unless <paramref name="cells"/>.</summary>
    public static Scenario Planet(Layout map, Spec.Surfaces.Kind? kind = null, uint seed = 1, bool cells = false)
    {
        var w = new Scenario(map, seed, doubled: !cells);
        w.M.Creatures.Where = new On(kind ?? Spec.PlanetTypes.First);
        return w;
    }

    /// <summary><paramref name="w"/> saved and loaded, its creatures told again they run on the planet it put them on.</summary>
    public static Scenario ReloadOnPlanet(this Scenario w)
    {
        var where = w.M.Creatures.Where;
        w.Reload();
        w.M.Creatures.Where = where;
        return w;
    }

    /// <summary>The first planet with nothing living on it: gravity, no spawns.</summary>
    public static readonly Spec.PlanetType Lifeless = Spec.PlanetTypes.First with { Creatures = [] };

    /// <summary>The bottom-left cell of the map's character at (<paramref name="x"/>, <paramref name="y"/>): where feet drawn on it stand.</summary>
    public static int Feet(this Scenario w, int x, int y) => w.At(x, y) + (w.Scale - 1) * w.Layout.Width;

    /// <summary>A loper let out with its bottom left in the map's character at (<paramref name="x"/>, <paramref name="y"/>), and the creature it is.</summary>
    public static Creature Loper(this Scenario w, int x, int y) => w.Let(Spec.Creatures.Loper, x, y);

    /// <summary>A creature of <paramref name="species"/> let out with its bottom left in the map's character at (<paramref name="x"/>, <paramref name="y"/>), and the creature it is.</summary>
    public static Creature Let(this Scenario w, Spec.Creatures.Species species, int x, int y)
    {
        w.Do(new Release(species.Id, w.Feet(x, y))).Run();
        return w.M.Creatures.Creatures[^1];
    }

    /// <summary><paramref name="c"/> set charging <paramref name="way"/> (+1 right, -1 left), its charge just begun (0103), as its retaliation.</summary>
    public static Creature Charging(this Creature c, int way)
    {
        var spec = Spec.Creatures.Get(c.Species);
        var lunge = (Spec.Creatures.Lunge)spec.Moves[0];
        c.Drive = Place<Spec.Creatures.Retaliate>(spec);
        c.Act = new Act { Move = 0, Phase = ActPhase.Active, Left = lunge.Ticks };
        c.VX = way * lunge.Speed;
        return c;
    }

    /// <summary><paramref name="c"/> set walking <paramref name="way"/> (+1 right, -1 left) a long while.</summary>
    public static Creature Walking(this Creature c, int way, Posture posture = Posture.Wander)
    {
        var spec = Spec.Creatures.Get(c.Species);
        var body = (Spec.Creatures.Walker)spec.Body;
        bool flees = posture == Posture.Flee;
        (c.Drive, c.Step, c.DriveLeft, c.Act) = (flees ? Place<Spec.Creatures.Flee>(spec) : Place<Spec.Creatures.Graze>(spec), flees ? 0 : 1, 100_000, null);
        c.VX = way * (flees ? body.Run : body.Walk);
        return c;
    }

    /// <summary><paramref name="c"/> set standing still a long while.</summary>
    public static Creature Still(this Creature c)
    {
        (c.Drive, c.Step, c.DriveLeft, c.Act) = (Place<Spec.Creatures.Graze>(Spec.Creatures.Get(c.Species)), 0, 100_000, null);
        c.VX = 0;
        return c;
    }

    /// <summary>The place in <paramref name="spec"/>'s brain of its drive of kind <typeparamref name="D"/>.</summary>
    public static int Place<D>(Spec.Creatures.Species spec) where D : Spec.Creatures.Drive
    {
        for (int i = 0; i < spec.Brain.Count; i++)
            if (spec.Brain[i] is D) return i;
        throw new InvalidOperationException($"{spec.Id} has no {typeof(D).Name}");
    }

    /// <summary>A species' first move, its ram (0103).</summary>
    public static Spec.Creatures.Lunge Lunge(this Spec.Creatures.Species s) => (Spec.Creatures.Lunge)s.Moves[0];

    /// <summary>A species' retaliation: when and how long it flees before it rams (0103).</summary>
    public static Spec.Creatures.Retaliate Retaliation(this Spec.Creatures.Species s) => s.Brain.OfType<Spec.Creatures.Retaliate>().Single();

    /// <summary>The suit units a species' ram takes, 0 for one that never rams.</summary>
    public static int Ram(this Spec.Creatures.Species s) => s.Moves.Count > 0 ? s.Moves[0].Damage : 0;

    /// <summary>The chance in a thousand a species turns back on the hero that struck it, 0 for one that never does.</summary>
    public static int RamChance(this Spec.Creatures.Species s) => s.Brain.OfType<Spec.Creatures.Retaliate>().FirstOrDefault()?.Chance ?? 0;

    public static int Run(this Spec.Creatures.Species s) => ((Spec.Creatures.Walker)s.Body).Run;

    public static int Walk(this Spec.Creatures.Species s) => ((Spec.Creatures.Walker)s.Body).Walk;

    /// <summary>How long a species flees once struck.</summary>
    public static int FleeTicks(this Spec.Creatures.Species s) => s.Brain.OfType<Spec.Creatures.Flee>().Single().Ticks;

    /// <summary>Creative mode: every hero's suit endless, so heroes out in a long run never pass out and wake elsewhere.</summary>
    public static Scenario Breathing(this Scenario w) => w.Do(new Items.SetCreative(true));

    /// <summary>Hero <paramref name="peer"/> reported at the feet of the map's character at (<paramref name="x"/>, <paramref name="y"/>), level with a creature standing there.</summary>
    public static Scenario HeroAt(this Scenario w, int x, int y, int peer = 0) => w.Do(new Enter(w.Feet(x, y)) { From = peer });

    /// <summary>The middle of the cell a hero <see cref="HeroAt"/> column <paramref name="x"/> is reported in, in thousandths of a cell.</summary>
    public static int HeroMiddle(this Scenario w, int x) => x * w.Scale * CreaturesModule.Unit + CreaturesModule.Unit / 2;
}
