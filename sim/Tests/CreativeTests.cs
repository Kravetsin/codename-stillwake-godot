using System.Text.Json.Nodes;
using FsCheck.Xunit;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// Creative mode is each peer's (2026-11-07-creative-per-player): peer <see cref="A"/> switches it on, and nothing changes
/// for peer <see cref="B"/>.
/// </summary>
public class CreativeTests
{
    private const int A = 1, B = 0;

    private const string Open = """
        #########
        #.......#
        #.......#
        #.......#
        #########
        """;

    /// <summary>The cell of the character at (<paramref name="x"/>, <paramref name="y"/>) of <see cref="Open"/>, doubled.</summary>
    private static int At(int x, int y) => 2 * y * 18 + 2 * x;

    /// <summary><see cref="Open"/>, knowing what <paramref name="known"/> says (every blueprint by default), <see cref="A"/> creative.</summary>
    private static Scenario Started(IReadOnlyList<string>? known = null) =>
        new Scenario(Layout.Parse(Open) with { Known = known }, doubled: true).Do(new SetCreative(true) { From = A }).Run();

    private static string[] Reasons(Scenario w) => w.Refusals.Select(r => r.Reason).ToArray();
    private static int Carried(Scenario w, string item, int peer) => w.M.Items.Inventory(peer).Sum(s => s?.Item == item ? s.Count : 0);

    [Fact]
    public void SetCreativeSwitchesTheSendersModeAlone()
    {
        var w = Started();
        Assert.True(w.M.Items.Creative(A));
        Assert.False(w.M.Items.Creative(B));
        w.Do(new SetCreative(true) { From = B }).Do(new SetCreative(false) { From = A }).Run();
        Assert.False(w.M.Items.Creative(A));
        Assert.True(w.M.Items.Creative(B));
        w.Do(new SetCreative(true) { From = A }).Run().Reload();
        Assert.True(w.M.Items.Creative(A) && w.M.Items.Creative(B));
    }

    [Fact]
    public void ACreativePeerPaysAsIfItHadTheItemsAndAnotherPaysAsEver()
    {
        var w = Started().GiveItems(B, ("plate", 1));
        (string, int)[] cost = [("plate", 5)];
        Assert.Null(w.M.Items.ShortOf(A, cost));
        Assert.Equal("items.notEnough", w.M.Items.ShortOf(B, cost));
        // What A pays is all of it, though nothing leaves its slots: what records a payment records it.
        Assert.Equal(cost, w.M.Items.Take(A, cost));
        Assert.Equal(0, Carried(w, "plate", A));
        Assert.Equal(1, Carried(w, "plate", B));
    }

    [Fact]
    public void SpawnIsRefusedToAPeerOutOfCreativeMode()
    {
        var w = Started();
        w.Do(new Spawn("plate", 3) { From = B }).Run();
        Assert.Equal(new[] { "items.notCreative" }, Reasons(w));
        w.Do(new Spawn("plate", 3) { From = A }).Run();
        Assert.Equal(3, Carried(w, "plate", A));
        Assert.Equal(0, Carried(w, "plate", B));
    }

    [Fact]
    public void ACreativePeersOwnSlotsOpenAllAndAnothersAsItsUpgradesSay()
    {
        var w = Started(known: []);
        var last = new Items.Place(Content.Inventory.Slots - 1);
        Assert.True(w.M.Items.SlotOpen(A, last));
        Assert.False(w.M.Items.SlotOpen(B, last));
    }

    [Fact]
    public void APeerOutOfCreativeModeMayPlanOnlyWhatTheWorldKnows()
    {
        var w = Started(known: []);
        var wall = Placement.Wall("iron");
        Assert.Equal("building.unknown", w.M.Building.Problem(At(1, 1), wall, B));
        Assert.Null(w.M.Building.Problem(At(1, 1), wall, A));
        w.Do(new PlacePlan(At(1, 1), wall) { From = B }).Run();
        Assert.Equal(new[] { "building.unknown" }, Reasons(w));
        w.Do(new PlacePlan(At(1, 1), wall) { From = A }).Run();
        Assert.Single(w.M.Building.Plans);
        Assert.False(w.M.Research.Knows(Research.Blueprints.Of(wall)));
    }

    [Fact]
    public void ACreativePeerLeavesNoBodyAndAnotherLeavesItsItems()
    {
        var w = Started().GiveItems(A, ("iron", 5)).GiveItems(B, ("iron", 7));
        w.M.Items.LeaveBody(A, At(2, 2));
        w.M.Items.LeaveBody(B, At(3, 2));
        Assert.Equal(7, w.M.Items.Floor.Sum(s => s.Item == "iron" ? s.Count : 0));
        Assert.Equal(5, Carried(w, "iron", A));
    }

    [Fact]
    public void ACreativePeersHeroKeepsItsAirAndAnothersBreathesAsEver()
    {
        var w = Started().Do(new Crew.Enter(At(2, 3)) { From = A }).Do(new Crew.Enter(At(5, 3)) { From = B }).Run();
        int a = w.M.Crew.HeroOf(A)!.Air, b = w.M.Crew.HeroOf(B)!.Air;
        w.Run(200);
        Assert.Equal(a, w.M.Crew.HeroOf(A)!.Air);
        Assert.True(w.M.Crew.HeroOf(B)!.Air < b);
    }

    /// <summary>
    /// The requirement itself: a peer's creative mode changes nothing for another. Peer 0 plays the wreck by its modules'
    /// fuzz in two worlds alike but that peer 1 is creative in one; they stay the same, refusals and all.
    /// </summary>
    [Property(MaxTest = 6)]
    public void APeersCreativeModeChangesNothingForAnother(uint seed)
    {
        var plain = World.Create(1, Registry.Wreck, new Registry().All);
        var creative = World.Create(1, Registry.Wreck, new Registry().All);
        creative.Step([new SetCreative(true) { From = A }]);
        plain.Step([]);
        var rng = new Rng(new RngState { S = seed | 1 });
        for (int i = 0; i < 200; i++)
        {
            var commands = new List<Command>();
            for (uint n = rng.Below(4); n > 0; n--)
            {
                var fuzzable = plain.ModulesOn(plain.SurfaceOf(B)).Where(m => m.Fuzzes).ToList();
                if (fuzzable[(int)rng.Below((uint)fuzzable.Count)].Fuzz(rng, B) is { } c) commands.Add(c);
            }
            Assert.Equal(plain.Step(commands).Select(r => r.Reason), creative.Step(commands).Select(r => r.Reason));
        }
        Assert.Equal(WithoutCreative(plain.Save()), WithoutCreative(creative.Save()));
    }

    /// <summary><paramref name="save"/> with who is in creative mode left out, on every surface.</summary>
    private static string WithoutCreative(string save)
    {
        var root = JsonNode.Parse(save)!;
        foreach (var slices in new[] { root["slices"]! }.Concat((root["surfaces"]?.AsArray() ?? []).Select(s => s!["slices"]!)))
            slices["items"]!.AsObject().Remove("creative");
        return root.ToJsonString();
    }

    [Fact]
    public void AWorldInCreativeModeFromAnOlderSaveKeepsItForEveryPeerWithAnInventory()
    {
        var old = JsonNode.Parse("""
            {"slices":{"items":{"creative":true,"inventories":[{"id":3},{"id":0}]}},
             "surfaces":[{"slices":{"items":{"creative":false,"inventories":[{"id":2}]}}}]}
            """)!.AsObject();
        var upgraded = Migrations.Steps[52 - Migrations.Oldest](old);
        Assert.Equal([0, 2, 3], upgraded["slices"]!["items"]!["creative"]!.AsArray().Select(p => p!.GetValue<int>()));
        Assert.Empty(upgraded["surfaces"]![0]!["slices"]!["items"]!["creative"]!.AsArray());
    }
}
