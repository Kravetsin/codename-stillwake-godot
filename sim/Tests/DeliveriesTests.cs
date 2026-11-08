using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Research;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;
using Tree = Stillwake.Content.Research;

namespace Stillwake.Sim.Tests;

/// <summary>The mafia's deliveries (2026-11-08-mafia-deliveries): a purchase drifts in on the trade station's row, caught or broken to be had.</summary>
public class DeliveriesTests
{
    /// <summary>A hull with a trade station, space all round: doubled, 80 cells wide, the hull's right edge at column 39, the lane row 6.</summary>
    private static readonly string[] Rows =
    [
        "                                        ",
        "          ##########                    ",
        "          #$$......#                    ",
        "          #$$......#                    ",
        "          ##########                    ",
        "                                        ",
    ];
    private const int Width = 80;
    private const int Lane = 6;
    private const int Unit = 1000;
    private static readonly int Start = (Width - 1) * Unit + Unit / 2;
    /// <summary>A cell in space, right of the hull, where a hero catches and breaks what drifts by.</summary>
    private static readonly int Out = 2 * Width + 60;

    private static Scenario World(string[]? rows = null, int credits = 1_000_000)
    {
        var w = new Scenario(Layout.Parse(rows ?? Rows, null) with { Known = Tree.StartKnown }, doubled: true);
        w.M.Knowledge.Earn(credits);
        return w;
    }

    /// <summary>The delivery pieces out, in spawn order.</summary>
    private static List<Piece> Sent(Scenario w) => [.. w.M.Debris.Pieces.Where(p => p.Delivery != 0)];

    private static Scenario Buy(Scenario w, params string[] goods)
    {
        foreach (var good in goods) w.Do(new BuyBlock(good));
        return w.Run();
    }

    /// <summary>The piece of delivery brought into reach of a hero at <see cref="Out"/>: hit or grabbed there.</summary>
    private static Piece Near(Scenario w, Piece piece)
    {
        piece.X = Out % Width * Unit + Unit / 2;
        piece.Y = Out / Width * Unit + Unit / 2;
        return piece;
    }

    private static Scenario Break(Scenario w, Piece piece, int peer = 0)
    {
        Near(w, piece);
        int hp = Content.Wreckage.HpOf(piece.Item)!.Value, hits = (hp + Content.Weapons.Axe.Damage - 1) / Content.Weapons.Axe.Damage;
        for (int i = 0; i < hits; i++) w.Do(HewTesting.Hit(w, piece.Id, Content.Weapons.Axe.Item) with { From = peer }).Run();
        return w;
    }

    [Fact]
    public void APurchaseOnTheShipSpawnsItsPieceAtTheStreamsStartOnTheLaneTheSameTick()
    {
        var w = Buy(World(), "bag1");
        Assert.Empty(w.Refusals);
        var piece = Assert.Single(Sent(w));
        Assert.Equal((DebrisSpec.Parcel, DebrisSpec.DeliveryZone, Start, Lane * Unit + Unit / 2), (piece.Item, piece.Zone, piece.X, piece.Y));
        Assert.Equal(w.M.Knowledge.Deliveries.Single().Id, piece.Delivery);
        Assert.Equal(DebrisSpec.Ship.DriftMin, piece.Speed);
    }

    [Theory]
    [InlineData("bag1", DebrisSpec.Parcel)]
    [InlineData("solarPanel", DebrisSpec.DeliveryCrate)]
    [InlineData("scanner", DebrisSpec.DeliveryCrate)]
    [InlineData(Tree.Confiscated, Tree.Confiscated)]
    public void AnUpgradeComesAsAParcelABlueprintOrGearAsACrateTheContainerAsItself(string good, string kind)
    {
        var w = World();
        Mafia.Reach(w, Tree.Node(good)!.Ring);
        Buy(w, good);
        Assert.Equal(kind, Assert.Single(Sent(w)).Item);
    }

    [Fact]
    public void WithNoTradeStationADeliveryWaitsQueued()
    {
        var w = Buy(World([.. Rows.Select(r => r.Replace('$', '.'))]), "bag1");
        w.Run(100);
        Assert.Empty(Sent(w));
        Assert.True(w.M.Knowledge.InFlight("bag1"));
    }

    [Fact]
    public void APurchaseMadeOnAPlanetDriftsInOnTheShipAndNeverThere()
    {
        var w = World();
        w.Do(new Enter(Out)).Do(new OpenSurface(Content.Surfaces.TestStrip.Id)).Run();
        var planet = w.World.Surfaces[^1].Id;
        w.Do(new Transfer(planet)).Run();
        Buy(w, "bag1");
        Assert.DoesNotContain(w.On(planet).Debris.Pieces, p => p.Delivery != 0);
        Assert.Single(Sent(w));
    }

    [Fact]
    public void TwoBoughtOnOneTickComeOneAfterTheOtherAGapApart()
    {
        var w = Buy(World(), "bag1", "bag2");
        Assert.Equal(2, w.M.Knowledge.Deliveries.Count);
        var first = Assert.Single(Sent(w));
        while (Sent(w).Count == 1) w.Run();
        var second = Sent(w)[1];
        Assert.Equal(Start, second.X);
        Assert.True(Start - first.X >= DebrisSpec.DeliveryGap * Unit, $"{Start - first.X}");
        Assert.True(Start - first.X < DebrisSpec.DeliveryGap * Unit + first.Speed, $"{Start - first.X}");
        Assert.Equal(["bag1", "bag2"], Sent(w).Select(p => w.M.Knowledge.Deliveries.Single(d => d.Id == p.Delivery).Good));
    }

    [Fact]
    public void AParcelCaughtGrantsItsUpgradeCrewWide()
    {
        var w = Buy(World(), "bag1");
        var piece = Near(w, Sent(w).Single());
        w.Do(new Enter(Out)).Run().Do(new Grab(piece.Id)).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(Sent(w));
        Assert.Empty(w.M.Knowledge.Deliveries);
        Assert.True(w.M.Research.Knows("bag1"));
        Assert.True(w.M.Items.SlotOpen(0, new Stillwake.Sim.Items.Place(Content.Inventory.Start)));
        Assert.DoesNotContain(w.M.Items.Inventory(0), s => s?.Item == DebrisSpec.Parcel);
        Assert.Contains(w.M.Items.Gains, g => g is { Peer: 0, Item: "bag1", Count: 1 });
    }

    [Fact]
    public void ACrateIsNoParcelAndRefusesAGrabAsAnyWreck()
    {
        var w = World();
        Mafia.Reach(w, 1);
        var piece = Near(w, Buy(w, "scanner").M.Debris.Pieces.Single(p => p.Delivery != 0));
        w.Do(new Enter(Out)).Run().Do(new Grab(piece.Id)).Run();
        Assert.Equal(["debris.tooBig"], w.Refusals.Select(r => r.Reason));
    }

    [Fact]
    public void ACrateBrokenGrantsItsBlueprint()
    {
        var w = World();
        Mafia.Reach(w, Tree.Node("solarPanel")!.Ring);
        Buy(w, "solarPanel").Do(new Enter(Out)).Run();
        Break(w, Sent(w).Single());
        Assert.Empty(w.Refusals);
        Assert.Empty(Sent(w));
        Assert.True(w.M.Research.Knows("solarPanel"));
        Assert.Empty(w.M.Items.Floor);
        Assert.Contains(w.M.Items.Gains, g => g is { Peer: 0, Item: "solarPanel", Count: 1 });
    }

    [Fact]
    public void ACrateOfGearBrokenDropsItUnderItAsAWrecksLoot()
    {
        var w = World();
        Mafia.Reach(w, 1);
        Buy(w, "scanner").Do(new Enter(Out)).Do(new Enter(Out) { From = 2 }).Run();
        Break(w, Sent(w).Single(), peer: 2);
        Assert.Empty(w.Refusals);
        Assert.Equal([("scanner", 1)], w.M.Items.Floor.Select(s => (s.Item, s.Count)));
        Assert.DoesNotContain(w.M.Items.Inventory(2), s => s?.Item == "scanner");
        Assert.DoesNotContain(w.M.Items.Gains, g => g.Item == "scanner");
        Assert.False(w.M.Research.Knows("scanner"));
        Assert.Empty(w.M.Knowledge.Deliveries);
    }

    [Fact]
    public void TheSeizedContainerBreaksIntoItsLootAndIsFound()
    {
        var w = World();
        Mafia.Reach(w, Tree.Node(Tree.Confiscated)!.Ring);
        Buy(w, Tree.Confiscated).Do(new Enter(Out)).Run();
        Break(w, Sent(w).Single());
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Knowledge.Deliveries);
        var loot = Content.Loot.Of(Tree.Confiscated)!;
        Assert.NotEmpty(w.M.Items.Floor);
        Assert.All(w.M.Items.Floor, s => Assert.Contains(s.Item, loot.Select(e => e.Item)));
        Assert.Contains(w.M.Debris.Found, f => f.Kind == Tree.Confiscated);
        Assert.False(w.M.Research.Knows(Tree.Confiscated));
    }

    [Fact]
    public void GearAndTheContainerAreBoughtAgainAndAgain()
    {
        var w = World();
        Mafia.Reach(w, Tree.Node(Tree.Confiscated)!.Ring);
        Buy(w, Tree.Confiscated, Tree.Confiscated, "heavyHelmet", "heavyHelmet");
        Assert.Empty(w.Refusals);
        Assert.Equal(4, w.M.Knowledge.Deliveries.Count);
    }

    [Fact]
    public void AnUpgradeInFlightIsBoughtAlready()
    {
        var w = Buy(World(), "bag1");
        Buy(w, "bag1");
        Assert.Equal(["research.bought"], w.Refusals.Select(r => r.Reason));
    }

    [Fact]
    public void ADeliveryDriftedPastIsLostAndItsGoodBuyableAgain()
    {
        var w = Buy(World(), "bag1");
        var piece = Sent(w).Single();
        int ticks = (piece.X - piece.End) / piece.Speed + 2;
        w.Run(ticks);
        Assert.Empty(Sent(w));
        Assert.Empty(w.M.Knowledge.Deliveries);
        Assert.Equal(1, w.M.Knowledge.Lost);
        Assert.False(w.M.Research.Knows("bag1"));
        Assert.Null(w.M.Research.BuyProblem(Tree.Node("bag1")!));
    }

    [Fact]
    public void ADeliveryOutIsSavedWithItsPieceAndLoadsTheSame()
    {
        var w = Buy(World(), "bag1", "bag2").Run(30);
        var before = w.World.Save();
        w.Reload();
        Assert.Equal(before, w.World.Save());
        Assert.Equal(w.M.Knowledge.Deliveries.First().Id, Sent(w).Single().Delivery);
        while (Sent(w).Count == 1) w.Run();
        Assert.Equal(3, w.M.Knowledge.Deliveries.Max(d => d.Id) + 1);
    }

    /// <summary>One step of the invariant: a buy, a grab or a break of the first delivery out, or ticks run.</summary>
    public sealed record Step(int Kind, int Good, int Ticks);

    private static readonly string[] Goods = ["bag1", "bag2", "bag3", "scanner", Tree.Confiscated, "solarPanel"];

    /// <summary>
    /// Over any buys, catches, breaks and ticks: a delivery is queued once until its piece is caught, broken or gone; no
    /// piece names one not queued, nor two the same; nothing granted is still queued; piece ids ascend; credits never go negative.
    /// </summary>
    [Property(MaxTest = 30)]
    public Property EveryDeliveryIsQueuedUntilItsPieceIsCaughtBrokenOrGone()
    {
        var steps = Gen.ListOf(Gen.Choose(0, 3).SelectMany(k => Gen.Choose(0, Goods.Length - 1).SelectMany(g => Gen.Choose(1, 400).Select(t => new Step(k, g, t))))).Select(s => s.Take(25).ToList());
        return Prop.ForAll(steps.ToArbitrary(), list =>
        {
            var w = World(credits: 40_000);
            Mafia.Reach(w, 4);
            w.Do(new Enter(Out)).Run();
            foreach (var step in list)
            {
                var first = Sent(w).FirstOrDefault();
                switch (step.Kind)
                {
                    case 0: Buy(w, Goods[step.Good]); break;
                    case 1 when first != null: w.Do(new Grab(Near(w, first).Id)).Run(); break;
                    case 2 when first != null && Content.Wreckage.HpOf(first.Item) != null: Break(w, first); break;
                    default: w.Run(step.Ticks); break;
                }
                var queued = w.M.Knowledge.Deliveries.Select(d => d.Id).ToList();
                Assert.Equal(queued.Distinct().Count(), queued.Count);
                var named = Sent(w).Select(p => p.Delivery).ToList();
                Assert.Equal(named.Distinct().Count(), named.Count);
                Assert.All(named, id => Assert.Contains(id, queued));
                Assert.DoesNotContain(w.M.Knowledge.Deliveries, d => !ResearchModule.Repeats(d.Good) && w.M.Knowledge.Granted(d.Good));
                Assert.True(w.M.Research.Credits >= 0);
                var ids = w.M.Debris.Pieces.Select(p => p.Id).ToList();
                Assert.Equal(ids.Order(), ids);
            }
        });
    }

    /// <summary>Random commands on a world with a station and credits, deliveries among them: two worlds alike, saved and loaded alike.</summary>
    [Theory]
    [InlineData(5u)]
    [InlineData(77u)]
    public void FuzzedWithDeliveriesTwoWorldsStayAlikeAndLoadTheSame(uint seed)
    {
        var rng = new Rng(new RngState { S = seed });
        var (a, b) = (World(), World());
        Mafia.Reach(a, 5);
        Mafia.Reach(b, 5);
        bool seen = false;
        for (int i = 0; i < 400; i++)
        {
            var commands = ModulesTests.RandomCommands(a.World, rng);
            if (i % 20 == 0) commands.Add(new BuyBlock(Goods[i / 20 % Goods.Length]));
            a.World.Step(commands);
            b.World.Step(commands);
            seen |= Sent(a).Count > 0;
        }
        Assert.True(seen);
        Assert.Equal(a.World.Hash(), b.World.Hash());
        b.Reload(a.World.Save());
        Assert.Equal(a.World.Hash(), b.World.Hash());
        for (int i = 0; i < 100; i++)
        {
            var commands = ModulesTests.RandomCommands(a.World, rng);
            a.World.Step(commands);
            b.World.Step(commands);
        }
        Assert.Equal(a.World.Hash(), b.World.Hash());
    }
}
