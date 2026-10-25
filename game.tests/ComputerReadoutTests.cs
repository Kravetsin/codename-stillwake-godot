using Stillwake.Content;
using Stillwake.Game.Features.Computer;
using Stillwake.Game.I18n;
using Stillwake.Sim;
using Xunit;
using Pairs = System.Collections.Generic.IReadOnlyList<((int X, int Y) A, (int X, int Y) B)>;

namespace Stillwake.Game.Tests;

/// <summary>The ship's computer as its screen reads it: its power, the radar antenna on its network, and what it says.</summary>
public class ComputerReadoutTests
{
    /// <summary>A panel (5) and the antenna (1) out on the roof; in the room the computer (2) and three lamps (1 each).</summary>
    private static readonly string[] Rows =
    [
        "PPP NN      ",
        "    NN      ",
        "############",
        "#UUU.LLL...#",
        "#UUU.......#",
        "############",
    ];
    private static readonly (int X, int Y) Panel = (0, 0), Antenna = (4, 1), Computer = (1, 3);
    private static readonly (int X, int Y)[] Lamps = [(5, 3), (6, 3), (7, 3)];

    private static (TestWorld W, Reading Reading) Read(params ((int, int), (int, int))[] cables)
    {
        Pairs pairs = [.. cables];
        var w = new TestWorld(Layout.Parse(Rows, null, pairs).Doubled());
        w.Step().Step();
        int computer = w.M.Grid.MachinesWith(MachineTrait.Computer)[0].Id;
        return (w, Readout.Of(w.M.Grid, w.M.Power, computer));
    }

    [Fact]
    public void WithNoPowerSaysSoBeforeAllElse()
    {
        var (_, reading) = Read((Antenna, Computer));
        Assert.False(reading.Powered);
        Assert.Equal(new Screen("computer.noPower", null), Readout.ScreenOf(reading, Programs.Radar));
    }

    [Fact]
    public void WithNoModuleOnItsNetworkHasNoButtonsAndSaysNoneWasFound()
    {
        // The antenna stands on the roof, cabled to nothing.
        var (_, reading) = Read((Panel, Computer));
        Assert.True(reading.Powered);
        Assert.Empty(reading.Buttons);
        Assert.Equal(new Screen("computer.noModules", null), Readout.ScreenOf(reading, null));
        Assert.False(Readout.Ready(reading, Programs.Radar));
    }

    [Fact]
    public void WithTheAntennaOnItsNetworkFullyPoweredHasAButtonForEveryProgramAndShowsTheRadarOnceChosen()
    {
        var (_, reading) = Read((Panel, Computer), (Panel, Antenna));
        Assert.Equal(Programs.All.Count, reading.Buttons.Count);
        Assert.Equal(new ProgramButton(Programs.All[0], ProgramState.Ready), reading.Buttons[0]);
        Assert.Equal(new Screen("computer.select", null), Readout.ScreenOf(reading, null));
        Assert.True(Readout.Ready(reading, Programs.Radar));
        Assert.Equal(new Screen(null, Programs.Radar), Readout.ScreenOf(reading, Programs.Radar));
    }

    [Fact]
    public void WithItsNetworkShortTheRadarButtonStandsShortAndTheScreenSaysWhyEvenWhileTheRadarIsChosen()
    {
        // Three lamps more: 6 drawn of the panel's 5, every user slowed alike, the computer's screen still lit.
        var (_, reading) = Read([(Panel, Computer), (Panel, Antenna), .. Lamps.Select(l => (Panel, l))]);
        Assert.True(reading.Powered);
        Assert.Equal(ProgramState.Short, reading.Buttons[0].State);
        Assert.False(Readout.Ready(reading, Programs.Radar));
        Assert.Equal(new Screen("computer.short.radar", null), Readout.ScreenOf(reading, Programs.Radar));
    }

    [Fact]
    public void ReadsAComputerTakenDownAsUnpoweredAndSaysEveryLineItCanInWords()
    {
        var (w, _) = Read((Panel, Computer));
        Assert.Equal(new Reading(false, []), Readout.Of(w.M.Grid, w.M.Power, -1));
        foreach (var key in (string[])["computer.noPower", "computer.noModules", "computer.select", "use.open.computer"])
            Assert.True(En.Table.ContainsKey(key), key);
        foreach (var program in Programs.All)
        {
            Assert.True(En.Table.ContainsKey($"computer.short.{program.Id}"), program.Id);
            Assert.True(En.Table.ContainsKey($"computer.program.{program.Id}"), program.Id);
            Assert.True(Machines.TryGet(program.Module, out _), program.Module);
        }
    }
}
