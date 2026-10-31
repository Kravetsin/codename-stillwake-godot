using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Hazards;
using Stillwake.Sim.Research;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>The rule (docs/plans/board.md): a thing is understood when each of its parts is enough and, for a craft, a machine that makes it stands.</summary>
public class UnderstandingTests
{
    private static readonly Placement Bench = new("machine", "workbench");

    /// <summary>A bare room knowing what the wreck knows, with the map's own machines: <paramref name="row"/> its middle row.</summary>
    private static Scenario Room(string row = "#........#") =>
        new(Layout.Parse(["##########", row, "##########"], null) with { Known = Content.Research.StartKnown }, doubled: true);

    private static IReadOnlyList<(string Item, int Count)> CostOf(Scenario w, string id) => w.M.Research.Recipes().Single(r => r.Id == id).Cost;

    /// <summary>Each part of <paramref name="id"/> counted to enough, all but <paramref name="short"/> of the last.</summary>
    private static Scenario Count(Scenario w, string id, int @short = 0)
    {
        var cost = CostOf(w, id);
        for (int i = 0; i < cost.Count; i++) w.M.Knowledge.Obtain(cost[i].Item, Understanding.Enough(cost[i].Item) - (i == cost.Count - 1 ? @short : 0));
        return w;
    }

    [Fact]
    public void AWreckKnowsNothingButWhatItsPodMakesForNothingAndItsFirstWallsComeAtTenChunks()
    {
        var w = new Scenario(Registry.Wreck).Run();
        Assert.Equal(["helmet", "screwdriver", "suit"], w.M.Knowledge.GrantedIds.Order(StringComparer.Ordinal));
        Assert.False(w.M.Research.Knows(Placement.Wall("scrap")));
        w.M.Knowledge.Obtain("scrapChunk", Understanding.Enough("scrapChunk") - 1);
        Assert.False(w.Run().M.Research.Knows(Placement.Wall("scrap")));
        w.M.Knowledge.Obtain("scrapChunk", 1);
        Assert.True(w.Run().M.Research.Knows(Placement.Wall("scrap")));
        Assert.True(w.M.Research.Knows(Placement.BackWall("scrap")));
        Assert.False(w.M.Research.Knows(new Placement("machine", "tradeStation")));
    }

    [Fact]
    public void ABlueprintIsUnderstoodAtItsCountsAndItsBlockShowsDoneUnbought()
    {
        var w = Count(Room(), "furnace").Run();
        Assert.True(w.M.Research.Knows("furnace"));
        Assert.True(w.M.Research.Done(Content.Research.Node("furnace")!));
        Assert.Equal(0, w.M.Research.Credits);
    }

    [Fact]
    public void OneShortByOneIsNot()
    {
        var w = Count(Room(), "furnace", @short: 1).Run(5);
        Assert.False(w.M.Research.Knows("furnace"));
        w.M.Knowledge.Obtain(CostOf(w, "furnace")[^1].Item, 1);
        Assert.True(w.Run().M.Research.Knows("furnace"));
    }

    [Fact]
    public void ACraftWaitsForAMachineThatMakesIt()
    {
        var w = Count(Room(), "wire").Run(5);
        Assert.False(w.M.Research.Knows("wire"));
        w.Do(new Place(w.At(1, 1), Bench)).Run();
        Assert.Empty(w.Refusals);
        Assert.True(w.M.Research.Knows("wire"));
    }

    [Fact]
    public void AMachineThatMakesSomethingElseIsNoReason()
    {
        var w = Count(Room("#KK......#"), "wire").Run(5);
        Assert.False(w.M.Research.Knows("wire"));
    }

    [Fact]
    public void AMachineOnAnotherSurfaceGrantsForTheWholeWorld()
    {
        var w = Room();
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new OpenSurface(Surfaces.TestStrip.Id)).Run();
        var there = w.On(1);
        int cell = Enumerable.Range(0, there.Grid.Width * there.Grid.Height).First(c => there.Grid.PlacementProblem(c, Bench) == null);
        there.Grid.Place(cell, Bench);
        Count(w, "wire").Run();
        Assert.Empty(w.M.Grid.MachinesOf("workbench"));
        Assert.True(w.M.Research.Knows("wire"));
        Assert.True(there.Research.Knows("wire"));
    }

    [Fact]
    public void AMachineGoneKeepsWhatItTaught()
    {
        var w = Count(Room("#II......#"), "wire").Run();
        Assert.True(w.M.Research.Knows("wire"));
        w.Do(new ClearAt(w.At(1, 1), "machine")).Run(3);
        Assert.Empty(w.M.Grid.MachinesOf("workbench"));
        Assert.True(w.M.Research.Knows("wire"));
    }

    [Fact]
    public void WhatOnlyTheMarketOpensIsNeverGrantedByCounts()
    {
        var w = Count(Room(), "solarPanel").Run(3);
        Assert.False(w.M.Research.Knows("solarPanel"));
        Assert.DoesNotContain(ResearchModule.Walked, t => t.Id == "solarPanel");
    }

    [Fact]
    public void AWorldLoadedBeforeItsWalkGrantsTheSameAsOneThatRanOn()
    {
        var live = Count(Room("#II......#"), "wire");
        var loaded = Count(Room("#II......#"), "wire").Reload();
        live.Run();
        loaded.Run();
        Assert.True(loaded.M.Research.Knows("wire"));
        Assert.Equal(live.World.Hash(), loaded.World.Hash());
        Assert.True(loaded.Reload().Run().M.Research.Knows("wire"));
        Assert.Equal(live.Run().World.Hash(), loaded.World.Hash());
    }

    [Fact]
    public void FlaresArmOnceTheGeneratorIsUnderstood()
    {
        var w = new Scenario(Layout.Parse(["##########", "#........#", "##########"], null) with { Known = Content.Research.StartKnown, Meteors = true }, doubled: true).Run(3);
        int FlareIn() => ((HazardsState)w.M.Hazards.Slice).FlareIn;
        Assert.Equal(0, FlareIn());
        Count(w, "generator").Run(2);
        Assert.True(w.M.Research.Knows("generator"));
        Assert.True(FlareIn() > 0, "no flare drawn");
    }

    /// <summary>A pile takes no card, so it is no blueprint to learn: it comes to a world only as the wreck's.</summary>
    [Fact]
    public void EveryWalkedBlueprintHasACard()
    {
        var machines = Stillwake.Sim.Research.Blueprints.All.Where(b => b.Placement.Layer == "machine").ToList();
        Assert.All(machines, b => Assert.NotNull(Machines.Get(b.Placement.Kind).Menu));
        Assert.All(ResearchModule.Walked.Where(t => t.Stations == null), t => Assert.Contains(Stillwake.Sim.Research.Blueprints.All, b => b.Id == t.Id));
        Assert.DoesNotContain(ResearchModule.Walked, t => t.Id.StartsWith("pile", StringComparison.Ordinal));
    }

    [Fact]
    public void EveryWalkedCraftHasAMachineThatMakesIt() =>
        Assert.All(ResearchModule.Walked.Where(t => t.Stations != null), t => Assert.NotEmpty(t.Stations!));

    // The invariant: counts drawn and machines stood up and taken down, the rule worked out afresh after each tick.

    private static readonly string[] Makers = [.. Stations.All.Where(s => s.Rows.Count > 0).Select(s => s.Machine)];

    private static Arbitrary<uint> Seeds => Gen.Choose(1, int.MaxValue).Select(s => (uint)s).ToArbitrary();

    private static bool Allowed(Scenario w, Recipe recipe, HashSet<string> blueprints)
    {
        if (Content.Research.Node(recipe.Id) is { Tab: Content.Research.Market }) return false;
        if (recipe.Cost.Any(c => w.M.Knowledge.ObtainedOf(c.Item) < Understanding.Enough(c.Item))) return false;
        return blueprints.Contains(recipe.Id) || Stations.All.Any(s => s.Rows.Any(r => r.Contains(recipe.Id)) && w.M.Grid.MachinesOf(s.Machine).Count > 0);
    }

    [Property(MaxTest = 40)]
    public Property AfterAWalkNothingTheRuleAllowsIsUngranted() => Prop.ForAll(Seeds, seed =>
    {
        var rng = new Rng(new RngState { S = seed });
        int Below(int n) => (int)rng.Below((uint)n);
        string[] rows = [new string('#', 80), .. Enumerable.Repeat("#" + new string('.', 78) + "#", 8), new string('#', 80)];
        var w = new Scenario(Layout.Parse(rows, null) with { Known = Content.Research.StartKnown });
        var blueprints = Stillwake.Sim.Research.Blueprints.All.Select(b => b.Id).ToHashSet(StringComparer.Ordinal);
        var recipes = w.M.Research.Recipes();
        var ever = new HashSet<string>(Content.Research.StartKnown, StringComparer.Ordinal);
        for (int step = 0; step < 60; step++)
        {
            int cell = w.At(1 + 10 * Below(Makers.Length), 1);
            string maker = Makers[(cell % w.Layout.Width - 1) / 10];
            if (Below(3) == 0) w.Do(w.M.Grid.MachineAt(cell) != null ? new ClearAt(cell, "machine") : new Place(cell, new Placement("machine", maker)));
            var pick = recipes[Below(recipes.Count)];
            foreach (var (item, _) in pick.Cost) w.M.Knowledge.Obtain(item, Below(4) == 0 ? 1 : Understanding.Enough(item));
            w.Run();
            Assert.Empty(w.Refusals);
            foreach (var recipe in recipes.Where(r => Allowed(w, r, blueprints))) ever.Add(recipe.Id);
            var missed = ever.Where(id => !w.M.Knowledge.Granted(id)).ToList();
            Assert.True(missed.Count == 0, $"allowed and not granted: {string.Join(", ", missed)}");
            var stray = w.M.Knowledge.GrantedIds.Where(id => !ever.Contains(id)).ToList();
            Assert.True(stray.Count == 0, $"granted and not allowed: {string.Join(", ", stray)}");
        }
    });
}
