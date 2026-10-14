using Stillwake.Game.Features.Items;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>What an analyzer's panel teaches: what its item opened, what is analysed, what is one item short.</summary>
public class LessonsTests
{
    /// <summary>A solar panel cabled to an analyzer in a room, the world granted what the wreck is.</summary>
    private static (TestWorld W, int Analyzer) World()
    {
        var layout = Layout.Parse(["PPP       ", "          ", "##########", "#AA......#", "#AA......#", "##########"], null, [((0, 0), (1, 3))]);
        var w = new TestWorld(layout with { Known = Content.Research.StartKnown }).Give(0, ("iron", 1));
        return (w, w.M.Grid.MachineAt(31)!.Id);
    }

    [Fact]
    public void ShowNothingOpenedUntilTheItemIsAnalysedThenWhatIsMadeOfItButWhatWasGranted()
    {
        var (w, analyzer) = World();
        w.Step(new Move(new Place(0), new Target(Crate: analyzer), 1));
        Assert.Empty(Lessons.Of(w.M.Research, "iron").Opened);
        for (int i = 0; i < Content.Research.Ticks; i++) w.Step();
        var lessons = Lessons.Of(w.M.Research, "iron");
        Assert.Equal(["plate", "gear"], lessons.Opened.Select(r => r.Id));
        Assert.Equal(["iron"], lessons.Analysed);
        // Scrap opens the floor; the wire, made of it too, was granted and opens nothing.
        Assert.Contains(lessons.OneShort, s => s.Recipe.Id == "floor" && s.Item == "scrap");
        Assert.DoesNotContain(lessons.OneShort, s => s.Recipe.Id == "wire");
        // The door needs plates and a gear: two short. The solar panel is locked.
        Assert.DoesNotContain(lessons.OneShort, s => s.Recipe.Id is "door" or "solarPanel");
    }

    [Fact]
    public void ShowOnlyWhatIsOneShortForAnEmptyAnalyzer()
    {
        var (w, _) = World();
        var lessons = Lessons.Of(w.M.Research, null);
        Assert.Empty(lessons.Opened);
        Assert.Empty(lessons.Analysed);
        Assert.Contains(lessons.OneShort, s => s.Recipe.Id == "plate" && s.Item == "iron");
    }
}
