using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The blasting charge (blasting-charge, unified-damage): thrown as a gun fires, it bursts where its shot ends, a hit in every direction,
/// what it breaks dropped there for anyone and what it cannot pay for cracked; wrecks in reach take its damage.
/// </summary>
public class BlastChargeTests
{
    private const int Unit = CreaturesModule.Unit, W = 41;
    private static readonly string Charge = Spec.Shots.Thrown.Item;
    private static readonly Spec.Blast Blast = Spec.Blasting.Charge;

    /// <summary>Headings: along +x, and down.</summary>
    private const int Right = 0, Down = 64;

    /// <summary>
    /// The first planet's rules, a cell a character: 41 cells across, ten open rows over twenty of <paramref name="ground"/>;
    /// <paramref name="paint"/> changes a cell's look by (column, row), and the hero stands on the ground at column 2.
    /// </summary>
    private static Scenario Pit(char ground, Func<int, int, char?>? paint = null, IReadOnlyList<(int Cell, string Kind)>? flora = null)
    {
        var rows = new List<string>();
        var looks = new List<string>();
        for (int y = 0; y < 30; y++)
        {
            var row = new char[W];
            var look = new char[W];
            for (int x = 0; x < W; x++)
            {
                char? painted = paint?.Invoke(x, y);
                bool wall = y >= 10 || painted != null;
                row[x] = wall ? '#' : '.';
                look[x] = wall ? painted ?? ground : '.';
            }
            rows.Add(new string(row));
            looks.Add(new string(look));
        }
        var layout = Layout.Parse(string.Join('\n', rows)).WithMaterials(string.Join('\n', looks)) with
        {
            Kind = Spec.Surfaces.TestStrip.Id,
            Flora = flora ?? [],
        };
        return new Scenario(layout).Do(new Crew.Enter(9 * W + 2)).GiveItems(0, (Charge, 5)).Run();
    }

    /// <summary>A charge thrown from the middle of the cell at (<paramref name="column"/>, <paramref name="row"/>).</summary>
    private static Fire At(int column, int row, int heading) => new(Charge, column * Unit + Unit / 2, row * Unit + Unit / 2, heading);

    /// <summary>Throws <paramref name="fire"/> and runs till its shot ends; that end.</summary>
    private static ShotEnd Thrown(Scenario w, Fire fire)
    {
        w.Do(fire);
        for (int i = 0; i < 200; i++)
        {
            w.Run();
            if (w.M.Creatures.Ended.Count > 0) return w.M.Creatures.Ended.Single();
        }
        throw new InvalidOperationException("the charge never landed");
    }

    /// <summary>The ground's cells the burst opened: walls of the map that are walls no more.</summary>
    private static List<int> Opened(Scenario w) =>
        [.. Enumerable.Range(0, W * 30).Where(c => w.Layout.At(c) == '#' && w.M.Grid.WallAt(c) == null)];

    private static int Count(Scenario w, string item) => w.M.Items.Totals(0).Where(t => t.Item == item).Sum(t => t.Count);

    private static int Lying(Scenario w, string item) => w.M.Items.Floor.Where(s => s.Item == item).Sum(s => s.Count);

    private static int CrackAt(Scenario w, int cell) => w.M.Building.Cracks.SingleOrDefault(c => c.Cell == cell)?.Damage ?? 0;

    [Fact]
    public void ThrownDownOnStoneItOpensACraterFourDeepCracksItsRimAndDropsGravelForAnyone()
    {
        var w = Pit('t');
        var end = Thrown(w, At(20, 8, Down));
        Assert.Empty(w.Refusals);
        Assert.Equal(4, Count(w, Charge));
        // It met the stone below and burst from the open cell it flew from.
        Assert.True(end.Met);
        Assert.Equal(8 * W + 20, end.Burst);
        var opened = Opened(w);
        Assert.All([10, 11, 12, 13], y => Assert.Contains(y * W + 20, opened));
        Assert.DoesNotContain(14 * W + 20, opened);
        Assert.True(CrackAt(w, 14 * W + 20) > 0);
        Assert.All(opened, c => Assert.True(Math.Abs(c % W - 20) <= Blast.Reach && c / W - 8 <= Blast.Reach));
        // Each stone it broke lies where it was as a gravel, anyone's: quick, but no rock to build with.
        Assert.Equal(opened.Count, Lying(w, "gravel"));
        Assert.Equal(0, Lying(w, "rock") + Count(w, "rock"));
        Assert.All(w.M.Items.Floor, s => Assert.Equal(FloorStack.Nobody, s.By));
    }

    [Fact]
    public void ASecondChargeOnTheSameStoneGoesFurther()
    {
        var w = Pit('t');
        Thrown(w, At(20, 8, Down));
        int first = Opened(w).Count;
        Thrown(w, At(20, 8, Down));
        Assert.Contains(14 * W + 20, Opened(w));
        Assert.True(Opened(w).Count > first);
    }

    [Fact]
    public void SoilOpensWiderThanStoneAndAlloyNotAtAll()
    {
        int Crater(char ground)
        {
            var w = Pit(ground);
            Thrown(w, At(20, 8, Down));
            return Opened(w).Count;
        }
        int soil = Crater('s'), stone = Crater('t'), alloy = Crater('a');
        Assert.True(soil > stone, $"soil {soil}, stone {stone}");
        Assert.True(stone > 0);
        Assert.Equal(0, alloy);
    }

    [Fact]
    public void ItsCraterIsRoundInSoilTheWholeCircleOfItsReachAndInStoneAsFarAslantAsStraightDown()
    {
        // Soil, cheap to break: every cell of the ground within its reach of the burst opens, none beyond, a circle cut off by the surface.
        var soil = Pit('s');
        Thrown(soil, At(20, 8, Down));
        var open = Opened(soil).ToHashSet();
        for (int c = 10 * W; c < 30 * W; c++)
        {
            int dx = c % W - 20, dy = c / W - 8;
            Assert.True(dx * dx + dy * dy <= Blast.Reach * Blast.Reach == open.Contains(c), $"({c % W}, {c / W})");
        }
        // Stone soaks it within its reach: dropped down a shaft into solid stone, the air no shortcut round it, it opens as far out
        // down the diagonal as straight down, give or take the cell and a half four cells' 7/5 steps round to.
        var stone = Pit('t', (x, y) => y < 10 && x is >= 8 and <= 32 && (x != 20 || y == 9) ? 't' : null);
        Thrown(stone, At(20, 5, Down));
        var opened = Opened(stone).ToHashSet();
        int down = Enumerable.Range(1, 21).Last(k => opened.Contains((8 + k) * W + 20) || k == 1);
        int aslant = Enumerable.Range(1, 21).Last(k => opened.Contains((8 + k) * W + 20 + k) || k == 1);
        Assert.True(Math.Abs(aslant * Math.Sqrt(2) - down) <= 1.5, $"stone {down} deep, {aslant} aslant");
        Assert.True(down < Blast.Reach, $"stone {down} deep: soaked short of its reach");
    }

    [Fact]
    public void AlloyOnlyCracksAndASecondChargeBeforeItHealsOpensIt()
    {
        var w = Pit('a');
        Thrown(w, At(20, 8, Down));
        Assert.Empty(Opened(w));
        // What was left after the two steps down to it.
        Assert.Equal(Blast.Power - 2 * Spec.Damage.Step, CrackAt(w, 10 * W + 20));
        Thrown(w, At(20, 8, Down));
        Assert.Contains(10 * W + 20, Opened(w));
    }

    [Fact]
    public void AScrapWallInTheGroundBreaksToOneChargeInFull()
    {
        var w = Pit('t', (x, y) => x == 20 && y == 10 ? 'w' : null);
        Thrown(w, At(20, 8, Down));
        Assert.Null(w.M.Grid.WallAt(10 * W + 20));
        Assert.Equal(1, Lying(w, "scrapChunk"));
    }

    [Fact]
    public void AnIronWallOnlyCracksAndBedrockStopsItWhatLiesBehindThemStays()
    {
        // An iron wall down column 15 through the open and the stone; bedrock down column 25.
        var w = Pit('t', (x, y) => x == 15 ? 'n' : x == 25 ? 'b' : null);
        Thrown(w, At(20, 8, Down));
        var opened = Opened(w);
        Assert.Contains(12 * W + 20, opened);
        Assert.All(opened, c => Assert.InRange(c % W, 16, 24));
        Assert.Contains(w.M.Building.Cracks, c => c.Cell % W == 15);
        Assert.DoesNotContain(w.M.Building.Cracks, c => c.Cell % W is < 15 or >= 25);
    }

    [Fact]
    public void OreAGeodeAndADeepCacheItBreaksFallWhereTheyWereForAnyone()
    {
        // Sulfur at (20, 11) and (21, 11), a geode in the stone at (17, 11), an amber cache at (23, 12).
        var w = Pit('t', (x, y) => y == 11 && x is 20 or 21 ? 'y' : null, [(11 * W + 17, "geode"), (12 * W + 23, "amberCache")]);
        Thrown(w, At(20, 8, Down));
        Assert.Null(w.M.Grid.WallAt(11 * W + 20));
        Assert.Equal(2, Lying(w, "sulfur"));
        Assert.Equal(Spec.Flora.Get("geode").Yields.Single().Count, Lying(w, "crystal"));
        Assert.Equal(Spec.Flora.Get("amberCache").Yields.Single().Count, Lying(w, "opal"));
        Assert.Equal(0, Count(w, "sulfur") + Count(w, "crystal") + Count(w, "opal"));
        Assert.Empty(w.M.Building.Plants);
    }

    [Fact]
    public void AboardTheShipItCracksTheHullAndBreaksNone()
    {
        var w = new Scenario(".....#....\n..........\n##########", cells: true).Do(new Crew.Enter(12)).GiveItems(0, (Charge, 1)).Run();
        var end = Thrown(w, At(1, 0, Right));
        Assert.True(end.Burst >= 0);
        Assert.Equal(0, Count(w, Charge));
        Assert.NotNull(w.M.Grid.WallAt(5));
        Assert.True(CrackAt(w, 5) > 0);
        Assert.All(Enumerable.Range(20, 10), c => Assert.NotNull(w.M.Grid.WallAt(c)));
    }

    [Fact]
    public void InSpaceEveryWreckInItsReachTakesItsDamageTheOneItStruckItsOwnTooAndOnesFartherNothing()
    {
        var w = ShotsTests.Space().GiveItems(0, (Charge, 1));
        var debris = (DebrisState)w.M.Debris.Slice;
        Piece Wreck(int x, int y) => new() { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = "engineBlock", X = x, Y = y, Start = x, SeenAt = debris.Clock };
        // Struck at its left edge, 25 cells across; one above the path, its box within reach of the burst; one far beyond.
        var struck = Wreck(30_000, 10_000);
        var near = Wreck(20_000, 3_000);
        var far = Wreck(80_000, 10_000);
        debris.Pieces.AddRange([struck, near, far]);
        SentryTests.Guarded(w);
        var end = Thrown(w, At(10, 10, Right));
        Assert.True(end.Met);
        Assert.Equal(Spec.Shots.DamageOf(Charge, Charge) + Blast.WreckDamage, struck.Damage);
        Assert.Equal(Blast.WreckDamage, near.Damage);
        Assert.Equal(0, far.Damage);
    }

    [Fact]
    public void OutOfTheBuildZoneItStillBurstsOnTheWrecksInReach()
    {
        // The ship's open space past its zone: nothing built there, but the wrecks drifting in it.
        var layout = Layout.Parse(string.Join('\n', Enumerable.Repeat(new string('.', 320), 20))) with { Zone = new Box(0, 0, 15, 19) };
        var w = new Scenario(layout).Do(new Crew.Enter(10 * 320 + 2)).GiveItems(0, (Charge, 1)).Run();
        var debris = (DebrisState)w.M.Debris.Slice;
        var struck = new Piece { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = "engineBlock", X = 30_000, Y = 10_000, Start = 30_000, SeenAt = debris.Clock };
        debris.Pieces.Add(struck);
        SentryTests.Guarded(w);
        var end = Thrown(w, At(10, 10, Right));
        Assert.True(end.Met);
        Assert.True(end.Burst >= 0);
        Assert.Equal(Spec.Shots.DamageOf(Charge, Charge) + Blast.WreckDamage, struck.Damage);
    }

    [Fact]
    public void AWreckItsHitBreaksStopsItThereNeverFlownThrough()
    {
        var w = ShotsTests.Space().GiveItems(0, (Charge, 1));
        var debris = (DebrisState)w.M.Debris.Slice;
        Piece Wreck(int x) => new() { Id = debris.NextId++, Zone = Spec.Debris.Far.Id, Item = "engineBlock", X = x, Y = 10_000, Start = x, SeenAt = debris.Clock };
        // The first all but broken, so the charge's own hit breaks it; the second behind it, past the burst's reach.
        var weak = Wreck(30_000);
        weak.Damage = Spec.Wreckage.HpOf(weak.Item)!.Value - 1;
        var behind = Wreck(80_000);
        debris.Pieces.AddRange([weak, behind]);
        SentryTests.Guarded(w);
        var end = Thrown(w, At(10, 10, Right));
        Assert.True(end.Met);
        Assert.True(end.Burst >= 0 && end.Burst % 320 < 30, "it bursts where it struck the first");
        Assert.DoesNotContain(weak, debris.Pieces);
        Assert.Equal(0, behind.Damage);
    }

    [Fact]
    public void InAVesselItBurstsOnItsHullInFull()
    {
        var w = new Scenario(Registry.Wreck);
        w.Do(new Places.Find("shuttle")).Run();
        w.Do(new Places.Jump(w.M.Places.Contacts[^1].Id)).Run();
        var box = w.M.Grid.Reserved!.Value;
        int width = w.M.Grid.Width;
        int hull = Enumerable.Range(box.Top, box.Bottom - box.Top + 1).Select(y => y * width + box.Left).First(c => w.M.Grid.WallAt(c) != null);
        w.Do(new Crew.Enter(hull + 5)).GiveItems(0, (Charge, 1)).Run();
        var end = Thrown(w, new Fire(Charge, (hull % width + 3) * Unit + Unit / 2, hull / width * Unit + Unit / 2, Spec.Headings.Turn / 2));
        Assert.True(end.Burst >= 0);
        Assert.True(w.M.Grid.WallAt(hull) == null || CrackAt(w, hull) > 0);
    }

    [Fact]
    public void ThrownAtTheLifepodItBurstsShortOfIt()
    {
        // The pod with a wall flush behind it: a charge that flew into the pod ended in it, bursting nowhere.
        var w = new Scenario(Layout.Parse("""
            ..........OOOOOO#...
            ..........OOOOOO#...
            ..........OOOOOO#...
            ..........OOOOOO#...
            ..........OOOOOO#...
            ..........OOOOOO#...
            ####################
            """)).Do(new Crew.Enter(5 * 20 + 2)).GiveItems(0, (Charge, 1)).Run();
        var end = Thrown(w, new Fire(Charge, 3 * Unit + Unit / 2, 2 * Unit + Unit / 2, Right));
        Assert.True(end.Met);
        Assert.True(end.Burst >= 0);
        Assert.True(end.Burst % 20 < 10, "it bursts short of the pod");
        Assert.NotEmpty(w.M.Building.Cracks);
    }

    [Fact]
    public void WithNoChargeItIsRefused()
    {
        var w = Pit('t');
        w.Do(At(20, 8, Down) with { From = 1 }).Run();
        Assert.Equal("creatures.noGun", w.Refusals.Single().Reason);
        Assert.Empty(w.M.Creatures.Shots);
    }
}
