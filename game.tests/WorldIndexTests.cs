using System;
using System.IO;
using System.Linq;
using Stillwake.Game.Platform;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The player's worlds on disk (0031): their list, what it says of each, a save's backup.</summary>
public class WorldIndexTests
{
    private const int Oldest = 3, Current = 4, PerSecond = 20;

    private static string Fresh() => Path.Combine(Path.GetTempPath(), $"worlds-{Guid.NewGuid():N}", "worlds");

    private static string SaveOf(int format, int tick) => $"{{\"format\":{format},\"tick\":{tick},\"rng\":1,\"slices\":{{}}}}";

    private static WorldIndex Index(string dir) => new(dir, Oldest, Current, PerSecond);

    [Fact]
    public void ListsTheLastPlayedFirstWithItsNameAndPlayTimeFromItsSave()
    {
        string dir = Fresh();
        var index = Index(dir);
        string first = index.Create("Factory", new DateTime(2026, 10, 1, 12, 0, 0));
        index.Write(first, SaveOf(Current, 20 * 125));
        string second = index.Create("  Long drift  ", new DateTime(2026, 10, 2, 12, 0, 0));
        index.Write(second, SaveOf(Current, 0));
        File.SetLastWriteTimeUtc(Path.Combine(dir, first, "world.json"), new DateTime(2026, 10, 5));
        File.SetLastWriteTimeUtc(Path.Combine(dir, second, "world.json"), new DateTime(2026, 10, 3));
        var list = index.List();
        Assert.Equal(new[] { "Factory", "Long drift" }, list.Select(w => w.Name));
        Assert.Equal(125, list[0].PlaySeconds);
        Assert.Equal(first, index.Continue()?.Id);
    }

    [Fact]
    public void TakesTheOldSaveSlotInOnceAsAWorldLastPlayedWhenItWasSaved()
    {
        string dir = Fresh(), slot = Path.Combine(Path.GetDirectoryName(dir)!, "world.json");
        var index = Index(dir);
        Assert.Null(index.Adopt(slot, "Old ship", new DateTime(2026, 10, 16)));
        Directory.CreateDirectory(Path.GetDirectoryName(slot)!);
        File.WriteAllText(slot, SaveOf(Oldest, 20 * 60));
        File.SetLastWriteTimeUtc(slot, new DateTime(2026, 10, 10));
        string? id = index.Adopt(slot, "Old ship", new DateTime(2026, 10, 16));
        Assert.NotNull(id);
        var world = Assert.Single(index.List());
        Assert.Equal(("Old ship", new DateTime(2026, 10, 10), 60, WorldStatus.Playable), (world.Name, world.LastPlayed, world.PlaySeconds, world.Status));
        Assert.False(File.Exists(slot));
        Assert.True(File.Exists(Path.ChangeExtension(slot, ".adopted.json")));
        Assert.Null(index.Adopt(slot, "Old ship", new DateTime(2026, 10, 17)));
        Assert.Single(index.List());
    }

    [Fact]
    public void SaysWhichWorldsThisBuildCannotPlayAndContinuesPastThem()
    {
        string dir = Fresh();
        var index = Index(dir);
        string newer = index.Create("Newer", new DateTime(2026, 10, 1)), old = index.Create("Old", new DateTime(2026, 10, 2));
        string broken = index.Create("Broken", new DateTime(2026, 10, 3)), fine = index.Create("Fine", new DateTime(2026, 10, 4));
        index.Write(newer, SaveOf(Current + 1, 0));
        index.Write(old, SaveOf(Oldest - 1, 0));
        index.Write(broken, "{\"form");
        index.Write(fine, SaveOf(Oldest, 0));
        File.SetLastWriteTimeUtc(Path.Combine(dir, fine, "world.json"), new DateTime(2026, 9, 1));
        var by = index.List().ToDictionary(w => w.Name, w => w.Status);
        Assert.Equal((WorldStatus.Newer, WorldStatus.TooOld, WorldStatus.Damaged, WorldStatus.Playable), (by["Newer"], by["Old"], by["Broken"], by["Fine"]));
        Assert.Equal(fine, index.Continue()?.Id);
        Assert.Null(Index(Fresh()).Continue());
    }

    [Fact]
    public void NamesAWorldAsGivenCutShortOrNextInLineAndKeepsTwoMadeTheSameSecondApart()
    {
        var index = Index(Fresh());
        var now = new DateTime(2026, 10, 16, 9, 30, 0);
        string a = index.Create("", now);
        index.Write(a, SaveOf(Current, 0));
        string b = index.Create(new string('x', 40), now);
        Assert.NotEqual(a, b);
        Assert.Equal("World 1", index.NameOf(a));
        Assert.Equal(WorldIndex.NameMost, index.NameOf(b).Length);
    }

    [Fact]
    public void KeepsTheSaveBeforeAsItsBackupAndDeletesAWorldWholly()
    {
        string dir = Fresh();
        var index = Index(dir);
        string id = index.Create("Backed up", new DateTime(2026, 10, 1));
        index.Write(id, SaveOf(Current, 1));
        index.Write(id, SaveOf(Current, 2));
        Assert.Equal(SaveOf(Current, 2), index.Read(id));
        Assert.Equal(SaveOf(Current, 1), File.ReadAllText(Path.Combine(dir, id, "world.bak.json")));
        index.Delete(id);
        Assert.False(Directory.Exists(Path.Combine(dir, id)));
        Assert.Empty(index.List());
    }

    [Fact]
    public void KeepsAPictureOfAWorldInItsFolderGoneWithItAndNoneForAWorldGone()
    {
        string dir = Fresh();
        var index = Index(dir);
        string id = index.Create("Pictured", new DateTime(2026, 10, 1));
        index.WriteShot(id, [1, 2, 3]);
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(index.ShotOf(id)));
        index.Delete(id);
        index.WriteShot(id, [4]);
        Assert.False(File.Exists(index.ShotOf(id)));
    }
}
