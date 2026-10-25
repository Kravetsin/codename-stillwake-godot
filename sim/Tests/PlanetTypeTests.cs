using Stillwake.Content;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Planet types (0095): every one whole and consistent, and a second one only data the same generator grows.</summary>
public class PlanetTypeTests
{
    private static Flora.Kind Plant(string id, int chance) => Flora.All.Single(k => k.Id == id) with { Chance = chance };

    private static Flora.CaveKind Cave(string id, params int[] chances) => Flora.Cave.Single(k => k.Id == id) with { Chances = chances };

    /// <summary>
    /// A cold type, for these tests only, never shipped: lighter gravity, thinner air, a pale sky; the small ground with low hills
    /// and a soil two rows deep at most, no ruins; a frozen core, bedrock where the first planet's lava lies; only rocks and tufts
    /// on its surface; three bands of caves, ice in the cold one, copper deep.
    /// </summary>
    private static readonly PlanetType Frost = PlanetTypes.Small with
    {
        Id = "frost",
        Gravity = 450,
        ThinAir = 3,
        Sky = new Surfaces.Light(820, 900, 1000),
        Ground = PlanetTypes.Small.Ground with { HillRise = 1, SoilLeast = 1, SoilMost = 2, Spires = 0, Homes = 0 },
        Core = "bedrock",
        Plants = [Plant("rock", 300), Plant("tuft", 400)],
        Caves =
        [
            Cave("crystalIce", 600, 80, 0),
            Cave("stalagmite", 400, 300, 300),
            Cave("crystalIceHang", 500, 40, 0),
            Cave("stalactite", 700, 500, 500),
            Cave("mica", 1600, 1200, 1200),
        ],
        Bands = [400, 800],
        // Ice veins only, in its cold and middle bands.
        Veins = [new("iceVein", Wave: 40, Tall: 24, Widths: [900, 300, 0])],
    };

    /// <summary>What is wrong with <paramref name="t"/>, or null: what its ground grows from exists and fits its draws.</summary>
    private static string? Wrong(PlanetType t)
    {
        foreach (string material in (string[])[t.Soil, t.Stone, t.Bedrock, t.Core])
            if (!Materials.TryGet(material, out var m) || !m.Natural) return $"{material}: no natural material";
        if (!Materials.Get(t.Soil).Breaks || !Materials.Get(t.Stone).Breaks) return "its soil and stone must be dug";
        if (Materials.Get(t.Bedrock).Breaks) return "its bedrock breaks";
        if (t.Gravity is not > 0) return "no gravity";
        var g = t.Ground;
        if (g.Width < 1 || g.Height < 1 || g.Surface < 1 || g.Surface >= g.Height - g.CoreRows) return "its surface is off its grid";
        if (g.SoilLeast < 1 || g.SoilLeast > g.SoilMost || g.MouthFadeLeast > g.MouthFadeMost || g.HomeDeepLeast > g.HomeDeepMost) return "a range upside down";
        if (g.BedrockRows < 1 || g.LavaRows < 0 || g.BedrockSide < 1) return "no floor or sides";
        if (t.Bands.Count == 0 || t.Bands.Any(b => b <= 0 || b >= 1000) || !t.Bands.Zip(t.Bands.Skip(1)).All(p => p.First < p.Second)) return "bands not rising inside 1000";
        if (t.PatchWave < 1 || t.PatchMost < 1 || t.Hall < 1) return "its patches";
        if (t.Plants.Select(k => k.Id).Distinct().Count() != t.Plants.Count || t.Caves.Select(k => k.Id).Distinct().Count() != t.Caves.Count) return "a growth twice";
        // A type takes growths from the catalog and says only where and how often they grow: what each is stays the catalog's.
        foreach (var k in t.Plants)
        {
            if (Flora.All.FirstOrDefault(c => c.Id == k.Id) is not { } c || !Alike(k, c)) return $"{k.Id}: not the catalog's plant";
            if (k.Level < 0 || k.Room < 0 || k.Clear < 1 || k.Chance <= 0 || k.Apart < 1) return $"{k.Id}: its room";
        }
        // The generator draws under 1000 a column and walks the chances in order (0079): past 1000, the last never grows.
        if (t.Plants.Sum(k => k.Chance) is < 1 or > 1000) return "plants' chances past a thousand";
        foreach (var k in t.Caves)
        {
            if (Flora.Cave.FirstOrDefault(c => c.Id == k.Id) is not { } c || !Alike(k, c) || k.Holds != c.Holds) return $"{k.Id}: not the catalog's cave growth";
            if (k.Chances.Count != t.Bands.Count + 1 || k.Chances.Any(n => n < 0) || k.Wide < 0 || k.Apart < 1) return $"{k.Id}: a chance for each band";
        }
        foreach (var v in t.Veins)
        {
            if (!Materials.TryGet(v.Material, out var m) || m.In != t.Stone || !m.Breaks) return $"{v.Material}: no vein in its stone";
            if (v.Wave < 1 || v.Tall < 1 || v.Widths.Count != t.Bands.Count + 1 || v.Widths.Any(n => n is < 0 or >= 32768)) return $"{v.Material}: a width for each band";
        }
        if (t.LodeWave < 1 || t.LodeOver is < 0 or >= 65536 || t.LodeMost < 0) return "its lodes";
        foreach (var on in Enum.GetValues<Flora.Anchor>())
            for (int band = 0; band <= t.Bands.Count; band++)
                if (t.Caves.Where(k => k.On == on).Sum(k => k.Chances[band]) > 10000) return $"{on} in band {band}: chances past a slot";
        return null;
    }

    private static bool Alike(Flora.Growth a, Flora.Growth b) =>
        a.Tall == b.Tall && a.On == b.On && a.Across == b.Across && a.Behind == b.Behind && a.Fixed == b.Fixed && a.Strength == b.Strength
        && a.Yields.SequenceEqual(b.Yields);

    [Fact]
    public void EveryTypeIsWholeAndConsistentAndASurfaceKindOfItsOwn()
    {
        Assert.All(PlanetTypes.All, t => Assert.Null(Wrong(t)));
        Assert.Equal(Surfaces.All.Count, Surfaces.All.Select(k => k.Id).Distinct().Count());
        Assert.All(PlanetTypes.All, t => Assert.Same(t, Surfaces.Get(t.Id)));
        Assert.DoesNotContain(Surfaces.Ship.Id, PlanetTypes.All.Select(t => t.Id));
    }

    [Fact]
    public void TheChecksBite()
    {
        Assert.NotNull(Wrong(PlanetTypes.Small with { Core = "molten" }));
        Assert.NotNull(Wrong(PlanetTypes.Small with { Bedrock = "stone" }));
        Assert.NotNull(Wrong(PlanetTypes.Small with { Bands = [600, 250] }));
        Assert.NotNull(Wrong(PlanetTypes.Small with { Veins = [new("stone", 40, 24, [0, 0, 0, 0])] }));
        Assert.NotNull(Wrong(PlanetTypes.Small with { Veins = [new("iceVein", 40, 24, [900])] }));
        Assert.NotNull(Wrong(PlanetTypes.Small with { Bands = [250, 600] }));
        Assert.NotNull(Wrong(PlanetTypes.Small with { Plants = [Plant("acacia", 600), Plant("bush", 600)] }));
        Assert.NotNull(Wrong(PlanetTypes.Small with { Plants = [Plant("acacia", 100) with { Yields = [("planks", 99)] }] }));
        Assert.NotNull(Wrong(PlanetTypes.Small with { Caves = [Cave("moss", 9000, 0, 0, 0), Cave("mica", 2000, 0, 0, 0)] }));
    }

    [Fact]
    public void TheTestStripLivesUnderTheFirstPlanetsRules()
    {
        var (strip, first) = (Surfaces.TestStrip, PlanetTypes.First);
        Assert.Equal((first.Gravity, first.ThinAir, first.Sky, first.Debris, first.Builds), (strip.Gravity, strip.ThinAir, strip.Sky, strip.Debris, strip.Builds));
        Assert.IsNotType<PlanetType>(strip);
    }

    [Fact]
    public void ASecondTypeIsOnlyDataTheGeneratorGrowsByItsOwnNumbers()
    {
        Assert.Null(Wrong(Frost));
        Assert.DoesNotContain(Surfaces.All, k => k.Id == Frost.Id);
        int soil = Materials.Get(Frost.Soil).Code, lava = Materials.Get(Lava.Material).Code;
        foreach (uint seed in new uint[] { 1, 2, 3, 4, 5, 6 })
        {
            var l = Terrain.Generate(Frost, seed);
            Assert.Equal(l.Rows, Terrain.Generate(Frost, seed).Rows);
            Assert.NotEqual(l.Rows, Terrain.Generate(PlanetTypes.Small, seed).Rows);
            // Its core frozen: no lava anywhere.
            Assert.DoesNotContain(lava, l.Materials!);
            // Its soil two rows deep at most, under the open sky or anywhere.
            for (int x = 0; x < l.Width; x++)
                Assert.True(Enumerable.Range(0, l.Height).Count(y => Math.Abs(l.Materials![y * l.Width + x]) == soil) <= Frost.Ground.SoilMost, $"seed {seed}: column {x}");
            // Only its own growths, the ruins' aside, each in a band it grows in.
            foreach (var (cell, kind) in l.Flora!)
            {
                if (Ruins.Growths.Contains(kind)) continue;
                if (Frost.Caves.FirstOrDefault(k => k.Id == kind) is { } cave)
                    Assert.True(cave.Chances[Terrain.BandOf(Frost, cell / l.Width)] > 0, $"seed {seed}: {kind} out of its band");
                else Assert.Contains(kind, Frost.Plants.Select(k => k.Id));
            }
            // A world plays on its ground.
            new Scenario(l).Run(2);
        }
        var grown = Enumerable.Range(1, 6).SelectMany(s => Terrain.Generate(Frost, (uint)s).Flora!).Select(p => p.Kind).ToHashSet();
        // Its own veins, not the first planet's.
        var materials = Enumerable.Range(1, 6).SelectMany(s => Terrain.Generate(Frost, (uint)s).Materials!).ToHashSet();
        Assert.Contains(Materials.Get("iceVein").Code, materials);
        Assert.DoesNotContain(Materials.Get("copperVein").Code, materials);
        Assert.Contains("rock", grown);
        Assert.Equal(3, Terrain.BandOf(Frost, Frost.Ground.Height - Frost.Ground.CoreRows - 1) + 1);
        // Its rules are its own: a fall gains less a step than on the first planet.
        Assert.True(Frost.Fall(Content.Hero.Gravity) < PlanetTypes.First.Fall(Content.Hero.Gravity));
    }
}
