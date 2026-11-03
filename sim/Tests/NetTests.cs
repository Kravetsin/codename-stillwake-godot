using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using Xunit;
using DebrisSpec = Stillwake.Content.Debris;
using Loot = Stillwake.Content.Loot;
using Machines = Stillwake.Content.Machines;

namespace Stillwake.Sim.Tests;

/// <summary>The debris net (docs/decisions/2026-11-03-debris-net.md): hung on the bow, catching small pieces, emptied by E, dropping them when it goes.</summary>
public class NetTests
{
    private const int Width = 16;

    /// <summary>A sealed room, its right wall at column 5, rows 1 to 8; with <paramref name="net"/>, a net hung on it at (6, 2), rows 2 to 7.</summary>
    private static Layout Map(bool net = true) => Layout.Parse(
    [
        "                ",
        "  ####          ",
        .. Enumerable.Repeat(net ? "  #..#nn        " : "  #..#          ", 6),
        "  ####          ",
        "                ",
    ]);

    private static int At(int x, int y) => y * Width + x;

    /// <summary>Where the net of <see cref="Map"/> catches: the cell of a piece's middle in the middle of it.</summary>
    private static readonly int Middle = At(6, 4);

    private static readonly Placement Net = new("machine", "net");

    private static DebrisState State(Scenario w) => (DebrisState)w.M.Debris.Slice;

    /// <summary>A world of <paramref name="map"/> whose stream spawns nothing: every piece in it is one a test drifts in.</summary>
    private static Scenario Quiet(Layout? map = null, uint seed = 1)
    {
        var w = new Scenario(map ?? Map(), seed);
        foreach (var beat in State(w).Beats) beat.Next = int.MaxValue;
        return w;
    }

    private static int NetId(Scenario w) => w.M.Grid.MachinesWith(Content.MachineTrait.Catches)[0].Id;

    /// <summary>A piece of <paramref name="item"/> put out a tick's drift right of <paramref name="cell"/>'s middle: there on the next tick.</summary>
    private static Piece Drift(Scenario w, string item, int cell, int width = Width, int speed = 100)
    {
        var s = State(w);
        int x = cell % width * 1000 + 500 + speed;
        var piece = new Piece { Id = s.NextId++, Zone = DebrisSpec.Ship.Id, Item = item, X = x, Y = cell / width * 1000 + 500, Speed = speed, Start = x, End = 0 };
        s.Pieces.Add(piece);
        return piece;
    }

    /// <summary>The pieces of <paramref name="items"/> caught, one a tick, in order.</summary>
    private static Scenario Caught(Scenario w, params string[] items)
    {
        foreach (var item in items)
        {
            Drift(w, item, Middle);
            w.Run();
        }
        Assert.Equal(items, w.M.Debris.Held.Select(e => e.Kind));
        return w;
    }

    private static int Count(Scenario w, string item, int peer) => w.M.Items.Inventory(peer).Sum(s => s?.Item == item ? s.Count : 0);

    private static int Lying(Scenario w, string item) => w.M.Items.Floor.Where(s => s.Item == item).Sum(s => s.Count);

    private static int InNets(Scenario w, string item) => w.M.Debris.Held.Sum(e => e.Items.Where(s => s.Item == item).Sum(s => s.Count));

    private static string[] Reasons(Scenario w) => [.. w.Refusals.Select(r => r.Reason)];

    /// <summary><paramref name="peer"/>'s inventory filled with canisters: room for nothing else, but on a stack started first.</summary>
    private static Scenario Fill(Scenario w, int peer)
    {
        int stack = Content.Items.Get("canister").Stack;
        while (w.M.Items.FullFor(peer, [("canister", stack)]) == null) w.GiveItems(peer, ("canister", stack));
        for (int n = stack - 1; n > 0; n--)
            if (w.M.Items.FullFor(peer, [("canister", n)]) == null) w.GiveItems(peer, ("canister", n));
        return w;
    }

    private static string Rng(Scenario w) => JsonNode.Parse(w.World.Save())!["rng"]!.ToJsonString();

    // Placement.

    [Fact]
    public void HangsInSpaceAgainstTheHullsRightWallEveryRowOfIt()
    {
        var w = new Scenario(Map(net: false));
        Assert.Null(w.M.Grid.PlacementProblem(At(6, 2), Net));
        Assert.Null(w.M.Building.Problem(At(6, 2), Net));
    }

    [Fact]
    public void IsRefusedWithARowMissingItsWallABackWallInACellOrTheWallOnItsRight()
    {
        var w = new Scenario(Map(net: false));
        // Rows 4 to 9: the last has no wall on its left.
        Assert.Equal("grid.notOnHull", w.M.Grid.PlacementProblem(At(6, 4), Net));
        Assert.Equal("grid.notOnHull", w.M.Building.Problem(At(6, 4), Net));
        // Left of the room, its wall on the right; and inside it, against the left wall, where back walls stand.
        Assert.Equal("grid.notOnHull", w.M.Grid.PlacementProblem(At(0, 2), Net));
        Assert.Equal("grid.notOnHull", w.M.Grid.PlacementProblem(At(3, 2), Net));
        w.Do(new Place(At(7, 5), Placement.BackWall())).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal("grid.notOnHull", w.M.Grid.PlacementProblem(At(6, 2), Net));
        // A machine in the way is still that.
        Assert.Equal("grid.machineInTheWay", new Scenario(Map()).M.Grid.PlacementProblem(At(6, 2), Net));
    }

    [Fact]
    public void IsDrawnOnAMapBareOfBackWallsAsItWouldBeHung()
    {
        var w = new Scenario(Map());
        for (int y = 2; y < 8; y++)
            for (int x = 6; x < 8; x++)
                Assert.Null(w.M.Grid.BackAt(At(x, y)));
    }

    // The catch.

    [Theory]
    [InlineData("scrapChunk")]
    [InlineData("hullPlate")]
    [InlineData("cargoCrate")]
    [InlineData("supplyPod")]
    public void CatchesEveryKindItTakesIntoAnEntryOfWhatItGives(string kind)
    {
        var w = Quiet();
        var piece = Drift(w, kind, Middle);
        w.Run();
        Assert.Empty(w.M.Debris.Pieces);
        var entry = Assert.Single(w.M.Debris.Held);
        Assert.Equal((NetId(w), piece.Id, kind, Middle), (entry.Net, entry.Id, entry.Kind, entry.Cell));
        if (Loot.Of(kind) is { } loot) Assert.All(entry.Items, s => Assert.Contains(loot, e => e.Item == s.Item && s.Count >= e.Min && s.Count <= e.Max));
        else Assert.Equal([(kind, 1)], entry.Items.Select(s => (s.Item, s.Count)));
        var found = Assert.Single(w.M.Debris.Found);
        Assert.Equal(kind, found.Kind);
        Assert.Equal(entry.Items.Select(s => s.Item), found.Items);
        Assert.Null(w.M.Debris.CaughtFrom(piece.Id));
    }

    [Theory]
    [InlineData("hullSection")]
    [InlineData("soilClod")]
    [InlineData("asteroidSmall")]
    public void LetsAWreckAClodAndAnAsteroidPassThrough(string kind)
    {
        var w = Quiet();
        Drift(w, kind, Middle);
        w.Run(3);
        Assert.Single(w.M.Debris.Pieces);
        Assert.Empty(w.M.Debris.Held);
    }

    [Fact]
    public void LetsAPieceBeingReeledPassThrough()
    {
        var w = Quiet();
        var piece = Drift(w, "scrapChunk", Middle);
        piece.Reel = new Reeling { By = 1, ToX = piece.X - 100, ToY = piece.Y, Left = 5 };
        w.Run(2);
        Assert.Single(w.M.Debris.Pieces);
        Assert.Empty(w.M.Debris.Held);
    }

    [Fact]
    public void CatchesTenPiecesThenLetsEverythingPassDrawingNothingForIt()
    {
        var w = Quiet();
        Caught(w, [.. Enumerable.Repeat("scrapChunk", DebrisSpec.NetHolds)]);
        var twin = Quiet();
        Caught(twin, [.. Enumerable.Repeat("scrapChunk", DebrisSpec.NetHolds)]);
        Drift(w, "cargoCrate", Middle);
        Drift(twin, "scrapChunk", At(10, 0));
        w.Run();
        twin.Run();
        Assert.Equal(DebrisSpec.NetHolds, w.M.Debris.Held.Count);
        Assert.Equal("cargoCrate", Assert.Single(w.M.Debris.Pieces).Item);
        Assert.Equal(Rng(twin), Rng(w));
        Assert.DoesNotContain(w.M.Debris.Found, f => f.Kind == "cargoCrate");
    }

    [Fact]
    public void RollsAContainersLootOnceAsItIsCaughtRecordingItFound()
    {
        var w = Quiet();
        uint before = JsonNode.Parse(w.World.Save())!["rng"]!.GetValue<uint>();
        Drift(w, "cargoCrate", Middle);
        w.Run();
        var rng = new RngState { S = before };
        var rolled = DebrisModule.LootOf("cargoCrate", new Rng(rng));
        Assert.Equal(rolled, Assert.Single(w.M.Debris.Held).Items.Select(s => (s.Item, s.Count)));
        Assert.Equal(rng.S, JsonNode.Parse(w.World.Save())!["rng"]!.GetValue<uint>());
        Assert.Equal(rolled.Select(r => r.Item), Assert.Single(w.M.Debris.Found).Items);
    }

    [Fact]
    public void CatchesAPieceOnTheTickItsNetIsBuilt()
    {
        var w = Quiet(Map(net: false));
        Drift(w, "hullPlate", Middle);
        w.Do(new Place(At(6, 2), Net)).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Debris.Pieces);
        Assert.Equal("hullPlate", Assert.Single(w.M.Debris.Held).Kind);
    }

    // Collect.

    [Fact]
    public void CollectsEverythingItHoldsIntoTheSendersInventoryCountedObtained()
    {
        var w = Caught(Quiet(), "scrapChunk", "hullPlate", "scrapChunk");
        int obtained = w.M.Knowledge.ObtainedOf("scrapChunk");
        w.Do(new Collect(NetId(w)) { From = 2 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Empty(w.M.Debris.Held);
        Assert.Equal((2, 1), (Count(w, "scrapChunk", 2), Count(w, "hullPlate", 2)));
        Assert.Equal(obtained + 2, w.M.Knowledge.ObtainedOf("scrapChunk"));
        Assert.Equal("debris.netEmpty", w.M.Debris.CollectProblem(2, NetId(w)));
    }

    [Fact]
    public void GivesWhatFitsKeepingTheRestAndGoesOnPastAnEntryThatDoesNot()
    {
        var w = Caught(Quiet(), "hullPlate", "scrapChunk");
        w.GiveItems(2, ("scrapChunk", 1));
        Fill(w, 2);
        int obtained = w.M.Knowledge.ObtainedOf("hullPlate");
        w.Do(new Collect(NetId(w)) { From = 2 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(2, Count(w, "scrapChunk", 2));
        Assert.Equal("hullPlate", Assert.Single(w.M.Debris.Held).Kind);
        Assert.Equal(obtained, w.M.Knowledge.ObtainedOf("hullPlate"));
        Assert.Equal("debris.bagFull", w.M.Debris.CollectProblem(2, NetId(w)));
    }

    [Fact]
    public void RefusesNoNetAnEmptyNetAndABagWithNoRoomLeavingTheWorldAsItWas()
    {
        // A crate out in space beside the net: a machine that catches nothing.
        static Scenario Make(bool full)
        {
            var w = Quiet().Do(new Place(At(11, 0), new Placement("machine", "crate"))).Run();
            Assert.Empty(w.Refusals);
            if (full) Caught(w, "scrapChunk");
            return w;
        }
        var probe = Make(false);
        int net = NetId(probe), crate = probe.M.Grid.MachinesOf("crate")[0].Id;
        foreach (var (full, command, reason) in new (bool, Command, string)[]
        {
            (true, new Collect(99) { From = 1 }, "debris.noNet"),
            (true, new Collect(crate) { From = 1 }, "debris.noNet"),
            (false, new Collect(net) { From = 1 }, "debris.netEmpty"),
            (true, new Collect(net) { From = 1 }, "debris.bagFull"),
        })
        {
            var (w, twin) = (Make(full), Make(full));
            if (reason == "debris.bagFull")
            {
                Fill(w, 1);
                Fill(twin, 1);
            }
            Assert.Equal(reason, w.M.Debris.CollectProblem(1, ((Collect)command).Machine));
            w.Do(command).Run();
            twin.Run();
            Assert.Equal([reason], Reasons(w));
            Assert.Equal(twin.World.Hash(), w.World.Hash());
        }
    }

    [Fact]
    public void CollectsForAHeroInsideTheShipThroughTheWall()
    {
        var w = Caught(Quiet(), "hullPlate");
        w.Do(new Enter(At(3, 7)) { From = 3 }).Run();
        Assert.NotEqual(Rooms.RoomsModule.Space, w.M.Rooms.RoomAt(w.M.Crew.HeroOf(3)!.Cell));
        w.Do(new Collect(NetId(w)) { From = 3 }).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal(1, Count(w, "hullPlate", 3));
    }

    [Fact]
    public void GivesTwoCollectsOnOneTickInHostOrderTheSecondTheRestOrNothing()
    {
        var w = Caught(Quiet(), "scrapChunk", "hullPlate");
        w.Do(new Collect(NetId(w)) { From = 1 }).Do(new Collect(NetId(w)) { From = 2 }).Run();
        Assert.Equal(["debris.netEmpty"], Reasons(w));
        Assert.Equal((1, 1), (Count(w, "scrapChunk", 1), Count(w, "hullPlate", 1)));
        // The first with room only for chunks: the second gets the plate.
        var part = Caught(Quiet(), "scrapChunk", "hullPlate");
        part.GiveItems(1, ("scrapChunk", 1));
        Fill(part, 1);
        part.Do(new Collect(NetId(part)) { From = 1 }).Do(new Collect(NetId(part)) { From = 2 }).Run();
        Assert.Empty(part.Refusals);
        Assert.Equal((2, 1), (Count(part, "scrapChunk", 1), Count(part, "hullPlate", 2)));
        Assert.Empty(part.M.Debris.Held);
    }

    [Fact]
    public void RefusesACollectOnTheTickAWorkFinishesTakingTheNetApartWhosePiecesFallThen()
    {
        var w = Caught(Quiet(), "scrapChunk", "hullPlate");
        w.Do(new Deconstruct(At(6, 2), "machine")).Run();
        int plan = Assert.Single(w.M.Building.Plans).Id;
        int ticks = Costs.ApartTicks(Machines.Get("net").BuildTicks);
        for (int i = 1; i < ticks; i++) w.Do(new Work(plan) { From = 1 }).Run();
        Assert.NotEmpty(w.M.Grid.MachinesOf("net"));
        w.Do(new Work(plan) { From = 1 }).Do(new Collect(2) { From = 1 }).Run();
        Assert.Equal(["debris.noNet"], Reasons(w));
        Assert.Empty(w.M.Grid.MachinesOf("net"));
        Assert.Empty(w.M.Debris.Held);
        Assert.Equal((1, 1), (Lying(w, "scrapChunk"), Lying(w, "hullPlate")));
        Assert.All(w.M.Items.Floor, s => Assert.Equal(Middle, s.Cell));
    }

    // Meteors.

    /// <summary>Six rows of hull four thick, a net hung on every one: alloy, which no meteor here breaks, so they keep coming.</summary>
    [Fact]
    public void AimPastTheNetAtTheWallItHangsOn()
    {
        var layout = Layout.Parse(string.Join('\n', Enumerable.Repeat("..####nn..", 6)))
            .WithMaterials(string.Join('\n', Enumerable.Repeat("..aaaa....", 6))) with
        { Meteors = true };
        var w = new Scenario(layout);
        var shower = Content.Meteors.BeforeLanding;
        for (int i = 0; i < shower.First + 2 && w.M.Hazards.Incoming == null; i++) w.Run();
        var meteor = w.M.Hazards.Incoming;
        Assert.NotNull(meteor);
        Assert.Equal(5, meteor.Cell % 10);
    }

    /// <summary>Eight rows of hull: rows 1 to 6 with a net hung on them at columns 6 and 7, rows 0 and 7 bare.</summary>
    private static Scenario Struck(string material = ".")
    {
        string[] rows = ["..####....", .. Enumerable.Repeat("..####nn..", 6), "..####...."];
        string walls = ".." + new string(material[0], 4) + "....";
        var w = Quiet(Layout.Parse(rows).WithMaterials(string.Join('\n', Enumerable.Repeat(walls, 8))) with { Meteors = true });
        foreach (var item in new[] { "scrapChunk", "hullPlate" })
        {
            Drift(w, item, 4 * 10 + 6, width: 10);
            w.Run();
        }
        Assert.Equal(2, w.M.Debris.Held.Count);
        return w;
    }

    [Fact]
    public void BreakTheNetWithTheWallItHangsOnItsPiecesFloatingOutWhereTheyHung()
    {
        var w = Struck();
        var (walls, _) = HazardsTests.StrikeAt(w, 3 * 10 + 5, 2);
        Assert.Contains(3 * 10 + 5, walls);
        Assert.Empty(w.M.Grid.MachinesOf("net"));
        Assert.Empty(w.M.Debris.Held);
        Assert.Equal((1, 1), (Lying(w, "scrapChunk"), Lying(w, "hullPlate")));
        Assert.All(w.M.Items.Floor, s => Assert.Equal(4 * 10 + 6, s.Cell));
    }

    [Fact]
    public void BreakTheNetWhenTheBlastBreaksTheWallBesideItsTopRow()
    {
        var w = Struck();
        var (walls, _) = HazardsTests.StrikeAt(w, 5, 2);
        Assert.Contains(10 + 5, walls);
        Assert.Empty(w.M.Grid.MachinesOf("net"));
        Assert.Empty(w.M.Debris.Held);
    }

    [Fact]
    public void BreakNothingWithAPowerBelowTheWallsArmour()
    {
        var w = Struck("n");
        var (walls, _) = HazardsTests.StrikeAt(w, 3 * 10 + 5, Content.Materials.Get("iron").Armour - 1);
        Assert.Empty(walls);
        Assert.Single(w.M.Grid.MachinesOf("net"));
        Assert.Equal(2, w.M.Debris.Held.Count);
    }

    // A net gone, and saves.

    [Fact]
    public void DropsWhatItHeldWhenTakenApartWhereEachWasCaught()
    {
        var w = Caught(Quiet(), "scrapChunk", "cargoCrate");
        var held = w.M.Debris.Held.SelectMany(e => e.Items).GroupBy(s => s.Item).Select(g => (g.Key, g.Sum(s => s.Count))).ToList();
        int obtained = w.M.Knowledge.ObtainedOf("scrapChunk");
        w.Do(new ClearAt(At(6, 2), "machine")).Run();
        Assert.Empty(w.M.Debris.Held);
        Assert.All(held, h => Assert.Equal(h.Item2, Lying(w, h.Key)));
        Assert.All(w.M.Items.Floor, s => Assert.Equal(Middle, s.Cell));
        Assert.Equal(obtained + 1, w.M.Knowledge.ObtainedOf("scrapChunk"));
        // Built again on the spot, it is another net, and empty.
        w.Do(new Place(At(6, 2), Net)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal("debris.netEmpty", w.M.Debris.CollectProblem(1, NetId(w)));
    }

    [Fact]
    public void KeepsWhatItHoldsThroughASaveAndDropsItWhenClearedAfterALoad()
    {
        var w = Caught(Quiet(), "scrapChunk", "supplyPod");
        var held = w.M.Debris.Held.Select(e => (e.Net, e.Id, e.Kind, e.Cell, string.Join(",", e.Items))).ToList();
        ulong hash = w.World.Hash();
        w.Reload();
        Assert.Equal(hash, w.World.Hash());
        Assert.Equal(held, w.M.Debris.Held.Select(e => (e.Net, e.Id, e.Kind, e.Cell, string.Join(",", e.Items))));
        w.Run();
        Assert.Equal(2, w.M.Debris.Held.Count);
        w.Reload().Do(new ClearAt(At(6, 2), "machine")).Run();
        Assert.Empty(w.M.Debris.Held);
        Assert.True(Lying(w, "scrapChunk") >= 1);
    }

    // Invariants.

    /// <summary>A step: pieces of <c>Kinds</c> drifted in, one a row of the net from its top, a collect by <c>Peer</c>, the net cleared or built again, a save and load.</summary>
    private sealed record Step(int What, int[] Kinds, int Peer);

    private static readonly string[] Kinds = [.. DebrisSpec.NetCatches, "hullSection", "soilClod"];

    private static readonly string[] Counted = [.. DebrisSpec.NetCatches.Concat(Loot.All.SelectMany(l => l.Entries.Select(e => e.Item))).Distinct()];

    /// <summary>Of each counted item, what the peers carry, what lies about and what the net holds.</summary>
    private static int[] Total(Scenario w) =>
        [.. Counted.Select(i => Enumerable.Range(0, 5).Sum(p => Count(w, i, p)) + Lying(w, i) + InNets(w, i))];

    /// <summary>
    /// Whatever pieces drift in, whoever collects, however often the net goes and comes back and the world is saved: every entry's
    /// net stands after the drop, none holds more than its ten, and a collect or a drop neither makes nor loses an item, the
    /// conservation counting what the net holds and what it caught that tick.
    /// </summary>
    [Property(MaxTest = 40)]
    public Property HoldsNoMoreThanTenEveryEntryInANetStandingAndMakesNorLosesAnything()
    {
        // Mostly pieces, several a tick, so the net fills to its ten between the rarer collects and clears.
        var step = from what in Gen.Frequency((10, Gen.Constant(0)), (2, Gen.Constant(1)), (1, Gen.Constant(2)), (1, Gen.Constant(3)))
                   from kinds in Gen.Choose(0, Kinds.Length - 1).ArrayOf(3)
                   from peer in Gen.Choose(0, 4)
                   select new Step(what, kinds, peer);
        var steps = from n in Gen.Choose(1, 40) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var w = Quiet();
            // Peers 3 and 4 have room for little, so collects leave entries behind, a supply pod's part given.
            w.GiveItems(4, ("scrapChunk", 1));
            Fill(w, 4);
            w.GiveItems(3, ("scrap", Content.Items.Get("scrap").Stack - 3));
            Fill(w, 3);
            var total = Total(w);
            foreach (var s in all)
            {
                var before = w.M.Debris.Held.Select(e => e.Id).ToHashSet();
                var lying = w.M.Items.Floor.Select(f => (f.Id, f.Item, f.Count)).ToList();
                var nets = w.M.Grid.MachinesOf("net");
                switch (s.What)
                {
                    case 0:
                        for (int k = 0; k < s.Kinds.Length; k++) Drift(w, Kinds[s.Kinds[k]], At(6, 2 + k));
                        break;
                    case 1: if (nets.Count > 0) w.Do(new Collect(nets[0].Id) { From = s.Peer }); break;
                    case 2: w.Do(nets.Count > 0 ? new ClearAt(At(6, 2), "machine") : new Place(At(6, 2), Net)); break;
                    default: w.Reload(); break;
                }
                w.Run();
                foreach (var e in w.M.Debris.Held)
                    if (w.M.Grid.MachineById(e.Net) is not { } m || !Machines.Is(m.Kind, Content.MachineTrait.Catches)) return false;
                if (w.M.Debris.Held.GroupBy(e => e.Net).Any(g => g.Count() > DebrisSpec.NetHolds)) return false;
                var caught = w.M.Debris.Held.Where(e => !before.Contains(e.Id)).SelectMany(e => e.Items).ToList();
                var after = Total(w);
                for (int i = 0; i < Counted.Length; i++)
                {
                    // Only a stack floating off the grid is lost, as Items' floor rules say.
                    int lost = total[i] + caught.Where(c => c.Item == Counted[i]).Sum(c => c.Count) - after[i];
                    int left = lying.Where(l => l.Item == Counted[i] && w.M.Items.Floor.All(f => f.Id != l.Id)).Sum(l => l.Count);
                    if (lost < 0 || lost > left) return false;
                }
                total = after;
            }
            return true;
        });
    }
}
