using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>What grows on a planet's surface (0079): grown from the seed on open soil, felled by a hit or with its ground.</summary>
public class FloraTests
{
    private static Arbitrary<uint> Seeds => Gen.Choose(0, int.MaxValue).Select(s => (uint)s * 2654435761u).ToArbitrary();
    private static readonly int Soil = Materials.Get("soil").Code;

    /// <summary>Whether every plant of <paramref name="l"/> stands on the surface: an open cell under the sky, over whole soil.</summary>
    private static bool OnTheSurface(Layout l) => Surface(l.Flora!).All(p =>
    {
        int x = p.Cell % l.Width, y = p.Cell / l.Width;
        return y + 1 < l.Height && l.Rows[y + 1][x] == '#' && l.Materials![p.Cell + l.Width] == Soil
            && Enumerable.Range(0, y + 1).All(above => l.Rows[above][x] == ' ');
    });

    /// <summary>The plants of the surface among <paramref name="growth"/>, the caves' left out (0080).</summary>
    private static IEnumerable<(int Cell, string Kind)> Surface(IEnumerable<(int Cell, string Kind)> growth) =>
        growth.Where(p => Flora.Get(p.Kind) is Flora.Kind);

    private static IEnumerable<Plant> Surface(IEnumerable<Plant> growth) => growth.Where(p => Flora.Get(p.Kind) is Flora.Kind);

    [Property(MaxTest = 60)]
    public Property PlantsGrowOnlyOnWholeSoilOpenToTheSkyNeverInACave() => Prop.ForAll(Seeds, seed => OnTheSurface(Terrain.Generate(PlanetTypes.Small, seed)));

    [Fact]
    public void TheFirstPlanetGrowsEveryKindOnItsSurfaceAndNoneInItsCaves()
    {
        var l = Terrain.Generate(PlanetTypes.First, 1);
        Assert.True(OnTheSurface(l));
        Assert.All(Flora.All, k => Assert.Contains(l.Flora!, p => p.Kind == k.Id));
        // Most columns of open soil stay bare: a few hundred plants across two thousand columns.
        Assert.InRange(Surface(l.Flora!).Count(), 200, 900);
    }

    /// <summary>
    /// The surface's density, measured on the first planet's seed 1, against what it first grew (0079: 18 acacias, 21 bushes, 14
    /// rocks, 139 bells): the flowers two to three and a half times as many, so a walk passes them often; trees, bushes and rocks at least as
    /// many, spacing keeping them apart. What grows, before the spires clear their boxes (0085).
    /// </summary>
    [Fact]
    public void TheFirstPlanetsSurfaceGrowsAtLeastItsFirstTreesBushesAndRocksAndTwoToThreeTimesItsFlowers()
    {
        var l = Terrain.Generate(PlanetTypes.First with { Ground = PlanetTypes.First.Ground with { Spires = 0 } }, 1);
        int Of(params string[] kinds) => l.Flora!.Count(p => kinds.Contains(p.Kind));
        // At 8 px cells the ground steps twice as finely, so the level ground a tree, a bush or a rock needs is rarer (0104):
        // they grow about half as often as at 16 px, the flowers, needing none, as often; more since every step of a hill is
        // whole soil, no slope (0106); more again on the plains' level ground (veins-and-stairs).
        Assert.InRange(Of("acacia"), 18, 4 * 18);
        Assert.InRange(Of("bush"), 21, 4 * 21);
        Assert.InRange(Of("rock"), 14, 4 * 14);
        Assert.InRange(Of("bellTeal", "bellViolet", "bellAmber"), 2 * 139, 7 * 139 / 2);
        Assert.Equal(l.Flora!.OrderBy(p => p.Cell), l.Flora!);
    }

    [Property(MaxTest = 60)]
    public Property TheSameSeedGrowsTheSamePlantsAndKindsKeepTheirDistance() => Prop.ForAll(Seeds, seed =>
    {
        var (a, b) = (Terrain.Generate(PlanetTypes.Small, seed), Terrain.Generate(PlanetTypes.Small, seed));
        bool apart = Flora.All.All(k =>
        {
            var xs = Surface(a.Flora!).Where(p => p.Kind == k.Id).Select(p => p.Cell % a.Width).Order().ToList();
            return xs.Zip(xs.Skip(1)).All(pair => pair.Second - pair.First >= k.Apart);
        });
        return a.Flora!.SequenceEqual(b.Flora!) && apart;
    });

    [Fact]
    public void AnotherSeedGrowsOtherPlants() =>
        Assert.False(Terrain.Generate(PlanetTypes.First, 1).Flora!.SequenceEqual(Terrain.Generate(PlanetTypes.First, 2).Flora!));

    /// <summary>Soil under open ground, with a flower at (2, 1) and an acacia at (5, 1), whose trunk stands up to the top row.</summary>
    private static Scenario Meadow()
    {
        var layout = Layout.Parse("""
            ........
            ........
            ########
            ########
            """).WithMaterials("""
            ........
            ........
            ssssssss
            tttttttt
            """) with { Flora = [(1 * 8 + 2, "bellTeal"), (1 * 8 + 5, "acacia")] };
        return new Scenario(layout, doubled: true);
    }

    /// <summary>The own cell of a plant drawn at (<paramref name="x"/>, <paramref name="y"/>): its block's bottom-left, on the ground.</summary>
    private static int Own(Scenario w, int x, int y) => w.At(x, y) + w.Layout.Width;

    private static string[] Kinds(Scenario w) => w.M.Building.Plants.Select(p => p.Kind).ToArray();

    [Fact]
    public void AHitOnAnyCellAPlantStandsInFellsItAndNothingElse()
    {
        var w = Meadow();
        Assert.Equal(["bellTeal", "acacia"], Kinds(w));
        // The acacia's trunk, a row above its own cell.
        Assert.Equal("acacia", w.M.Building.PlantAt(w.At(5, 0))?.Kind);
        // A tree takes hits to its strength (0080), each on any of its cells.
        for (int hit = 0; hit < Flora.Get("acacia").Strength / Weapons.Axe.BlockDamage; hit++) w.Do(HewTesting.Dig(w, w.At(5, 0), Weapons.Axe.Item)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(["bellTeal"], Kinds(w));
        Assert.Null(w.M.Building.PlantAt(w.At(5, 0)));
        // Nothing but the plant: the ground under it stands, uncracked, and only the tree's planks were given (0080).
        Assert.Equal("soil", w.M.Grid.WallAt(w.At(5, 2))?.Id);
        Assert.Empty(w.M.Building.Cracks);
        Assert.Equal(Flora.Get("acacia").Yields, w.M.Items.Totals(0));
    }

    [Fact]
    public void AHitIntoOpenAirWithNoPlantStillDigsNothing()
    {
        var w = Meadow();
        // From open ground straight up: the fan meets nothing before it leaves the grid.
        w.Do(HewTesting.Dig(w, w.At(3, 0), Weapons.Axe.Item, heading: HewTesting.Up)).Run();
        Assert.Equal(["building.nothingToDig"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(2, w.M.Building.Plants.Count);
    }

    [Fact]
    public void APlantGoesWithTheGroundUnderItOnTheTickItBreaks()
    {
        var w = Meadow();
        int under = w.At(2, 2);
        Assert.Equal(2, w.M.Building.Plants.Count);
        // Soil at a swing of the axe.
        w.Do(HewTesting.Dig(w, under, Weapons.Axe.Item)).Run();
        Assert.Null(w.M.Grid.WallAt(under));
        Assert.Equal(["acacia"], Kinds(w));
    }

    [Fact]
    public void APlantGoesWhenSomethingIsBuiltInItsCell()
    {
        var w = Meadow();
        w.Do(new SetCreative(true)).Run();
        // A ladder piece, two by two (0104): the block over the ground, the plant's own cell in it.
        w.Do(new PlacePlan(w.At(2, 1), new Placement("structure", "ladder"))).Run();
        int plan = w.M.Building.Plans.Single().Id;
        for (int i = 0; i < 100 && w.M.Building.Plans.Count > 0; i++) w.Do(new Work(plan)).Run();
        Assert.Equal(Structure.Ladder, w.M.Grid.StructureAt(Own(w, 2, 1)));
        Assert.Equal(["acacia"], Kinds(w));
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
    public void APlanetSavesOnlyThePlantsFelledAndGrowsTheRestAgainOnALoad()
    {
        var w = Down();
        var b = w.On(1).Building;
        var all = Surface(b.Plants).ToList();
        Assert.True(all.Count > 200);
        JsonNode Building(string save) => JsonNode.Parse(save)!["surfaces"]![0]!["slices"]!["building"]!;
        string fresh = w.World.Save();
        Assert.Empty(Building(fresh)["felled"]!.AsArray());
        Assert.True(fresh.Length < 16 * 1024, $"{fresh.Length} bytes");
        // Three felled, one of them by the ground under it; the middle one in a single hit of the axe.
        var (a, c, d) = (all[0], all.Skip(all.Count / 2).First(p => Flora.Get(p.Kind).Strength <= Weapons.Axe.BlockDamage), all[^1]);
        w.Do(HewTesting.Dig(w, a.Cell, Weapons.Axe.Item, surface: 1)).Do(HewTesting.Dig(w, c.Cell, Weapons.Axe.Item, surface: 1)).Run();
        for (int i = 0; i < 2; i++) w.Do(HewTesting.Dig(w, d.Cell + w.On(1).Grid.Width, Weapons.Axe.Item, surface: 1)).Run();
        var felled = Building(w.World.Save())["felled"]!.AsArray().Select(n => (int)n!).ToArray();
        Assert.Equal(new[] { a.Cell, c.Cell, d.Cell }, felled);
        w.Reload();
        Assert.Equal(all.Where(p => !felled.Contains(p.Cell)), Surface(w.On(1).Building.Plants));
    }
}
