using System;
using Stillwake.Game.Menu;
using Stillwake.Game.Platform;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The Worlds panel's pick: any world may be picked, to be deleted; only a playable one is played.</summary>
public class WorldPickTests
{
    private static WorldEntry World(string id, WorldStatus status) => new(id, id, new DateTime(2026, 10, 25), 0, status);

    private static readonly WorldEntry[] Worlds =
        [World("old", WorldStatus.TooOld), World("good", WorldStatus.Playable), World("new", WorldStatus.Newer)];

    [Fact]
    public void KeepsAWorldThatCannotBePlayedPickedSoItCanBeDeleted()
    {
        Assert.Equal("old", WorldPick.After(Worlds, "old"));
        Assert.Null(WorldPick.Playable(Worlds, "old"));
    }

    [Fact]
    public void PicksTheFirstPlayableWhenThePickIsGoneOrNone()
    {
        Assert.Equal("good", WorldPick.After(Worlds, null));
        Assert.Equal("good", WorldPick.After(Worlds, "deleted"));
        Assert.Equal("good", WorldPick.Playable(Worlds, "good"));
    }

    [Fact]
    public void PicksNothingWhenNoWorldCanBePlayed()
    {
        WorldEntry[] old = [World("a", WorldStatus.TooOld), World("b", WorldStatus.Damaged)];
        Assert.Null(WorldPick.After(old, null));
        Assert.Equal("b", WorldPick.After(old, "b"));
        Assert.Null(WorldPick.After([], "a"));
    }
}
