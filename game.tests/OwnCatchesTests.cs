using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Features.Items;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>What this peer's own catches gave, for the popups over its hero: once each, as the grab or the net's collect runs.</summary>
public class OwnCatchesTests
{
    /// <summary>What the sim says each piece's catch gave, once it has run: a pod's loot, a crate's planks; 9 was refused; all of 8 floated.</summary>
    private static readonly Dictionary<int, IReadOnlyList<(string Item, int Count)>> Ran = new()
    {
        [5] = [("canister", 1), ("scrap", 6), ("seed", 2)],
        [6] = [("planks", 4)],
        [8] = [],
    };

    /// <summary>The sim's answers: none until <see cref="Run"/> puts in what a catch gave, a new list each time, as the sim's are.</summary>
    private readonly Dictionary<int, IReadOnlyList<(string Item, int Count)>> _caught = [];

    private IReadOnlyList<(string Item, int Count)>? CaughtFrom(int piece) => _caught.GetValueOrDefault(piece);

    private void Run(params int[] pieces)
    {
        foreach (int piece in pieces)
            if (Ran.TryGetValue(piece, out var gave)) _caught[piece] = [.. gave];
    }

    private static IEnumerable<(string, int)> Items(IReadOnlyList<(string Item, int Count, (double X, double Y)? From)> gained) =>
        gained.Select(g => (g.Item, g.Count));

    [Fact]
    public void ShowsWhatAGrabGaveOnceWhenItHasRunAndNothingWhileItFlies()
    {
        var own = new OwnCatches();
        var grab = new Grab(5);
        Assert.Empty(own.Read([grab], CaughtFrom));
        Assert.Empty(own.Read([grab], CaughtFrom));
        Run(5);
        Assert.Equal([("canister", 1), ("scrap", 6), ("seed", 2)], Items(own.Read([], CaughtFrom)));
        Assert.Empty(own.Read([], CaughtFrom));
    }

    [Fact]
    public void TwoCatchesRunOnOneFrameShowBothAndARefusalOrACatchThatAllFloatedNothing()
    {
        var own = new OwnCatches();
        own.Read(new List<Command> { new Grab(5), new Grab(9), new Grab(6), new Grab(8) }, CaughtFrom);
        Run(5, 9, 6, 8);
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
        own.Read([new HewWrecks(Spec.Weapons.Axe.Item, 6_000, 0, 0)], CaughtFrom);
        Run(5);
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
        Run(5, 6);
        var gained = own.Read([], CaughtFrom, WhereIs);
        Assert.All(gained.Where(g => g.Item != "planks"), g => Assert.Equal((40d, 12d), g.From));
        Assert.Equal((80d, 20d), gained.Single(g => g.Item == "planks").From);
    }

    // A net's collect: every piece it held, as a grab of each.

    /// <summary>Net 3 holds pieces 5 and 6, each drawn where its net lays it; net 4 holds nothing.</summary>
    private static IReadOnlyList<(int Piece, (double X, double Y) At)> NetHolds(int net) =>
        net == 3 ? [(5, (100, 40)), (6, (101, 54))] : [];

    [Fact]
    public void ACollectShowsWhatEachPieceGaveOnceFlyingFromWhereItLayInTheNet()
    {
        var own = new OwnCatches();
        var collect = new Collect(3);
        Assert.Empty(own.Read([collect], CaughtFrom, netHolds: NetHolds));
        Assert.Empty(own.Read([collect], CaughtFrom, netHolds: NetHolds));
        Run(5, 6);
        var gained = own.Read([], CaughtFrom, netHolds: NetHolds);
        Assert.Equal([("canister", 1), ("scrap", 6), ("seed", 2), ("planks", 4)], Items(gained));
        Assert.All(gained.Take(3), g => Assert.Equal((100d, 40d), g.From));
        Assert.Equal((101d, 54d), gained[3].From);
        Assert.Empty(own.Read([], CaughtFrom, netHolds: NetHolds));
    }

    [Fact]
    public void ACollectPartlyGivenShowsWhatWentInAndOneRefusedAfterItNothingOfBefore()
    {
        var own = new OwnCatches();
        own.Read([new Collect(3)], CaughtFrom, netHolds: NetHolds);
        // Only one plank fit; nothing of 5.
        _caught[6] = [("planks", 1)];
        Assert.Equal([("planks", 1)], Items(own.Read([], CaughtFrom, netHolds: NetHolds)));
        // E again on a full bag: refused, the sim's answer for 6 the same list as before.
        own.Read([new Collect(3)], CaughtFrom, netHolds: NetHolds);
        Assert.Empty(own.Read([], CaughtFrom, netHolds: NetHolds));
        // Room made, E again: the rest, a new answer.
        own.Read([new Collect(3)], CaughtFrom, netHolds: NetHolds);
        _caught[6] = [("planks", 3)];
        Assert.Equal([("planks", 3)], Items(own.Read([], CaughtFrom, netHolds: NetHolds)));
    }

    [Fact]
    public void TwoPressesInFlightAtOnceShowTheOneCollectThatGaveOnceAndAnEmptyNetNothing()
    {
        var own = new OwnCatches();
        var (first, second) = (new Collect(3), new Collect(3));
        own.Read([first], CaughtFrom, netHolds: NetHolds);
        own.Read([first, second], CaughtFrom, netHolds: NetHolds);
        Run(5, 6);
        Assert.Equal(4, own.Read([second], CaughtFrom, netHolds: NetHolds).Count);
        Assert.Empty(own.Read([], CaughtFrom, netHolds: NetHolds));
        own.Read([new Collect(4)], CaughtFrom, netHolds: NetHolds);
        Assert.Empty(own.Read([], CaughtFrom, netHolds: NetHolds));
    }
}
