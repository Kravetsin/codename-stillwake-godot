using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Landing;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Planets differ (planet-variety): the system's ores dealt once a world, each planet's ground grown with its own only, an absent
/// ore leaving plain stone and moving nothing else; caves grown by their band's temperature, the first planet's as they were.
/// </summary>
public class PlanetVarietyTests
{
    private static readonly int Stone = Materials.Get("stone").Code;

    private static List<List<string>> Deal(uint seed) => LandingModule.Dealt(new Rng(new RngState { S = seed }));

    /// <summary>
    /// For any world: the first planet holds the starting ores and mica, each other 2 to 4 more and mica, in the pool's order;
    /// every varying ore lies on some planet; the same seed deals the same.
    /// </summary>
    [Property(MaxTest = 500)]
    public Property EveryWorldDealsItsOresByTheRules() => Prop.ForAll(Gen.Choose(0, int.MaxValue).ToArbitrary(), drawn =>
    {
        uint seed = (uint)drawn;
        var sets = Deal(seed);
        var pool = Content.Landing.Ores;
        if (sets.Count != Content.Landing.Planets.Count) return false.Label("one set a planet");
        if (!sets[0].SequenceEqual(["copperOre", "ironOre", "ice", "mica"])) return false.Label($"planet 1: {string.Join(", ", sets[0])}");
        for (int p = 0; p < sets.Count; p++)
        {
            var s = sets[p];
            if (!s.SequenceEqual(pool.Where(s.Contains))) return false.Label($"planet {p + 1} out of order or repeated: {string.Join(", ", s)}");
            if (s[^1] != Content.Landing.Everywhere) return false.Label($"planet {p + 1} lacks mica");
            if (p > 0 && (s.Count - 1 < Content.Landing.OresLeast || s.Count - 1 > Content.Landing.OresMost))
                return false.Label($"planet {p + 1} holds {s.Count - 1}");
        }
        if (Content.Landing.Varying.FirstOrDefault(o => !sets.Skip(1).Any(s => s.Contains(o))) is { } missing) return false.Label($"{missing} nowhere");
        return Deal(seed).Zip(sets).All(z => z.First.SequenceEqual(z.Second)).Label("the same seed, other sets");
    });

    /// <summary>The ores lean to their types: over many worlds an icy planet holds sulfur far more often than magnetite, a scorched one the reverse.</summary>
    [Fact]
    public void OresLeanToTheirTypes()
    {
        int Count(int planet, string ore) => Enumerable.Range(1, 400).Count(s => Deal((uint)s)[planet].Contains(ore));
        Assert.True(2 * Count(4, "sulfur") > 3 * Count(4, "magnetite"));
        Assert.True(2 * Count(1, "magnetite") > 3 * Count(1, "sulfur"));
    }

    public static TheoryData<string> PoolOres() => [.. Content.Landing.Ores];

    /// <summary>
    /// A planet without one ore: none of its veins, pockets or cave growths, their cells plain stone; every other cell, growth, lake
    /// and ruin as with every ore, the ruins' loot too. Over seeds where it grows at all, so the test bites.
    /// </summary>
    [Theory]
    [MemberData(nameof(PoolOres))]
    public void AnAbsentOreGrowsNothingAndMovesNothingElse(string ore)
    {
        var without = Content.Landing.Ores.Where(o => o != ore).ToList();
        int found = 0;
        foreach (var type in new[] { PlanetTypes.Small, PlanetTypes.SmallSpire })
            for (uint seed = 1; seed <= 6; seed++)
            {
                var (all, cut) = (Terrain.Generate(type, seed), Terrain.Generate(type, seed, without));
                Assert.Equal(all.Rows, cut.Rows);
                Assert.Equal(all.Liquids, cut.Liquids);
                var kept = all.Flora!.Where(f => Flora.Get(f.Kind) is not Flora.CaveKind k || !Yields(k.Yields, ore)).ToList();
                found += all.Flora!.Count - kept.Count;
                Assert.Equal(kept, cut.Flora!);
                for (int cell = 0; cell < all.Materials!.Count; cell++)
                {
                    bool its = Materials.ByCode(all.Materials[cell]) is { Natural: true } m && Yields(m.Cost, ore) && type.Veins.Any(v => v.Material == m.Id);
                    found += its ? 1 : 0;
                    Assert.Equal(its ? Stone : all.Materials[cell], cut.Materials![cell]);
                }
            }
        Assert.True(found > 0, $"{ore} never grew: the test cannot bite");
    }

    private static bool Yields(IReadOnlyList<(string Item, int Count)> yields, string ore) => yields.Any(y => y.Item == ore);

    /// <summary>The first planet's caves grow as before temperatures: its four bands stand at the temperatures the chances are given at.</summary>
    [Fact]
    public void TheFirstPlanetsCaveChancesAreAsTheyWere()
    {
        Assert.Equal(Flora.Degrees, PlanetTypes.First.Temperatures);
        var table = Terrain.CaveChances(PlanetTypes.First);
        for (int k = 0; k < Flora.Cave.Count; k++) Assert.Equal(Flora.Cave[k].Chances, table[k]);
    }

    /// <summary>
    /// Between two temperatures a chance runs straight, rounded down, held past the ends; an opal cache keeps its band's whatever
    /// the heat: heat cracks in the scorched planet's hot band under its surface, amber crystals in the icy planet's deepest.
    /// </summary>
    [Fact]
    public void CaveChancesFollowTheBandsTemperature()
    {
        int Of(PlanetType t, string kind, int band) => Terrain.CaveChances(t)[Flora.Cave.ToList().FindIndex(k => k.Id == kind)][band];
        // 50° on the scorched planet's band 2: 35 of the 45 degrees from 15° (0) to 60° (2400).
        Assert.Equal(2400 * 35 / 45, Of(PlanetTypes.Scorched, "heatCrack", 2));
        Assert.Equal(2400, Of(PlanetTypes.Scorched, "heatCrack", 3));
        // 40° on the icy planet's band 3: 380 at 15° less 340 × 25 / 45 (188.9) rounded down, 191.
        Assert.Equal(191, Of(PlanetTypes.Icy, "crystalAmber", 3));
        // −35° is past the coldest point: held at −30°'s.
        Assert.Equal(380, Of(PlanetTypes.Icy, "crystalIce", 0));
        Assert.Equal([3, 0, 0, 0], Enumerable.Range(0, 4).Select(b => Of(PlanetTypes.Scorched, "frostCache", b)));
        Assert.Equal([0, 0, 0, 3], Enumerable.Range(0, 4).Select(b => Of(PlanetTypes.Icy, "emberCache", b)));
    }

    /// <summary>
    /// On every system planet's type, every ore it may be dealt grows somewhere a suit made of the first planet's ores stands, the
    /// basic or the heat suit: a vein or pockets in a band of a temperature inside its limits, or a growth yielding it there.
    /// </summary>
    [Fact]
    public void EveryOreAPlanetMayHoldGrowsWhereASuitStands()
    {
        // Worn whole, helmet and suit adding their degrees (gear-roles): the basic pair and the heat pair.
        var pairs = new[] { (Helmet.Basic, Suit.Basic), (Helmet.HeatProof, Suit.Heat) }
            .Select(p => (Head: Content.Items.Get(p.Item1), Body: Content.Items.Get(p.Item2))).ToList();
        int low = pairs.Min(p => Temperature.BareLow + p.Head.Low!.Value + p.Body.Low!.Value);
        int high = pairs.Max(p => Temperature.BareHigh + p.Head.High!.Value + p.Body.High!.Value);
        foreach (var type in Content.Landing.Planets.Select(p => (PlanetType)Content.Surfaces.Get(p.Type)).Distinct())
        {
            var table = Terrain.CaveChances(type);
            foreach (var ore in Content.Landing.Ores.Where(o => o == Content.Landing.Everywhere || type.WeightOf(o) > 0))
            {
                bool In(int band) =>
                    type.Veins.Any(v => Yields(Materials.Get(v.Material).Cost, ore) && (v.Widths[band] > 0 || v.Pockets is { } p && p.Marks[band] > 0))
                    || Enumerable.Range(0, type.Caves.Count).Any(k => Yields(type.Caves[k].Yields, ore) && table[k][band] > 0);
                bool reached = Enumerable.Range(0, type.Temperatures.Count).Any(b => type.Temperatures[b] >= low && type.Temperatures[b] <= high && In(b));
                Assert.True(reached, $"{ore} on a {type.Id} planet grows only past the suits' {low}..{high}°");
            }
        }
    }
}
