using Stillwake.Sim.Pipes;
using Stillwake.Sim.Water;
using Xunit;
using WaterContent = Stillwake.Content.Water;

namespace Stillwake.Sim.Tests;

/// <summary>Cisterns in a line (tanks-in-a-line): a cistern piped from its output to another's input pours its water on.</summary>
public class CisternLineTests
{
    /// <summary>A cell a character: cisterns A, B and C, inputs (0, 2), (6, 2), (12, 2) and outputs (3, 2), (9, 2), (15, 2).</summary>
    private const string Line = """
        cccc..cccc..cccc
        cccc..cccc..cccc
        cccc..cccc..cccc
        cccc..cccc..cccc
        """;

    /// <summary>
    /// A cell a character: cistern A at (0, 0), its output (3, 2), and cistern B at (0, 6), its output (3, 8), both behind the
    /// electrolyser's input (6, 5); its output (9, 5) to the tank's input (12, 2); two panels out in space cabled to it.
    /// </summary>
    private static readonly string[] Two =
    [
        "cccc..yyyy..TTTT",
        "cccc..yyyy..TTTT",
        "cccc..yyyy..TTTT",
        "cccc..yyyy..TTTT",
        "......yyyy......",
        "......yyyy......",
        "cccc............",
        "cccc............",
        "cccc............",
        "cccc............",
        "                ",
        "PPPPPPPPPPPP    ",
        "PPPPPPPPPPPP    ",
    ];

    private static Scenario Piped()
    {
        var w = new Scenario(Line, cells: true).GiveItems(0, (WaterContent.Item, WaterContent.CisternBottles + 1), ("tube", 20));
        return w.Do(new LayPipe(w.At(3, 2), w.At(6, 2))).Do(new LayPipe(w.At(9, 2), w.At(12, 2))).Run();
    }

    private static int[] Cisterns(Scenario w) => [.. w.M.Grid.MachinesOf("cistern").Select(m => m.Id)];
    private static int[] Held(Scenario w) => [.. Cisterns(w).Select(w.M.Water.WaterIn)];

    [Fact]
    public void PoursALineIntoItsLastCisternCisternFlowATick()
    {
        var w = Piped();
        Assert.Empty(w.Refusals);
        w.Do(new PourWater(Cisterns(w)[0])).Run();
        // A gives B its flow and B gives it on to C in the same tick.
        Assert.Equal([WaterContent.Bottle - WaterContent.CisternFlow, 0, WaterContent.CisternFlow], Held(w));
        w.Run(WaterContent.Bottle / WaterContent.CisternFlow);
        Assert.Equal([0, 0, WaterContent.Bottle], Held(w));
    }

    [Fact]
    public void FillsTheLastCisternThenTheOneBeforeIt()
    {
        var w = Piped();
        int[] cisterns = Cisterns(w);
        for (int i = 0; i < WaterContent.CisternBottles; i++) w.Do(new PourWater(cisterns[0]));
        w.Do(new PourWater(cisterns[1])).Run((WaterContent.CisternBottles + 1) * WaterContent.Bottle / WaterContent.CisternFlow);
        Assert.Equal([0, WaterContent.Bottle, WaterContent.CisternCapacity], Held(w));
    }

    [Fact]
    public void PoursNothingThroughAnUnpipedLine()
    {
        var w = new Scenario(Line, cells: true).GiveItems(0, (WaterContent.Item, WaterContent.CisternBottles + 1), ("tube", 20));
        w.Do(new LayPipe(w.At(9, 2), w.At(12, 2))).Run();
        int[] cisterns = Cisterns(w);
        w.Do(new PourWater(cisterns[0])).Do(new PourWater(cisterns[2])).Run(100);
        Assert.Equal([WaterContent.Bottle, 0, WaterContent.Bottle], Held(w));
    }

    [Fact]
    public void AnElectrolyserSplitsTheWaterOfALaterCisternPastAnEmptyOne()
    {
        var w = new Scenario(Layout.Parse(Two, null, [((0, 11), (6, 0)), ((6, 11), (6, 0))]), doubled: false)
            .GiveItems(0, (WaterContent.Item, 2), ("tube", 30));
        w.Do(new LayPipe(w.At(3, 2), w.At(6, 5))).Do(new LayPipe(w.At(3, 8), w.At(6, 5))).Do(new LayPipe(w.At(9, 5), w.At(12, 2))).Run();
        Assert.Empty(w.Refusals);
        int b = Cisterns(w)[1], tank = w.M.Grid.MachinesOf("tank")[0].Id;
        w.Do(new PourWater(b)).Run(30);
        Assert.Equal(WaterContent.Bottle - 30, w.M.Water.WaterIn(b));
        Assert.Equal(30, w.M.Atmosphere.OxygenIn(tank));
    }
}
