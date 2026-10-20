using System.Collections.Generic;
using System.Linq;
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

    private static IEnumerable<(string, int)> Items(IReadOnlyList<(string Item, int Count, (double X, double Y)? From)> gained) =>
        gained.Select(g => (g.Item, g.Count));

    [Fact]
    public void ShowsWhatAGrabGaveOnceWhenItHasRunAndNothingWhileItFlies()
    {
        var own = new OwnCatches();
        var grab = new Grab(5);
        Assert.Empty(own.Read([grab], CaughtFrom));
        Assert.Empty(own.Read([grab], CaughtFrom));
        Assert.Equal([("canister", 1), ("scrap", 6), ("seed", 2)], Items(own.Read([], CaughtFrom)));
        Assert.Empty(own.Read([], CaughtFrom));
    }

    [Fact]
    public void TwoCatchesRunOnOneFrameShowBothAndARefusalOrACatchThatAllFloatedNothing()
    {
        var own = new OwnCatches();
        own.Read(new List<Command> { new Grab(5), new Grab(9), new Grab(6), new Grab(8) }, CaughtFrom);
        var gained = Items(own.Read([], CaughtFrom)).ToList();
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

    [Fact]
    public void TheLootFliesFromWhereThePieceWasWhenItsGrabSetOffNotWhereItDriftedSince()
    {
        var own = new OwnCatches();
        var at = new Dictionary<int, (double X, double Y)> { [5] = (40, 12), [6] = (80, 20) };
        (double X, double Y)? WhereIs(int piece) => at.TryGetValue(piece, out var p) ? p : null;
        var (five, six) = (new Grab(5), new Grab(6));
        own.Read([five], CaughtFrom, WhereIs);
        at[5] = (44, 12);
        own.Read([five, six], CaughtFrom, WhereIs);
        var gained = own.Read([], CaughtFrom, WhereIs);
        Assert.All(gained.Where(g => g.Item != "planks"), g => Assert.Equal((40d, 12d), g.From));
        Assert.Equal((80d, 20d), gained.Single(g => g.Item == "planks").From);
    }
}
