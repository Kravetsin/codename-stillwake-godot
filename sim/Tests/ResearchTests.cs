using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>packages/sim/src/modules/research/research.test.ts, ported, and its rule since 0027: what is analysed opens what is made of it.</summary>
public class ResearchTests
{
    private const int Width = 10;
    private const int Analyzer = 3 * Width + 1;
    private const int Ticks = Content.Research.Ticks;
    private const int Work = Ticks * 1000;

    /// <summary>
    /// Ten wide, granting <paramref name="known"/> or, with null, everything: a solar panel out in space
    /// cabled to an analyzer in a room, and, when <paramref name="generator"/>, to a generator beside it
    /// drawing as well; with <paramref name="two"/>, to a second analyzer there instead.
    /// </summary>
    private static Layout Map(IReadOnlyList<string>? known, bool generator = false, bool two = false)
    {
        string g = generator ? "GG" : two ? "AA" : "..";
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
            generator || two ? [((0, 0), (1, 3)), ((0, 0), (3, 3))] : [((0, 0), (1, 3))];
        var layout = Layout.Parse(rows, null, cables);
        return known == null ? layout : layout with { Known = known };
    }

    private static Layout StartMap(bool generator = false, bool two = false) => Map(Content.Research.StartKnown, generator, two);

    /// <summary>The world on <paramref name="layout"/>, a peer 0 carrying materials and parts, and the analyzer's id.</summary>
    private static (Scenario W, int Id) SetUp(Layout? layout = null)
    {
        var w = new Scenario(layout ?? StartMap())
            .GiveItems(0, ("iron", 5), ("scrap", 5), ("plate", 5), ("gear", 5), ("wire", 5), ("planks", 5));
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

    /// <summary>Puts <paramref name="item"/> in, waits until it is analysed, and takes it out again.</summary>
    private static Scenario Analyse(Scenario w, int id, string item)
    {
        Put(w, id, item);
        w.Run(Ticks);
        Assert.Contains(item, w.M.Research.Analysed);
        return TakeOut(w, id);
    }

    private static IReadOnlyList<string> Analysed(Scenario w) => w.M.Research.Analysed;
    private static int Demand(Scenario w) => w.M.Power.Networks[0].Demand;
    private static string[] Sorted(IEnumerable<string> ids) => ids.Order(StringComparer.Ordinal).ToArray();
    private static Recipe RecipeOf(Scenario w, string id) => w.M.Research.Recipes().Single(r => r.Id == id);

    [Fact]
    public void AnalysesAnItemInItsTicksAtFullPowerKeepsItAndOpensWhatIsMadeOfItAlone()
    {
        var (w, id) = SetUp();
        Put(w, id, "iron");
        // The tick of the move was the first of the work.
        w.Run(Ticks - 2);
        Assert.Equal(new Analyzing("iron", Work - 1000, Work, null), w.M.Research.At(id));
        Assert.False(w.M.Research.Knows("plate"));
        w.Run();
        Assert.Equal(["iron"], Analysed(w));
        Assert.True(w.M.Research.Knows("plate"));
        Assert.True(w.M.Research.Knows("gear"));
        // The door is made of plates and a gear, not of iron.
        Assert.False(w.M.Research.Knows(new Placement("structure", "door")));
        Assert.Equal(new Stack("iron", 1), w.M.Items.Crate(id)[0]);
        Assert.Equal("research.nothingNew", w.M.Research.At(id)?.Problem);
    }

    [Fact]
    public void OpensABlueprintOnceEveryItemOfItsCostIsAnalysedSayingWhichAreLeft()
    {
        var (w, id) = SetUp();
        var door = new Placement("structure", "door");
        Assert.Equal(["plate", "gear"], RecipeOf(w, "door").Unanalysed);
        Analyse(w, id, "plate");
        Assert.Equal(["gear"], RecipeOf(w, "door").Unanalysed);
        Assert.False(w.M.Research.Knows(door));
        Analyse(w, id, "gear");
        Assert.True(w.M.Research.Knows(door));
        Assert.True(RecipeOf(w, "door").Known);
        Assert.Empty(RecipeOf(w, "door").Unanalysed);
    }

    [Fact]
    public void OpensTheSolarPanelOnceItsCircuitsAndMaterialsAreAnalysedAndBuildsItOfThem()
    {
        var (w, id) = SetUp();
        var panel = new Placement("machine", "solarPanel");
        var cost = Content.Machines.Get("solarPanel").Cost;
        w.GiveItems(0, ("circuit", cost.Single(c => c.Item == "circuit").Count));
        Analyse(w, id, "scrap");
        Analyse(w, id, "iron");
        Assert.Equal(["circuit"], RecipeOf(w, "solarPanel").Unanalysed);
        Assert.False(w.M.Research.Knows(panel));
        Analyse(w, id, "circuit");
        Assert.True(RecipeOf(w, "solarPanel").Known);
        Assert.True(w.M.Research.Knows(panel));
        // Paid in full from what the hero carries, the analysed circuit taken back out among it.
        w.GiveItems(0, ("scrap", 6), ("iron", 6));
        w.Do(new PlacePlan(4, panel)).Run();
        Assert.Empty(w.Refusals);
        int plan = w.M.Building.Plans.Single().Id;
        w.Do(new Work(plan)).Run();
        Assert.Empty(w.M.Building.Lacking(plan));
        for (int t = 0; t < Content.Machines.Get("solarPanel").BuildTicks; t++) w.Do(new Work(plan)).Run();
        Assert.Empty(w.Refusals);
        Assert.Equal("solarPanel", w.M.Grid.MachineAt(4)?.Kind);
        Assert.Equal(0, w.M.Items.Inventory(0).Sum(s => s?.Item == "circuit" ? s.Count : 0));
    }

    [Fact]
    public void KnowsWhatIsGrantedWithNothingAnalysedTheWireCraftAmongIt()
    {
        var (w, _) = SetUp();
        Assert.True(w.M.Research.Knows("wire"));
        Assert.True(RecipeOf(w, "wire").Granted);
        Assert.True(w.M.Research.Knows(new Placement("machine", "furnace")));
        Assert.False(w.M.Research.Knows("rotor"));
        Assert.False(w.M.Research.Knows("nothing"));
        Assert.Equal(Sorted(Content.Research.StartKnown), Sorted(w.M.Research.Recipes().Where(r => r.Known).Select(r => r.Id)));
    }

    [Fact]
    public void ListsEveryBlueprintThenEveryCraftWithItsCost()
    {
        var (w, _) = SetUp();
        var recipes = w.M.Research.Recipes();
        Assert.Equal(Blueprints.All.Select(b => b.Id).Concat(Content.Crafts.All.Select(c => c.Item)), recipes.Select(r => r.Id));
        Assert.Equal<(string, int)>(Content.Builds.Get("door").Cost, RecipeOf(w, "door").Cost);
        Assert.Equal(new Placement("structure", "door"), RecipeOf(w, "door").Placement);
        Assert.Null(RecipeOf(w, "rotor").Placement);
        Assert.Equal<(string, int)>(Content.Crafts.Get("rotor").Cost, RecipeOf(w, "rotor").Cost);
    }

    [Fact]
    public void RestsEmptyAndOnceWhatItHoldsIsAnalysedUsingNothingUp()
    {
        var (w, id) = SetUp();
        w.Run();
        Assert.Equal(0, Demand(w));
        // Told to rest while empty, it draws from the tick after the put, which was the first of its work.
        Put(w, id, "planks").Run();
        Assert.Equal(2, Demand(w));
        w.Run(Ticks - 2);
        Assert.Equal(["planks"], Analysed(w));
        w.Run();
        Assert.Equal(0, Demand(w));
        w.Run(Ticks);
        var at = w.M.Research.At(id);
        Assert.Equal((0, "research.nothingNew"), (at?.Progress, at?.Problem));
        Assert.Equal(new Stack("planks", 1), w.M.Items.Crate(id)[0]);
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
        Assert.Empty(Analysed(shortW));
        shortW.Run();
        Assert.Equal(["iron"], Analysed(shortW));
    }

    [Fact]
    public void StartsOverOnWhatIsTakenOutBeforeItIsAnalysed()
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
    public void AnalysesAnItemOnceThatTwoAnalyzersFinishOnOneTick()
    {
        var (w, id) = SetUp(StartMap(two: true));
        int second = w.M.Grid.MachineAt(Analyzer + 2)?.Id ?? 0;
        // Five against two analyzers drawing two each: both in full.
        int slot = w.M.Items.Inventory(0).ToList().FindIndex(s => s?.Item == "iron");
        w.Do(new Items.Move(new Items.Place(slot), new Target(Crate: id), 1));
        w.Do(new Items.Move(new Items.Place(slot), new Target(Crate: second), 1)).Run(Ticks);
        Assert.Equal(["iron"], Analysed(w));
        Assert.Equal("research.nothingNew", w.M.Research.At(second)?.Problem);
    }

    [Fact]
    public void KnowsEveryBlueprintAndCraftOnAMapThatNamesNone()
    {
        var (w, _) = SetUp(Map(null));
        Assert.All(w.M.Research.Recipes(), r => Assert.True(r.Known, r.Id));
        Assert.True(w.M.Research.Knows(new Placement("machine", "solarPanel")));
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
        Assert.True(w.M.Research.Knows("rotor"));
    }

    [Fact]
    public void SavesAndLoadsWhatIsAnalysedAndAnAnalysisUnderWay()
    {
        var (w, id) = SetUp();
        Analyse(w, id, "iron");
        Put(w, id, "scrap");
        w.Run(20);
        var before = w.World.Save();
        w.Reload();
        Assert.Equal(before, w.World.Save());
        Assert.Equal(["iron"], Analysed(w));
        Assert.Equal(21_000, w.M.Research.At(id)?.Progress);
    }

    /// <summary>A put of an item, a take-out, or a run of ticks.</summary>
    private sealed record Step(string? Put, bool Out, int Run);

    /// <summary>The README's invariant: analysed only grows, holds only items there are, each once, sorted.</summary>
    [Property(MaxTest = 40)]
    public Property OnlyEverAnalysesAndOnlyItemsThereAreWhateverGoesInAndOut()
    {
        var items = Content.Items.All.Select(i => i.Id).ToList();
        var step = Gen.OneOf(
            Gen.Elements("iron", "scrap", "plate", "gear", "wire", "planks").Select(item => new Step(item, false, 0)),
            Gen.Constant(new Step(null, true, 0)),
            Gen.Choose(1, 2 * Ticks).Select(n => new Step(null, false, n)));
        var steps = from n in Gen.Choose(0, 25) from s in step.ArrayOf(n) select s;
        return Prop.ForAll(steps.ToArbitrary(), all =>
        {
            var (w, id) = SetUp();
            w.Run();
            var before = Analysed(w).ToArray();
            foreach (var s in all)
            {
                if (s.Put != null) Put(w, id, s.Put);
                else if (s.Out) TakeOut(w, id);
                else w.Run(s.Run);
                var now = Analysed(w).ToArray();
                if (!before.All(now.Contains)) return false;
                if (!now.All(items.Contains)) return false;
                if (now.Distinct().Count() != now.Length) return false;
                if (!Sorted(now).SequenceEqual(now)) return false;
                before = now;
            }
            return true;
        });
    }
}
