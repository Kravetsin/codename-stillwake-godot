using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The fog at a planet's sides (fog-edges): bands of bedrock under the open sky, outside the build zone, so nothing is dug or
/// built there; the walk out to a band's middle a stair; the zone kept across a load; and no cold in them.
/// </summary>
public class FogBandsTests
{
    private static int Code(string material) => Materials.Get(material).Code;

    /// <summary>The top of the ground in column <paramref name="x"/>: its first solid cell.</summary>
    private static int TopOf(Layout l, int x) => Enumerable.Range(0, l.Height).First(y => l.Rows[y][x] == '#');

    [Fact]
    public void EveryPlanetsFogIsEvenWiderThanItsBedrockAndAFractionOfItsWidth()
    {
        foreach (var g in PlanetTypes.All.Select(t => t.Ground))
        {
            Assert.Equal(0, g.Fog % 2);
            Assert.True(g.Fog >= g.BedrockSide + 2, $"{g.Fog} columns of fog over {g.BedrockSide} of bedrock");
            Assert.True(2 * g.Fog * 4 < g.Width, $"{g.Fog} columns of fog on a planet {g.Width} wide");
        }
    }

    [Fact]
    public void EachBandIsBedrockUnderTheSkyWithNothingGrownOrStampedAndOutsideTheZone()
    {
        foreach (var type in PlanetTypes.All)
            foreach (uint seed in new uint[] { 1, 2, 3 })
            {
                var g = type.Ground;
                var l = Terrain.Generate(type, seed, out bool[] hall, out _, out var spires, out var homes);
                int w = l.Width, floor = l.Height - g.CoreRows;
                string at = $"{type.Id}, seed {seed}";
                Assert.Equal(new Box(g.Fog, 0, w - 1 - g.Fog, l.Height - 1), l.Zone);
                var band = Enumerable.Range(g.BedrockSide, g.Fog - g.BedrockSide).Concat(Enumerable.Range(w - g.Fog, g.Fog - g.BedrockSide));
                foreach (int x in band)
                {
                    int top = TopOf(l, x);
                    for (int y = 0; y < floor; y++)
                    {
                        int cell = y * w + x;
                        if (y < top) Assert.True(l.Rows[y][x] == ' ' && l.Materials![cell] == 0, $"{at}: ({x}, {y}) is not sky");
                        else Assert.True(l.Rows[y][x] == '#' && l.Materials![cell] == Code("bedrock"), $"{at}: ({x}, {y}) is not bedrock");
                        Assert.False(hall[cell], $"{at}: ({x}, {y}) is a hall's");
                    }
                }
                Assert.DoesNotContain(l.Flora!, f => f.Cell % w < g.Fog || f.Cell % w >= w - g.Fog);
                Assert.All(spires, s => Assert.InRange(s.X, g.Fog + 1, w - g.Fog - Ruins.SpireWide - 1));
                Assert.All(homes, h => Assert.InRange(h.X, g.Fog + 1, w - g.Fog - h.Home.Wide - 1));
                // The walk from the zone's side out to each band's middle: no step of more than a row.
                foreach (var (from, to) in new[] { (g.Fog / 2, g.Fog), (w - 1 - g.Fog, w - g.Fog / 2) })
                    for (int x = from; x < to; x++)
                        Assert.True(Math.Abs(TopOf(l, x) - TopOf(l, x + 1)) <= 1, $"{at}: a step of more than a row at column {x}");
            }
    }

    /// <summary>A hero on the small planet, beamed down to its spawn.</summary>
    private static Scenario Down()
    {
        var w = new Scenario("""
            ......
            .S....
            ######
            """);
        w.Do(new Enter(w.M.Crew.Spawn)).Run();
        w.Do(new OpenSurface(PlanetTypes.Small.Id)).Run();
        return w.Do(new Transfer(1)).Run();
    }

    /// <summary>A cell of the small planet's left band: the ground's top, halfway out.</summary>
    private static int BandGround(Registry m)
    {
        int w = m.Grid.Width, x = PlanetTypes.Small.Ground.Fog / 2;
        int y = Enumerable.Range(0, m.Grid.Height).First(y => m.Grid.StructureAt(y * w + x) != Structure.Empty);
        return y * w + x;
    }

    [Fact]
    public void AHitInABandStrikesNothingAndAPlanThereIsRefusedOutsideTheZone()
    {
        var w = Down();
        var m = w.On(1);
        int ground = BandGround(m);
        w.Do(HewTesting.Dig(w, ground, Weapons.Axe.Item, surface: 1)).Run();
        w.Do(new PlacePlan(ground - m.Grid.Width, Placement.Wall())).Run();
        Assert.Equal(["building.nothingToDig", "grid.outsideZone"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(Structure.Wall, m.Grid.StructureAt(ground));
    }

    [Fact]
    public void ThePlanetsZoneIsTheColumnsBetweenTheBandsAndStaysSoAcrossALoad()
    {
        var w = Down();
        var g = PlanetTypes.Small.Ground;
        var zone = new Box(g.Fog, 0, g.Width - 1 - g.Fog, g.Height - 1);
        Assert.Equal(zone, w.On(1).Grid.Zone);
        w.Reload();
        Assert.Equal(zone, w.On(1).Grid.Zone);
    }

    [Fact]
    public void NoCellOfEitherPlanetPaysTheCold()
    {
        foreach (var type in PlanetTypes.All)
        {
            var w = new Scenario("""
                ......
                .S....
                ######
                """);
            w.Do(new Enter(w.M.Crew.Spawn)).Run();
            w.Do(new OpenSurface(type.Id)).Run();
            var m = w.On(1);
            int width = m.Grid.Width, height = m.Grid.Height;
            // The cost grows toward the world's edge by the deeper axis: none on the outermost cells, none inside.
            var ring = Enumerable.Range(0, width).SelectMany(x => new[] { x, (height - 1) * width + x })
                .Concat(Enumerable.Range(0, height).SelectMany(y => new[] { y * width, y * width + width - 1 }));
            Assert.All(ring, cell => Assert.Equal(0, m.Crew.EdgeCost(cell)));
        }
    }

    /// <summary>
    /// Whatever is asked on the small planet — the modules' own commands, hits and plans on any cell, the bands' included —
    /// no cell of a band changes.
    /// </summary>
    [Property(MaxTest = 20)]
    public Property NoCommandChangesACellOfABand()
    {
        var gen = from seed in Gen.Choose(1, 100_000) from ticks in Gen.Choose(20, 120) select (seed, ticks);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var w = Down();
            var m = w.On(1);
            int width = m.Grid.Width, fog = PlanetTypes.Small.Ground.Fog;
            var band = Enumerable.Range(0, width * m.Grid.Height).Where(c => c % width < fog || c % width >= width - fog).ToList();
            int[] Layers(int c) => [m.Grid.StructureLayer[c], m.Grid.MaterialLayer[c], m.Grid.BackLayer[c], m.Grid.BackMaterialLayer[c]];
            var before = band.Select(Layers).ToList();
            // The axe needs no power: its hits go at the ground's top in a few band columns, often enough to break stone there.
            int TopCell(int x) => Enumerable.Range(0, m.Grid.Height).First(y => m.Grid.StructureAt(y * width + x) != Structure.Empty) * width + x;
            int[] hit = [TopCell(fog / 2), TopCell(fog - 1), TopCell(width - fog), TopCell(width - 1 - fog / 2)];
            var rng = new Rng(new RngState { S = (uint)t.seed });
            for (int i = 0; i < t.ticks; i++)
            {
                foreach (var c in ModulesTests.RandomCommands(w.World, rng)) w.Do(c);
                w.Do(HewTesting.Dig(w, hit[rng.Below((uint)hit.Length)], Weapons.Axe.Item, surface: 1));
                w.Do(new PlacePlan(band[(int)rng.Below((uint)band.Count)], Placement.Wall())).Run();
            }
            for (int i = 0; i < band.Count; i++)
                if (!Layers(band[i]).SequenceEqual(before[i])) return false.Label($"cell {band[i]} of a band changed");
            return true.ToProperty();
        });
    }
}
