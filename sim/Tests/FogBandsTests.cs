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
/// The fog at a planet's sides (fog-edges): hollow bands, the open sky over their ground line and open air under it before a
/// bedrock back wall, outside the build zone, so nothing is dug or built there; their line beside the zone a drop's height
/// under the sky and level with the zone's ground; the zone kept across a load; and no cold in them.
/// </summary>
public class FogBandsTests
{
    private static int Code(string material) => Materials.Get(material).Code;

    /// <summary>The hero's height in whole rows, rounded up.</summary>
    private static readonly int HeroRows = (Content.Hero.Height + 999) / 1000;

    /// <summary>The top of the ground in column <paramref name="x"/>: its first solid cell.</summary>
    private static int TopOf(Layout l, int x) => Enumerable.Range(0, l.Height).First(y => l.Rows[y][x] == '#');

    /// <summary>A band column's ground line: its first row that is not open sky.</summary>
    private static int LineOf(Layout l, int x) => Enumerable.Range(0, l.Height).First(y => l.Rows[y][x] != ' ');

    [Fact]
    public void EveryPlanetsFogIsEvenWiderThanItsBedrockAFractionOfItsWidthAndDropsAHerosHeight()
    {
        foreach (var g in PlanetTypes.All.Select(t => t.Ground))
        {
            Assert.Equal(0, g.Fog % 2);
            Assert.True(g.Fog >= g.BedrockSide + 2, $"{g.Fog} columns of fog over {g.BedrockSide} of bedrock");
            Assert.True(2 * g.Fog * 4 < g.Width, $"{g.Fog} columns of fog on a planet {g.Width} wide");
            Assert.True(g.FogDrop >= HeroRows, $"a drop of {g.FogDrop} rows under a hero {HeroRows} tall");
        }
    }

    [Fact]
    public void EachBandIsHollowWithNothingGrownOrStampedAndOutsideTheZone()
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
                    int line = LineOf(l, x);
                    for (int y = 0; y < floor; y++)
                    {
                        int cell = y * w + x;
                        if (y < line) Assert.True(l.Rows[y][x] == ' ' && l.Materials![cell] == 0, $"{at}: ({x}, {y}) is not sky");
                        else Assert.True(l.Rows[y][x] == '.' && l.Materials![cell] == Code("bedrock"), $"{at}: ({x}, {y}) is not open before bedrock");
                        Assert.False(hall[cell], $"{at}: ({x}, {y}) is a hall's");
                    }
                }
                Assert.DoesNotContain(l.Flora!, f => f.Cell % w < g.Fog || f.Cell % w >= w - g.Fog);
                // No cave growth faces a band's hollow: each keeps a column off the zone's side.
                Assert.All(l.Flora!.Where(f => Content.Flora.Cave.Any(k => k.Id == f.Kind)), f => Assert.InRange(f.Cell % w, g.Fog + 1, w - g.Fog - 3));
                Assert.All(spires, s => Assert.InRange(s.X, g.Fog + 1, w - g.Fog - Ruins.SpireWide - 1));
                Assert.All(homes, h => Assert.InRange(h.X, g.Fog + 1, w - g.Fog - h.Home.Wide - 1));
                // Beside the zone a band's line is level with the zone's ground and a drop under the sky: a moved hero fits.
                foreach (var (inner, side) in new[] { (g.Fog - 1, g.Fog), (w - g.Fog, w - 1 - g.Fog) })
                {
                    int line = LineOf(l, inner);
                    Assert.True(Math.Abs(line - TopOf(l, side)) <= 1, $"{at}: the line at column {inner} is not level with the zone's ground");
                    Assert.True(line - g.FogDrop >= HeroRows, $"{at}: no room for a hero dropped at column {inner}");
                }
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

    /// <summary>The cell on a band column's ground line: its first with a back wall.</summary>
    private static int LineCell(Registry m, int x)
    {
        int w = m.Grid.Width;
        return Enumerable.Range(0, m.Grid.Height).First(y => m.Grid.BackLayer[y * w + x] != Back.None) * w + x;
    }

    [Fact]
    public void AHitInABandAimedOutwardStrikesNothingAndAPlanThereIsRefusedOutsideTheZone()
    {
        var w = Down();
        var m = w.On(1);
        int cell = LineCell(m, PlanetTypes.Small.Ground.Fog / 2);
        // Aimed left, out of the zone: through the band's open air to its unbreakable bedrock.
        w.Do(HewTesting.Dig(w, cell, Weapons.Axe.Item, heading: Headings.Turn / 2, surface: 1)).Run();
        w.Do(new PlacePlan(cell, Placement.Wall())).Run();
        Assert.Equal(["building.nothingToDig", "grid.outsideZone"], w.Refusals.Select(r => r.Reason));
        Assert.Equal(Structure.Empty, m.Grid.StructureAt(cell));
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
            // The axe needs no power: its hits go along the ground line of a few band columns, often enough to break what is there.
            int[] hit = [LineCell(m, fog / 2), LineCell(m, fog - 1), LineCell(m, width - fog), LineCell(m, width - 1 - fog / 2)];
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
