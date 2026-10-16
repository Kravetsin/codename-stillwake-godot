using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Every kind of salvage that drifts has its pictures, and pieces in a row wear different ones.</summary>
public class DebrisLooksTests
{
    private static string GameFolder([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "game"));

    [Fact]
    public void GivesEveryKindThatDriftsPicturesThatAreThere()
    {
        foreach (var (item, _) in Content.Debris.Kinds)
        {
            Assert.True(DebrisLooks.Of.ContainsKey(item), $"{item} drifts with no pictures of its own");
            for (int id = 0; id < DebrisLooks.Of[item].Count; id++)
            {
                string file = DebrisLooks.PictureOf(item, id)!["res://".Length..];
                Assert.True(File.Exists(Path.Combine(GameFolder(), file)), $"{file} is missing");
            }
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
