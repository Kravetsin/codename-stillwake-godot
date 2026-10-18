using System.Collections.Generic;
using Stillwake.Game.Features.Items;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>What this peer's own catches gave, for the popups over its hero: once each, as the grab runs.</summary>
public class OwnCatchesTests
{
    /// <summary>What the sim says each piece's catch gave: a pod's loot, a crate's planks; 9 was refused.</summary>
    private static readonly Dictionary<int, IReadOnlyList<(string Item, int Count)>> Caught = new()
    {
        [5] = [("canister", 1), ("scrap", 6), ("seed", 2)],
        [6] = [("planks", 4)],
        [8] = [],
    };

    private static IReadOnlyList<(string Item, int Count)>? CaughtFrom(int piece) => Caught.GetValueOrDefault(piece);

    [Fact]
    public void ShowsWhatAGrabGaveOnceWhenItHasRunAndNothingWhileItFlies()
    {
        var own = new OwnCatches();
        var grab = new Grab(5);
        Assert.Empty(own.Read([grab], CaughtFrom));
        Assert.Empty(own.Read([grab], CaughtFrom));
        Assert.Equal([("canister", 1), ("scrap", 6), ("seed", 2)], own.Read([], CaughtFrom));
        Assert.Empty(own.Read([], CaughtFrom));
    }

    [Fact]
    public void TwoCatchesRunOnOneFrameShowBothAndARefusalOrACatchThatAllFloatedNothing()
    {
        var own = new OwnCatches();
        own.Read(new List<Command> { new Grab(5), new Grab(9), new Grab(6), new Grab(8) }, CaughtFrom);
        var gained = own.Read([], CaughtFrom);
        Assert.Equal(4, gained.Count);
        Assert.Contains(("planks", 4), gained);
        Assert.Contains(("seed", 2), gained);
    }

    [Fact]
    public void AHitOrAFriendsCatchIsNeverOwnForNoOwnGrabFlewForIt()
    {
        var own = new OwnCatches();
        // A friend caught 5 meanwhile: the sim knows it, but no grab of this peer's flew for it.
        own.Read([new Hit(6, Spec.Weapons.Axe.Item)], CaughtFrom);
        Assert.Empty(own.Read([], CaughtFrom));
    }
}
