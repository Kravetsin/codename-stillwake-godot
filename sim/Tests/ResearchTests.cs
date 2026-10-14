using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/research/research.test.ts, ported.</summary>
public class ResearchTests
{
    private const int Width = 10;
    private const int Analyzer = 3 * Width + 1;
    private const int Ticks = Content.Research.Ticks;
    private const int Work = Ticks * 1000;

    /// <summary>
    /// Ten wide, knowing <paramref name="known"/> or, with null, everything: a solar panel out in space
    /// cabled to an analyzer in a room, and, when <paramref name="generator"/>, to a generator beside it
    /// drawing as well.
    /// </summary>
    private static Layout Map(IReadOnlyList<string>? known, bool generator = false)
    {
        string g = generator ? "GG" : "..";
        string[] rows =
        [
            "PPP       ",
            "          ",
            "##########",
            $"#AA{g}....#",
            $"#AA{g}....#",
            "##########",
        ];
        IReadOnlyList<((int X, int Y) A, (int X, int Y) B)> cables =
            generator ? [((0, 0), (1, 3)), ((0, 0), (3, 3))] : [((0, 0), (1, 3))];
        var layout = Layout.Parse(rows, null, cables);
        return known == null ? layout : layout with { Known = known };
    }

    private static Layout StartMap(bool generator = false) => Map(Content.Research.StartKnown, generator);

    /// <summary>The world on <paramref name="layout"/>, a peer 0 carrying iron, scrap and schematics, and the analyzer's id.</summary>
    private static (Scenario W, int Id) SetUp(Layout? layout = null)
    {
        var w = new Scenario(layout ?? StartMap()).GiveItems(0, ("iron", 5), ("scrap", 5), ("schematic", 3));
        return (w, w.M.Grid.MachineAt(Analyzer)?.Id ?? 0);
    }

    /// <summary>Puts one of <paramref name="item"/> from peer 0 into analyzer <paramref name="id"/>.</summary>
    private static Scenario Put(Scenario w, int id, string item)
    {
        int slot = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == item);
        return w.Do(new Items.Move(new Items.Place(slot), new Target(Crate: id), 1)).Run();
    }

    private static Scenario TakeOut(Scenario w, int id) =>
        w.Do(new Items.Move(new Items.Place(0, id), new Target(), 99)).Run();

    private static IReadOnlyList<string> Known(Scenario w) => w.M.Research.Known;
    private static int Demand(Scenario w) => w.M.Power.Networks[0].Demand;
    private static string[] Sorted(IEnumerable<string> ids) => ids.Order(StringComparer.Ordinal).ToArray();

    [Fact]
    public void LearnsWhatAMaterialTeachesInItsTicksAtFullPowerAndKeepsTheMaterial()
    {
        var (w, id) = SetUp();
        Put(w, id, "iron");
        // The tick of the move was the first of the work.
        w.Run(Ticks - 2);
        Assert.Equal(new Analyzing("iron", Work - 1000, Work, null), w.M.Research.At(id));
        Assert.DoesNotContain("wallIron", Known(w));
        w.Run();
        var iron = Content.Research.Unlocks.Single(u => u.Item == "iron").Blueprints;
        foreach (var blueprint in iron) Assert.Contains(blueprint, Known(w));
        Assert.Equal(Sorted(Known(w)), Known(w));
        Assert.Equal(new Stack("iron", 1), w.M.Items.Crate(id)[0]);
        Assert.Equal("research.nothingNew", w.M.Research.At(id)?.Problem);
    }

    [Fact]
    public void LearnsFromASchematicTheNextMachineNotKnownTheSchematicUsedUp()
    {
        var (w, id) = SetUp();
        var schematics = Content.Research.Schematics;
        Put(w, id, "schematic");
        w.Run(Ticks - 1);
        Assert.Contains(schematics[0], Known(w));
        Assert.DoesNotContain(schematics[1], Known(w));
        Assert.Null(w.M.Items.Crate(id)[0]);
        Put(w, id, "schematic");
        w.Run(Ticks - 1);
        Assert.Contains(schematics[1], Known(w));
    }

    [Fact]
    public void StandsWithNothingNewToTeachUsingNothingUpAndDrawingNoPower()
    {
        var (w, id) = SetUp(Map(null));
        Assert.Equal(0, Demand(w));
        Put(w, id, "schematic");
        w.Run(2 * Ticks);
        var at = w.M.Research.At(id);
        Assert.Equal((0, "research.nothingNew"), (at?.Progress, at?.Problem));
        Assert.Equal(new Stack("schematic", 1), w.M.Items.Crate(id)[0]);
        Assert.Equal(0, Demand(w));
    }

    [Fact]
    public void DrawsPowerWhileItLearnsAndNoneOnceItHasLearnedWhatIsInIt()
    {
        var (w, id) = SetUp();
        Put(w, id, "iron");
        Assert.Equal(2, Demand(w));
        w.Run(Ticks);
        Assert.Contains(Content.Research.Unlocks.Single(u => u.Item == "iron").Blueprints[0], Known(w));
        Assert.Equal("iron", w.M.Items.Crate(id)[0]?.Item);
        Assert.Equal(0, Demand(w));
        TakeOut(w, id);
        Put(w, id, "scrap");
        w.Run(1);
        Assert.Equal(2, Demand(w));
    }

    [Fact]
    public void StallsUnpoweredSayingSoAndWorksProportionallySlowerShortOfPower()
    {
        var (w, id) = SetUp();
        w.Do(new Power.Cut(0, Analyzer)).Run();
        Put(w, id, "iron");
        w.Run(50);
        var at = w.M.Research.At(id);
        Assert.Equal((0, "research.unpowered"), (at?.Progress, at?.Problem));

        // With a generator on the panel too, 5 against 6: each at 833‰.
        var (shortW, shortId) = SetUp(StartMap(generator: true));
        Put(shortW, shortId, "iron");
        Assert.Equal(833, shortW.M.Power.Satisfaction(shortId));
        shortW.Run((Work + 832) / 833 - 2);
        Assert.DoesNotContain("wallIron", Known(shortW));
        shortW.Run();
        Assert.Contains("wallIron", Known(shortW));
    }

    [Fact]
    public void StartsOverOnWhatIsTakenOutBeforeItIsLearned()
    {
        var (w, id) = SetUp();
        Put(w, id, "iron");
        w.Run(50);
        TakeOut(w, id);
        Assert.Null(w.M.Research.At(id));
        Put(w, id, "iron");
        Assert.Equal(1000, w.M.Research.At(id)?.Progress);
    }

    [Fact]
    public void KnowsEveryBlueprintOnAMapThatNamesNone()
    {
        var (w, _) = SetUp(Map(null));
        Assert.Equal(Sorted(Blueprints.All.Select(b => b.Id)), Known(w));
    }

    [Fact]
    public void RefusesAPlanForABlueprintNotKnownButInCreativeMode()
    {
        var (w, _) = SetUp();
        var wall = Placement.Wall("iron");
        w.Do(new PlacePlan(3 * Width + 6, wall)).Run();
        Assert.Equal(new[] { "building.unknown" }, w.Refusals.Select(r => r.Reason));
        Assert.Equal("building.unknown", w.M.Building.Problem(3 * Width + 6, wall));
        w.Do(new SetCreative(true)).Run();
        w.Do(new PlacePlan(3 * Width + 6, wall)).Run();
        Assert.Equal(new[] { "building.unknown" }, w.Refusals.Select(r => r.Reason));
        Assert.Single(w.M.Building.Plans);
    }

    /// <summary>Beyond research.test.ts: the names building asks <c>Knows</c> by.</summary>
    [Fact]
    public void KnowsAPlacementByItsBlueprintNameAndEveryOneInCreativeMode()
    {
        var (w, _) = SetUp();
        Assert.Equal("wallIron", Blueprints.Of(Placement.Wall("iron")));
        Assert.Equal("backWallScrap", Blueprints.Of(Placement.BackWall("scrap")));
        Assert.Equal("floor", Blueprints.Of(new Placement("structure", "floor")));
        Assert.Equal("furnace", Blueprints.Of(new Placement("machine", "furnace")));
        Assert.True(w.M.Research.Knows(Placement.Wall("scrap")));
        Assert.False(w.M.Research.Knows(Placement.Wall("iron")));
        w.Do(new SetCreative(true)).Run();
        Assert.True(w.M.Research.Knows(Placement.Wall("iron")));
    }

    /// <summary>A put of an item, a take-out, or a run of ticks.</summary>
    private sealed record Step(string? Put, bool Out, int Run);

    /// <summary>The README's invariant: known only grows, holds only blueprints there are, each once, sorted.</summary>
    [Property(MaxTest = 40)]
    public Property OnlyEverLearnsAndOnlyBlueprintsThereAreWhateverGoesInAndOut()
    {
        var ids = Blueprints.All.Select(b => b.Id).ToList();
        var step = Gen.OneOf(
            Gen.Elements("iron", "scrap", "schematic").Select(item => new Step(item, false, 0)),
            Gen.Constant(new Step(null, true, 0)),
            Gen.Choose(1, 2 * Ticks).Select(n => new Step(null, false, n)));
        var steps = from n in Gen.Choose(0, 25) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var (w, id) = SetUp();
            w.Run();
            var before = Known(w).ToArray();
            foreach (var s in all)
            {
                if (s.Put != null) Put(w, id, s.Put);
                else if (s.Out) TakeOut(w, id);
                else w.Run(s.Run);
                var now = Known(w).ToArray();
                if (!before.All(now.Contains)) return false;
                if (!now.All(ids.Contains)) return false;
                if (now.Distinct().Count() != now.Length) return false;
                if (!Sorted(now).SequenceEqual(now)) return false;
                before = now;
            }
            return true;
        });
    }
}
