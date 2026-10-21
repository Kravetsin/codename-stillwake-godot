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
    public Property PlantsGrowOnlyOnWholeSoilOpenToTheSkyNeverInACave() => Prop.ForAll(Seeds, seed => OnTheSurface(Terrain.Generate(Ground.Small, seed)));

    [Fact]
    public void TheFirstPlanetGrowsEveryKindOnItsSurfaceAndNoneInItsCaves()
    {
        var l = Terrain.Generate(Ground.Planet, 1);
        Assert.True(OnTheSurface(l));
        Assert.All(Flora.All, k => Assert.Contains(l.Flora!, p => p.Kind == k.Id));
        // Most columns of open soil stay bare: a few hundred plants across two thousand columns.
        Assert.InRange(Surface(l.Flora!).Count(), 200, 900);
        Assert.Equal(l.Flora!.OrderBy(p => p.Cell), l.Flora!);
    }

    [Property(MaxTest = 60)]
    public Property TheSameSeedGrowsTheSamePlantsAndKindsKeepTheirDistance() => Prop.ForAll(Seeds, seed =>
    {
        var (a, b) = (Terrain.Generate(Ground.Small, seed), Terrain.Generate(Ground.Small, seed));
        bool apart = Flora.All.All(k =>
        {
            var xs = Surface(a.Flora!).Where(p => p.Kind == k.Id).Select(p => p.Cell % a.Width).Order().ToList();
            return xs.Zip(xs.Skip(1)).All(pair => pair.Second - pair.First >= k.Apart);
        });
        return a.Flora!.SequenceEqual(b.Flora!) && apart;
    });

    [Fact]
    public void AnotherSeedGrowsOtherPlants() =>
        Assert.False(Terrain.Generate(Ground.Planet, 1).Flora!.SequenceEqual(Terrain.Generate(Ground.Planet, 2).Flora!));

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
        return new Scenario(layout);
    }

    private static string[] Kinds(Scenario w) => w.M.Building.Plants.Select(p => p.Kind).ToArray();

    [Fact]
    public void AHitOnAnyCellAPlantStandsInFellsItAndNothingElse()
    {
        var w = Meadow();
        Assert.Equal(["bellTeal", "acacia"], Kinds(w));
        // The acacia's trunk, a row above its own cell.
        Assert.Equal("acacia", w.M.Building.PlantAt(5)?.Kind);
        w.Do(new Dig(5, Weapons.Axe.Item)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(["bellTeal"], Kinds(w));
        Assert.Null(w.M.Building.PlantAt(5));
        // Nothing but the plant: the ground under it stands, uncracked, and only the tree's planks were given (0080).
        Assert.Equal("soil", w.M.Grid.WallAt(2 * 8 + 5)?.Id);
        Assert.Empty(w.M.Building.Cracks);
        Assert.Equal(Flora.Get("acacia").Yields, w.M.Items.Totals(0));
    }

    [Fact]
    public void AHitOnOpenGroundWithNoPlantStillDigsNothing()
    {
        var w = Meadow();
        w.Do(new Dig(1 * 8 + 3, Weapons.Axe.Item)).Run();
        Assert.Equal(["building.nothingToDig"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(2, w.M.Building.Plants.Count);
    }

    [Fact]
    public void APlantGoesWithTheGroundUnderItOnTheTickItBreaks()
    {
        var w = Meadow();
        int under = 2 * 8 + 2;
        w.Do(new Dig(under, Weapons.Axe.Item)).Run();
        Assert.Equal(2, w.M.Building.Plants.Count);
        w.Do(new Dig(under, Weapons.Axe.Item)).Run();
        Assert.Null(w.M.Grid.WallAt(under));
        Assert.Equal(["acacia"], Kinds(w));
    }

    [Fact]
    public void APlantGoesWhenSomethingIsBuiltInItsCell()
    {
        var w = Meadow();
        w.Do(new SetCreative(true)).Run();
        w.Do(new PlacePlan(1 * 8 + 2, new Placement("structure", "ladder"))).Run();
        int plan = w.M.Building.Plans.Single().Id;
        for (int i = 0; i < 100 && w.M.Building.Plans.Count > 0; i++) w.Do(new Work(plan)).Run();
        Assert.Equal(Structure.Ladder, w.M.Grid.StructureAt(1 * 8 + 2));
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
        w.Do(new OpenSurface(Surfaces.Planet.Id)).Run();
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
        // Three felled, one of them by the ground under it.
        var (a, c, d) = (all[0], all[all.Count / 2], all[^1]);
        w.Do(new Dig(a.Cell, Weapons.Axe.Item)).Do(new Dig(c.Cell, Weapons.Axe.Item)).Run();
        for (int i = 0; i < 2; i++) w.Do(new Dig(d.Cell + w.On(1).Grid.Width, Weapons.Axe.Item)).Run();
        var felled = Building(w.World.Save())["felled"]!.AsArray().Select(n => (int)n!).ToArray();
        Assert.Equal(new[] { a.Cell, c.Cell, d.Cell }, felled);
        w.Reload();
        Assert.Equal(all.Where(p => !felled.Contains(p.Cell)), Surface(w.On(1).Building.Plants));
    }
}
