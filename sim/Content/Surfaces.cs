namespace Stillwake.Content;

/// <summary>
/// The kinds of surface a world is made of (0076): the ship, and those a world may open besides it. Each names its
/// rules — gravity, air, sky, debris, what may be built — read by the modules and the client of a surface of it, never
/// saved; and the ground it is drawn as. Besides the ship, a strip of ground to stand heroes on, under the first planet's
/// rules, and the planet types (<see cref="PlanetTypes"/>, 0095).
/// </summary>
public static class Surfaces
{
    /// <summary>A light's colour, each part in thousandths of whole, which it may pass.</summary>
    public sealed record Light(int R, int G, int B);

    /// <summary>
    /// A kind of surface: its id, saved with each surface of it, and its ground drawn as a map in <see cref="Legend"/>'s
    /// characters, a character a 16 px cell doubled to the 8 px cells as a surface of it opens (0104), or, for a
    /// <see cref="PlanetType"/>, grown from each surface's seed.
    /// </summary>
    public record Kind(string Id, IReadOnlyList<string> Rows)
    {
        /// <summary>
        /// Gravity everywhere on it, the open sky too, in thousandths of the ship's; null for the ship's own way: gravity
        /// aboard, afloat in space, where the jetpack flies.
        /// </summary>
        public int? Gravity { get; init; }

        /// <summary>
        /// Thin air outside: where the air a hero breathes falls short of the breath, its suit pays one breath in this
        /// many; null for vacuum, the suit paying every one.
        /// </summary>
        public int? ThinAir { get; init; }

        /// <summary>The light through every open cell with no back wall: a planet's daylight; null for the stars'.</summary>
        public Light? Sky { get; init; }

        /// <summary>Whether debris drifts by in the zones of <see cref="Debris.Zones"/>: none on a planet.</summary>
        public bool Debris { get; init; } = true;

        /// <summary>The creatures that come with its debris, clamped on (<see cref="Creatures.OnDebris"/>).</summary>
        public IReadOnlyList<Creatures.OnDebris> Guards { get; init; } = [];

        /// <summary>The meteors that strike its hull (2026-10-28-hull-meteors) once a hero has landed on a planet; none on a planet.</summary>
        public Meteors.Shower? Meteors { get; init; }

        /// <summary>The weaker meteors striking it till then (2026-10-29-weak-meteors-till-landing); <see cref="Meteors"/> when none.</summary>
        public Meteors.Shower? MeteorsBeforeLanding { get; init; }

        /// <summary>
        /// Whether its tools break what stands on its grid, and nothing on it is taken apart by a mark (a planet: the fan of rays);
        /// else the plan alone takes things apart and its tools break nothing of its grid (the ship).
        /// </summary>
        public bool ToolsBreak { get; init; }

        /// <summary>The kinds of placement that may be built on it (<c>wall</c>, <c>ladder</c>, <c>cable</c>); null for every one.</summary>
        public IReadOnlyList<string>? Builds { get; init; }

        /// <summary>Whether heroes and stacks float in its open space: only where no gravity says otherwise.</summary>
        public bool Floats => Gravity == null;

        /// <summary>
        /// <paramref name="gravity"/>, what something gains falling a step aboard the ship, under this kind's gravity:
        /// scaled, rounded down, at least 1.
        /// </summary>
        public int Fall(int gravity) => Gravity is int g ? Math.Max(1, gravity * g / 1000) : gravity;

        /// <summary>Whether a placement of <paramref name="kind"/> (its grid kind: <c>wall</c>, <c>furnace</c>) may be built here.</summary>
        public bool Allows(string kind) => Builds == null || Builds.Contains(kind, StringComparer.Ordinal);
    }

    /// <summary>The ship, surface 0: its grid is the world's layout; every rule as it always was, sentries on the derelicts and the starting space's meteors.</summary>
    public static readonly Kind Ship = new("ship", []) { Guards = [Creatures.Derelicts], Meteors = Content.Meteors.StartingSpace, MeteorsBeforeLanding = Content.Meteors.BeforeLanding };

    /// <summary>
    /// Ground under open sky, a cliff, hills and a cave down a shaft with a ladder, drawn by hand for tests and --strip, a character a 16 px cell and doubled as it opens (0104): under
    /// the first planet's rules (<see cref="PlanetTypes.First"/>). Heroes beam down to its <c>S</c>, in a shelter at the cliff's foot.
    /// </summary>
    public static readonly Kind TestStrip = new("testStrip", Strip())
    {
        Gravity = PlanetTypes.First.Gravity,
        ThinAir = PlanetTypes.First.ThinAir,
        Sky = PlanetTypes.First.Sky,
        Debris = PlanetTypes.First.Debris,
        Builds = PlanetTypes.First.Builds,
        ToolsBreak = PlanetTypes.First.ToolsBreak,
    };

    /// <summary>The kinds a world may open: every one but the ship.</summary>
    public static readonly IReadOnlyList<Kind> All = [TestStrip, .. PlanetTypes.All];

    /// <summary>The kind named <paramref name="id"/>, the ship's too; one not here is our bug: peers run one build.</summary>
    public static Kind Get(string id) =>
        id == Ship.Id ? Ship : All.FirstOrDefault(k => k.Id == id) ?? throw new KeyNotFoundException($"No surface kind '{id}'.");

    private static string[] Strip()
    {
        const int width = 160, height = 64, ground = 30;
        // The top row of ground in each column: a cliff five high at 30, hills, the flat round the shaft.
        int Top(int x) => x switch
        {
            >= 30 and < 40 => ground - 5,
            >= 40 and < 50 => ground - 3,
            >= 110 and < 126 => ground - 2,
            >= 126 and < 140 => ground - 4,
            _ => ground,
        };
        var rows = new char[height][];
        for (int y = 0; y < height; y++)
        {
            rows[y] = new char[width];
            for (int x = 0; x < width; x++) rows[y][x] = y >= Top(x) ? '#' : ' ';
        }
        // A cave of back-walled ground, four rows tall, reached down a shaft three wide with a ladder up its left side.
        for (int y = ground + 3; y <= ground + 6; y++)
            for (int x = 70; x <= 100; x++) rows[y][x] = '.';
        for (int y = ground; y <= ground + 6; y++)
            for (int x = 70; x <= 72; x++) rows[y][x] = x == 70 ? 'H' : '.';
        // Heroes beam down into a shelter cut into the cliff's foot, two wide and three tall, its back the rock.
        for (int y = ground - 3; y < ground; y++)
            for (int x = 30; x <= 31; x++) rows[y][x] = '.';
        rows[ground - 1][30] = 'S';
        return [.. rows.Select(r => new string(r))];
    }
}
