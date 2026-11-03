using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Every kind of salvage that drifts has its pictures, and pieces in a row wear different ones.</summary>
public class DebrisLooksTests
{
    [Fact]
    public void GivesEveryKindThatDriftsPicturesThatAreThere()
    {
        // Named here for every kind the sim counts looks of (Content.Looks), and no other.
        Assert.Equal(Content.Looks.All.Select(l => l.Kind).Order(StringComparer.Ordinal), DebrisLooks.Of.Keys.Order(StringComparer.Ordinal));
        foreach (var (item, _) in Content.Debris.Kinds)
        {
            Assert.True(DebrisLooks.Of.ContainsKey(item), $"{item} drifts with no pictures of its own");
            for (int id = 0; id < DebrisLooks.Of[item].Count; id++)
            {
                string file = DebrisLooks.PictureOf(item, id)!;
                Assert.True(Png.Has(file), $"{file} is missing");
            }
        }
    }

    [Fact]
    public void KnowsTheSizeOfEveryWrecksPictureAsItIs()
    {
        foreach (var (item, _) in Content.Wreckage.All)
        {
            Assert.True(DebrisLooks.Wrecks.TryGetValue(item, out var size), $"{item} has no size to be aimed at by");
            for (int id = 0; id < DebrisLooks.Of[item].Count; id++) Assert.Equal(size, Png.SizeOf(DebrisLooks.PictureOf(item, id)!));
        }
    }

    [Fact]
    public void DressesPiecesInARowDifferently()
    {
        var looks = Enumerable.Range(0, 12).Select(id => DebrisLooks.PictureOf("scrapChunk", id)).ToList();
        Assert.Equal(12, looks.Distinct().Count());
        Assert.Equal(looks[0], DebrisLooks.PictureOf("scrapChunk", 12));
        Assert.Null(DebrisLooks.PictureOf("planks", 3));
    }
}
