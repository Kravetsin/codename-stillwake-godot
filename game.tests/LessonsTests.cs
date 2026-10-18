using Stillwake.Game.Features.Items;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Xunit;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Tests;

/// <summary>What an analyzer's panel shows: its stage, how far it is, and what the item in it goes into once analysed.</summary>
public class LessonsTests
{
    /// <summary>An analyzer in a room, cabled to a solar panel unless <paramref name="powered"/> is false; the world granted what the wreck is.</summary>
    private static (TestWorld W, int Analyzer) World(bool powered = true)
    {
        string[] map = ["PPP       ", "          ", "##########", "#AA......#", "#AA......#", "##########"];
        var layout = Layout.Parse(map, null, powered ? [((0, 0), (1, 3))] : []);
        var w = new TestWorld(layout with { Known = Content.Research.StartKnown }).Give(0, ("iron", 2), ("scrap", 1));
        return (w, w.M.Grid.MachineAt(31)!.Id);
    }

    private static Lessons Shown(TestWorld w, int analyzer) =>
        Lessons.Of(w.M.Research, w.M.Items.Crate(analyzer)[0]?.Item, w.M.Research.At(analyzer));

    private static void Analyse(TestWorld w, int analyzer, int from)
    {
        w.Step(new Move(new Place(from), new Target(Crate: analyzer), 1));
        for (int i = 0; i < Content.Research.Ticks; i++) w.Step();
        w.Step(new Move(new Place(0, analyzer), new Target(), 1));
    }

    [Fact]
    public void ListNothingForAnEmptyAnalyzer()
    {
        var (w, analyzer) = World();
        var lessons = Shown(w, analyzer);
        Assert.Equal((Stage.Empty, (string?)null, 0), (lessons.Stage, lessons.Item, lessons.Percent));
        Assert.Empty(lessons.Recipes);
    }

    [Fact]
    public void ShowAnItemJustPutInAtWorkFromNothingBeforeItsTickRuns()
    {
        var (w, _) = World();
        var lessons = Lessons.Of(w.M.Research, "iron", null);
        Assert.Equal((Stage.Working, 0), (lessons.Stage, lessons.Percent));
        Assert.Empty(lessons.Recipes);
    }

    [Fact]
    public void ShowHowFarTheWorkIsRoundedDownAndListNothingUntilItIsDone()
    {
        var (w, analyzer) = World();
        w.Step(new Move(new Place(0), new Target(Crate: analyzer), 1));
        for (int i = 0; i < Content.Research.Ticks / 3; i++) w.Step();
        var at = w.M.Research.At(analyzer)!;
        var lessons = Shown(w, analyzer);
        Assert.Equal(Stage.Working, lessons.Stage);
        Assert.Equal((int)((long)at.Progress * 100 / at.Work), lessons.Percent);
        Assert.InRange(lessons.Percent, 1, 99);
        Assert.Empty(lessons.Recipes);
    }

    [Fact]
    public void SayAnAnalyzerWithoutPowerIsUnpowered()
    {
        var (w, analyzer) = World(powered: false);
        w.Step(new Move(new Place(0), new Target(Crate: analyzer), 1));
        w.Step();
        Assert.Equal(Stage.Unpowered, Shown(w, analyzer).Stage);
    }

    [Fact]
    public void ListEveryBlueprintAndCraftTheAnalysedItemGoesIntoOpenedOrNot()
    {
        var (w, analyzer) = World();
        w.Step(new Move(new Place(0), new Target(Crate: analyzer), 1));
        for (int i = 0; i < Content.Research.Ticks; i++) w.Step();
        var lessons = Shown(w, analyzer);
        Assert.Equal((Stage.Done, "iron", 100), (lessons.Stage, lessons.Item, lessons.Percent));
        // Exactly what has iron in its cost, blueprints first, in content's order.
        var expected = w.M.Research.Recipes().Where(r => r.Cost.Any(c => c.Item == "iron")).Select(r => r.Id);
        Assert.Equal(expected, lessons.Recipes.Select(l => l.Recipe.Id));
        Assert.Contains("plate", expected);
        Assert.DoesNotContain("wire", expected);
        // Granted and opened alike are open: since the solar panel is built of solar cells (0057), nothing iron goes into waits for more.
        Assert.DoesNotContain(lessons.Recipes, l => !l.Opened);
        Assert.True(lessons.Recipes.Single(l => l.Recipe.Id == "crate").Opened);
    }

    [Fact]
    public void ShowAnItemAnalysedAlreadyAsDoneAtOnceWhenItIsPutBackIn()
    {
        var (w, analyzer) = World();
        Analyse(w, analyzer, 0);
        Assert.Equal(Stage.Empty, Shown(w, analyzer).Stage);
        var lessons = Lessons.Of(w.M.Research, "iron", null);
        Assert.Equal(Stage.Done, lessons.Stage);
        Assert.NotEmpty(lessons.Recipes);
    }

    [Fact]
    public void TipAnOpenedOneAsOpenedAndAClosedOneWithEachItemItWaitsFor()
    {
        var (w, analyzer) = World();
        Analyse(w, analyzer, 0);
        var lessons = Lessons.Of(w.M.Research, "iron", null);
        Assert.Equal(new Tip(T("item.plate"), new TipLine(T("research.opened"), Tone.Good)),
            lessons.Recipes.Single(l => l.Recipe.Id == "plate").Tip);
        Analyse(w, analyzer, 1);
        Assert.Equal(new Tip(T("tool.burnerGenerator"), new TipLine("Frame not analysed", Tone.Unknown, "frame"), new TipLine("Rotor not analysed", Tone.Unknown, "rotor"), new TipLine("Coil not analysed", Tone.Unknown, "coil")),
            Lessons.Of(w.M.Research, "scrap", null).Recipes.Single(l => l.Recipe.Id == "burnerGenerator").Tip);
    }

    [Fact]
    public void OpenTheLampOnceItsWireIsAnalysedToo()
    {
        var (w, analyzer) = World();
        w.Give(0, ("iron", 2), ("scrap", 1), ("wire", 1));
        Analyse(w, analyzer, 1);
        var lamp = Lessons.Of(w.M.Research, "scrap", null).Recipes.Single(l => l.Recipe.Id == "lamp");
        Assert.False(lamp.Opened);
        Assert.Equal([new TipLine("Wire not analysed", Tone.Unknown, "wire")], lamp.Tip.Lines);
        Analyse(w, analyzer, 2);
        Assert.True(Lessons.Of(w.M.Research, "wire", null).Recipes.Single(l => l.Recipe.Id == "lamp").Opened);
    }

    [Fact]
    public void OpenEveryOneInCreativeMode()
    {
        var (w, analyzer) = World();
        Analyse(w, analyzer, 0);
        w.Step(new SetCreative(true));
        Assert.All(Lessons.Of(w.M.Research, "iron", null).Recipes, l => Assert.True(l.Opened));
    }
}
