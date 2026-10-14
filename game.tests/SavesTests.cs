using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stillwake.Game.Platform;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>apps/desktop/src/saves.test.ts, ported.</summary>
public class SavesTests
{
    private static string Fresh() => Path.Combine(Path.GetTempPath(), $"saves-{Guid.NewGuid():N}", "saves");

    [Fact]
    public void ReadsBackWhatWasWrittenOverwritesASlotAndReadsNothingFromAnEmptyOne()
    {
        var saves = new Saves(Fresh());
        Assert.Null(saves.Read("dev"));
        saves.Write("dev", "first");
        saves.Write("dev", "second");
        Assert.Equal("second", saves.Read("dev"));
    }

    [Fact]
    public void KeepsOneFilePerSlotAndNothingLeftAside()
    {
        string dir = Fresh();
        var saves = new Saves(dir);
        saves.Write("dev", "{}");
        saves.Write("desync", "{}");
        Assert.Equal(new[] { "desync.json", "dev.json" },
            Directory.GetFiles(dir).Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task KeepsOverlappingWritesToOneSlotWholeOneOfThemWinning()
    {
        var saves = new Saves(Fresh());
        string longer = new('x', 3_000_000), shorter = new('y', 2_000_000);
        await Task.WhenAll(Task.Run(() => saves.Write("dev", longer)), Task.Run(() => saves.Write("dev", shorter)));
        // Writes here are not queued as promises: whichever runs last wins, and each is whole.
        Assert.Contains(saves.Read("dev"), new[] { longer, shorter });
    }

    [Fact]
    public void RefusesASlotNameThatIsNotAPlainName()
    {
        var saves = new Saves(Fresh());
        Assert.Contains("not a save slot name", Assert.Throws<ArgumentException>(() => saves.Write("../escape", "x")).Message);
        Assert.Contains("not a save slot name", Assert.Throws<ArgumentException>(() => saves.Read("a/b")).Message);
    }
}
