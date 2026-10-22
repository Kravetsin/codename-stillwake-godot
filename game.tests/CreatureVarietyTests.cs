using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.Features.Fauna;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// One creature differs from the next of its kind (0102): its looks from its id and its surface's seed alone, the same on every
/// peer, within its species' bounds, and its coat never leaving the palette.
/// </summary>
public class CreatureVarietyTests
{
    public static TheoryData<string> Species() => [.. Spec.Fauna.All.Select(s => s.Id)];

    private static Arbitrary<(uint Seed, int Id)> Creatures =>
        Gen.Choose(0, int.MaxValue).SelectMany(s => Gen.Choose(1, 1_000_000).Select(id => ((uint)s * 2654435761u, id))).ToArbitrary();

    [Theory]
    [MemberData(nameof(Species))]
    public void EverySpeciesVariesInRampsItsDesignIsPaintedInAndEveryPaletteHas(string species)
    {
        Assert.True(FaunaLooks.Varieties.TryGetValue(species, out var v), $"{species} has no variety");
        var design = FaunaArt.Designs[Spec.Fauna.Get(species).Look];
        Assert.Contains(v.Coat, design.Parts["body"].Ramps);
        Assert.InRange(v.Least, 500, 1000);
        Assert.InRange(v.Most, 1000, 1500);
        // At most a tone either way: shaded past it a coat would be another ramp's.
        Assert.InRange(v.Shade, 0, 4);
        Assert.InRange(v.Morph, -4, 4);
        Assert.InRange(v.Blend, 0, 500);
        Assert.InRange(v.Drop, 0, 999);
        Assert.InRange(v.Faint, 0, 1000);
        Assert.True(v.MorphOneIn >= 0);
        foreach (var palette in FaunaArt.Palettes.Values)
            Assert.All(new[] { v.Coat, v.Toward, v.Away }, r => Assert.True(palette.ContainsKey(r), $"{species}: ramp {r}"));
    }

    [Property(MaxTest = 300)]
    public Property AnIndividualIsItsIdsAndItsSeedsAloneWithinItsSpeciesBounds() => Prop.ForAll(Creatures, c =>
    {
        foreach (var (species, v) in FaunaLooks.Varieties)
        {
            var one = CreatureVariety.Of(v, c.Seed, c.Id);
            Assert.Equal(one, CreatureVariety.Of(v with { }, c.Seed, c.Id));
            Assert.InRange(one.Size, v.Least, v.Most);
            Assert.Contains(one.Hue, CreatureVariety.Tints);
            Assert.InRange(one.Opacity, v.Faint, 1000);
            if (one.Morph) Assert.Equal(v.Morph, one.Shade);
            else Assert.InRange(one.Shade, -v.Shade, v.Shade);
        }
    });

    /// <summary>Pinned, so a change of the hash shows here: a peer on another build would draw other individuals.</summary>
    [Fact]
    public void TheSameCreatureIsTheSameIndividualOnEveryPeer()
    {
        var v = FaunaLooks.Varieties["loper"];
        Assert.Equal(CreatureVariety.Of(v, 12345, 7), CreatureVariety.Of(v, 12345, 7));
        Assert.Equal(new CreatureVariety.Individual(1049, 1, 1, false, 24252, 1000), CreatureVariety.Of(v, 12345, 7));
        Assert.NotEqual(CreatureVariety.Of(v, 12345, 7), CreatureVariety.Of(v, 12346, 7));
    }

    [Fact]
    public void AHerdOfThreeInSpawnOrderSpreadsOverMostOfItsSizesAndEachWearsItsOwnTint()
    {
        foreach (var v in FaunaLooks.Varieties.Values)
            for (int id = 1; id < 500; id++)
            {
                var three = Enumerable.Range(id, 3).Select(i => CreatureVariety.Of(v, 98765, i)).ToList();
                // The golden ratio's steps: any three in a row span at least 0.618 of the range, so one is clearly the big one.
                Assert.True(three.Max(o => o.Size) - three.Min(o => o.Size) >= (v.Most - v.Least) * 6 / 10, $"{id}: {string.Join(", ", three)}");
                // Each its own tint, the cool and the warm by turns.
                Assert.Equal(3, three.Select(o => o.Hue).Distinct().Count());
                Assert.True(Math.Sign(three[0].Hue) != Math.Sign(three[1].Hue) && Math.Sign(three[1].Hue) != Math.Sign(three[2].Hue));
            }
    }

    [Theory]
    [MemberData(nameof(Species))]
    public void SomeAreMorphsAboutAsOftenAsTheirSpeciesSaysAndEveryShadeAndBlendIsDrawn(string species)
    {
        var v = FaunaLooks.Varieties[species];
        var all = Enumerable.Range(1, 7000).Select(id => CreatureVariety.Of(v, 4242, id)).ToList();
        Assert.InRange(all.Count(o => o.Morph), 7000 / v.MorphOneIn * 8 / 10, 7000 / v.MorphOneIn * 12 / 10);
        Assert.Equal(2 * v.Shade + 1, all.Where(o => !o.Morph).Select(o => o.Shade).Distinct().Count());
        Assert.Equal(CreatureVariety.Tints.Order(), all.Select(o => o.Hue).Distinct().Order());
        // Each marking dropped about as often as its species says.
        double dropped = all.Average(o => (o.Kept & 1) == 0 ? 1.0 : 0);
        Assert.InRange(dropped, v.Drop / 1000.0 - 0.03, v.Drop / 1000.0 + 0.03);
    }

    [Fact]
    public void ACoatStaysInThePaletteBetweenItsOwnRampAndItsNeighboursATonEitherSideAndNothingElseChanges()
    {
        foreach (var (species, v) in FaunaLooks.Varieties)
            foreach (var (name, ramps) in FaunaArt.Palettes)
                for (int shade = -4; shade <= 4; shade++)
                    for (int hue = -CreatureVariety.HueSteps; hue <= CreatureVariety.HueSteps; hue++)
                    {
                        var one = CreatureVariety.Plain with { Shade = shade, Hue = hue };
                        foreach (var ramp in ramps.Keys)
                            for (int tone = 0; tone < 6; tone++)
                            {
                                var got = CreatureVariety.Tone(ramps, v, one, ramp, tone);
                                if (ramp != v.Coat || tone == 0 || shade == 0 && hue == 0)
                                {
                                    // Its outline, eye, glow and every other ramp as painted.
                                    Assert.Equal(ramps[ramp][tone], got);
                                    continue;
                                }
                                int lo = Math.Max(1, tone - 1), hi = Math.Min(5, tone + 1);
                                var kin = ramps[hue < 0 ? v.Toward : v.Away];
                                var near = Enumerable.Range(lo, hi - lo + 1).SelectMany(t => hue == 0 ? new[] { ramps[ramp][t] } : [ramps[ramp][t], kin[t]]).ToList();
                                string at = $"{species} {name} shade {shade} hue {hue} tone {tone}";
                                Assert.InRange(got.R, near.Min(c => c.R), near.Max(c => c.R));
                                Assert.InRange(got.G, near.Min(c => c.G), near.Max(c => c.G));
                                Assert.InRange(got.B, near.Min(c => c.B), near.Max(c => c.B));
                                if (hue == 0 && shade % 4 == 0) Assert.True(got == ramps[ramp][Math.Clamp(tone + shade / 4, 1, 5)], at);
                            }
                    }
    }

    [Fact]
    public void APlainCreatureIsDrawnAsPainted()
    {
        var v = FaunaLooks.Varieties["loper"];
        var ramps = FaunaArt.Palettes["savanna"];
        for (int tone = 0; tone < 6; tone++) Assert.Equal(ramps[v.Coat][tone], CreatureVariety.Tone(ramps, v, CreatureVariety.Plain, v.Coat, tone));
        Assert.All(Enumerable.Range(0, 40), m => Assert.True(CreatureVariety.Wears(CreatureVariety.Plain, m)));
    }

    [Fact]
    public void MarkingsAreTheTouchingRunsOfPaintedPixelsNumberedAsTheyAreFirstMet()
    {
        string[] rows =
        [
            "#..#.",
            ".#.#.",
            "....#",
            "##...",
        ];
        int w = rows[0].Length, h = rows.Length;
        var rgba = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (rows[y][x] == '#') rgba[(y * w + x) * 4 + 3] = 255;
        var of = CreatureVariety.Markings(rgba, w, h);
        int[] expected =
        [
            0, -1, -1, 1, -1,
            -1, 0, -1, 1, -1,
            -1, -1, -1, -1, 1,
            2, 2, -1, -1, -1,
        ];
        Assert.Equal(expected, of);
        var one = CreatureVariety.Plain with { Kept = 0b101 };
        Assert.Equal([true, false, true], new[] { 0, 1, 2 }.Select(m => CreatureVariety.Wears(one, m)));
    }
}
