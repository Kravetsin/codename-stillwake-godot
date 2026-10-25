using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>What grows in a planet's caves (0080): grown from the seed by depth band, on what holds it, felled with it.</summary>
public class CaveFloraTests
{
    private static Arbitrary<uint> Seeds => Gen.Choose(0, int.MaxValue).Select(s => (uint)s * 2654435761u).ToArbitrary();
    private static readonly int Stone = Materials.Get("stone").Code;

    private static IEnumerable<(int Cell, Flora.CaveKind Kind)> CaveGrowth(Layout l) =>
        l.Flora!.Select(p => (p.Cell, Kind: Flora.Cave.FirstOrDefault(k => k.Id == p.Kind)!)).Where(p => p.Kind != null && !Ruins.Growths.Contains(p.Kind.Id));

    /// <summary>
    /// Whether every cave growth of <paramref name="l"/> is where it may be, in each of its <c>Across</c> columns: in a cave — an
    /// open cell with a back wall, its <c>Tall</c> cells open, on a whole wall or under one — or in stone, its block beside a cave cell (0104); in a band it lives in; roots near
    /// the surface (but under a spire, whose tip stands over the ground the roots grew under, 0085); and no two in one cell. A
    /// home's collapsed end is grown over by its prefab, not the caves (0091): what lies in a home's box is the home's.
    /// </summary>
    private static string? Misplaced(Layout l, PlanetType t, IReadOnlyList<(int X, int Y)>? spires = null, IReadOnlyList<(int X, int Y, Ruins.Home Home)>? homes = null)
    {
        int w = l.Width;
        char At(int cell) => cell < 0 || cell >= w * l.Height ? '#' : l.Rows[cell / w][cell % w];
        var cells = new HashSet<int>();
        foreach (var (cell, kind) in CaveGrowth(l))
        {
            int x = cell % w, y = cell / w;
            if (homes?.Any(h => x >= h.X && x < h.X + h.Home.Wide && y >= h.Y && y < h.Y + h.Home.Tall) == true) continue;
            string where = $"{kind.Id} at ({x}, {y})";
            if (kind.Chances[Terrain.BandOf(t, y)] == 0) return $"{where}: not in its band";
            switch (kind.On)
            {
                case Flora.Anchor.Rock:
                    var block = Enumerable.Range(0, kind.Across).SelectMany(dx => Enumerable.Range(0, kind.Tall).Select(i => cell + i * w + dx)).ToList();
                    if (block.Any(c => At(c) != '#' || l.Materials![c] != Stone)) return $"{where}: not stone";
                    if (!block.Any(c => new[] { c - w, c + w, c - 1, c + 1 }.Any(n => At(n) == '.'))) return $"{where}: not beside a cave";
                    if (!block.All(cells.Add)) return $"{where}: shares a cell";
                    if (kind.Floors && At(cell - w) != '.') return $"{where}: not in a floor";
                    break;
                default:
                    int step = kind.On == Flora.Anchor.Floor ? -w : w;
                    // On walls only, and room either side.
                    for (int dx = -kind.Wide; dx < kind.Across + kind.Wide; dx++)
                        if (At(cell - step + dx) != '#' || At(cell + dx) != '.') return $"{where}: nothing whole holds its column {dx}";
                    for (int dx = 0; dx < kind.Across; dx++)
                        for (int i = 0; i < kind.Tall; i++)
                            if (At(cell + i * step + dx) != '.' || !cells.Add(cell + i * step + dx)) return $"{where}: its cell ({dx}, {i}) not open and its own";
                    break;
            }
            bool spired = spires?.Any(s => x >= s.X && x < s.X + Ruins.SpireWide) == true;
            if (kind.Shallow is int shallow && !spired && y - Enumerable.Range(0, l.Height).First(r => l.Rows[r][x] != ' ') > shallow) return $"{where}: too deep";
        }
        return null;
    }

    [Property(MaxTest = 60)]
    public Property CaveGrowthIsOnlyInCavesOnWhatHoldsItInItsBand() =>
        Prop.ForAll(Seeds, seed => Misplaced(Terrain.Generate(PlanetTypes.Small, seed, out _, out _, out var spires, out var homes), PlanetTypes.Small, spires, homes) is not { } why
            ? true.ToProperty() : false.Label(why));

    [Fact]
    public void TheFirstPlanetGrowsEveryCaveKindWhereItMayAndEachBandItsOwn()
    {
        var l = Terrain.Generate(PlanetTypes.First, 1, out _, out _, out var spires, out var homes);
        Assert.Null(Misplaced(l, PlanetTypes.First, spires, homes));
        Assert.All(Flora.Cave, k => Assert.Contains(l.Flora!, p => p.Kind == k.Id));
        // The cold band's crystals are ice, the deep's amber, the ember band's ember: each band has its own look.
        var byBand = CaveGrowth(l).GroupBy(p => Terrain.BandOf(PlanetTypes.First, p.Cell / l.Width)).ToDictionary(b => b.Key, b => b.Select(p => p.Kind.Id).ToHashSet());
        Assert.Contains("crystalIce", byBand[0]);
        Assert.Contains("roots", byBand[0]);
        Assert.DoesNotContain("crystalAmber", byBand[0]);
        Assert.Contains("threadViolet", byBand[1]);
        Assert.Contains("glowcapTeal", byBand[1]);
        Assert.Contains("crystalAmber", byBand[2]);
        Assert.DoesNotContain("crystalIce", byBand[2]);
        Assert.Contains("heatCrack", byBand[3]);
        Assert.Equal(l.Flora!.OrderBy(p => p.Cell), l.Flora!);
    }

    [Property(MaxTest = 30)]
    public Property TheSameSeedGrowsTheSameCavesAndKindsKeepTheirDistanceAlongARow() => Prop.ForAll(Seeds, seed =>
    {
        var (a, b) = (Terrain.Generate(PlanetTypes.Small, seed), Terrain.Generate(PlanetTypes.Small, seed));
        bool apart = Flora.Cave.All(k => CaveGrowth(a).Where(p => p.Kind == k).GroupBy(p => p.Cell / a.Width)
            .All(row => row.Select(p => p.Cell).Order().Zip(row.Select(p => p.Cell).Order().Skip(1)).All(pair => pair.Second - pair.First >= k.Apart)));
        return a.Flora!.SequenceEqual(b.Flora!) && apart;
    });

    [Fact]
    public void AnotherSeedGrowsOtherCaves() =>
        Assert.False(CaveGrowth(Terrain.Generate(PlanetTypes.First, 1)).SequenceEqual(CaveGrowth(Terrain.Generate(PlanetTypes.First, 2))));

    [Fact]
    public void TheGreatHallsAreGardensAndTheTunnelsSparse()
    {
        var l = Terrain.Generate(PlanetTypes.First, 1, out var hall);
        int w = l.Width;
        // Growth per slot: an open cave cell with a wall under or over it, in a hall and out of one.
        bool Slot(int cell) => l.At(cell) == '.' && (l.At(cell + w) == '#' || l.At(cell - w) == '#');
        var grown = CaveGrowth(l).Where(p => p.Kind.On != Flora.Anchor.Rock).Select(p => p.Cell).ToHashSet();
        var slots = Enumerable.Range(w, w * (l.Height - 2)).Where(Slot).ToList();
        double Density(bool inHall)
        {
            var these = slots.Where(c => hall[c] == inHall).ToList();
            return (double)these.Count(grown.Contains) / these.Count;
        }
        Assert.True(Density(true) > 2 * Density(false), $"halls {Density(true):F3}, elsewhere {Density(false):F3}");
    }

    /// <summary>
    /// A cave in stone: a glowcap on its floor at (2, 2), a stalactite from its ceiling at (5, 1), moss in the wall at (7, 2),
    /// threads two characters long hanging at (4, 1); doubled.
    /// </summary>
    private static Scenario Cave()
    {
        var layout = Layout.Parse("""
            ########
            #......#
            #......#
            ########
            """).WithMaterials("""
            tttttttt
            t......t
            t......t
            tttttttt
            """) with { Flora = [(1 * 8 + 4, "threadViolet"), (1 * 8 + 5, "stalactite"), (2 * 8 + 2, "glowcapTeal"), (2 * 8 + 7, "moss")] };
        return new Scenario(layout, doubled: true);
    }

    /// <summary>The lower row of the drawn character at (<paramref name="x"/>, <paramref name="y"/>): the wall a ceiling's growth hangs from.</summary>
    private static int Bottom(Scenario w, int x, int y) => w.At(x, y) + w.Layout.Width;

    /// <summary>The axe's hits that break stone.</summary>
    private static readonly int Hits = (Materials.Get("stone").Strength + Weapons.Axe.BlockDamage - 1) / Weapons.Axe.BlockDamage;

    private static string[] Kinds(Scenario w) => w.M.Building.Plants.Select(p => p.Kind).ToArray();

    [Fact]
    public void AHitOnAGrowthFellsItAndOnHangingThreadsAnyCellTheyHangIn()
    {
        var w = Cave();
        w.Do(HewTesting.Dig(w, w.At(2, 2), Weapons.Axe.Item)).Do(HewTesting.Dig(w, w.At(4, 2), Weapons.Axe.Item)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(["stalactite", "moss"], Kinds(w));
        // Felled at a hit: no plant keeps a crack, whatever wall the fans cracked besides.
        Assert.All(w.M.Building.Cracks, c => Assert.NotNull(w.M.Grid.WallAt(c.Cell)));
    }

    [Fact]
    public void AGrowthGoesWithWhatHoldsItTheTickThatBreaks()
    {
        var w = Cave();
        // The floor under the glowcap, the ceiling over the stalactite, the stone the moss is in, hit together till they break.
        int[] holds = [w.At(2, 3), Bottom(w, 5, 0), w.At(7, 2)];
        for (int hit = 1; hit < Hits; hit++)
        {
            foreach (int cell in holds) w.Do(HewTesting.Dig(w, cell, Weapons.Axe.Item));
            Assert.Equal(4, w.Run().M.Building.Plants.Count);
        }
        foreach (int cell in holds) w.Do(HewTesting.Dig(w, cell, Weapons.Axe.Item));
        w.Run();
        Assert.Equal(["threadViolet"], Kinds(w));
    }


    [Fact]
    public void AHitOnStoneWithMossInItDigsTheStoneNotTheMoss()
    {
        var w = Cave();
        w.Do(HewTesting.Dig(w, w.At(7, 2), Weapons.Axe.Item)).Run();
        Assert.Single(w.M.Building.Cracks);
        Assert.Contains("moss", Kinds(w));
    }

    private static Scenario Down()
    {
        var w = new Scenario("""
            ......
            .S....
            ######
            """);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.Do(new OpenSurface(PlanetTypes.First.Id)).Run();
        return w.Do(new Transfer(1)).Run();
    }

    [Fact]
    public void APlanetsCavesSaveNothingButWhatWasFelled()
    {
        var w = Down();
        var b = w.On(1).Building;
        var cave = b.Plants.Where(p => Flora.Get(p.Kind) is Flora.CaveKind).ToList();
        Assert.True(cave.Count > 10000);
        JsonNode Building(string save) => JsonNode.Parse(save)!["surfaces"]![0]!["slices"]!["building"]!;
        string fresh = w.World.Save();
        Assert.Empty(Building(fresh)["felled"]!.AsArray());
        Assert.True(fresh.Length < 16 * 1024, $"{fresh.Length} bytes");
        // A glowcap hit, and the stone a moss lies in dug out.
        var cap = cave.First(p => p.Kind == "glowcapTeal");
        var moss = cave.First(p => p.Kind == "moss");
        w.Do(HewTesting.Dig(w, cap.Cell, Weapons.Axe.Item, surface: 1)).Run();
        for (int i = 0; i < Hits; i++) w.Do(HewTesting.Dig(w, moss.Cell, Weapons.Axe.Item, surface: 1)).Run();
        var felled = Building(w.World.Save())["felled"]!.AsArray().Select(n => (int)n!).ToArray();
        Assert.Equal(new[] { cap.Cell, moss.Cell }.Order(), felled);
        w.Reload();
        Assert.Equal(b.Plants.Count, w.On(1).Building.Plants.Count);
        Assert.DoesNotContain(w.On(1).Building.Plants, p => felled.Contains(p.Cell));
    }
}
