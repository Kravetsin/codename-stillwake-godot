using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Atmosphere;
using Stillwake.Sim.Building;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Xunit;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Tests;

/// <summary>The prompt for E: apps/game/src/features/crew/prompt.test.ts.</summary>
public class PromptTests
{
    private static readonly Registry M = new TestWorld("""
        #########
        #.d.D.vx#
        #########
        """).M;

    private static int At(int x, int y) => y * 9 + x;

    private static Aim Press(int cell, Command command, string? problem = null) => new(cell, true, new Use(command, problem));

    [Fact]
    public void SaysWhatAPressDoesByWhatItWouldMakeOfIt()
    {
        var doors = new[] { false, true }.Select(open => Prompts.Of(M, Press(At(2, 1), new SetDoor(At(2, 1), open)))?.Action);
        Assert.Equal(["use.closeDoor", "use.openDoor"], doors);
        var valves = new[] { false, true }.Select(open => Prompts.Of(M, Press(At(6, 1), new Sim.Pipes.SetValve(At(6, 1), open)))?.Action);
        Assert.Equal(["use.closeValve", "use.openValve"], valves);
        Assert.Equal(new Prompt("use.loadCanister", "items.notEnough"), Prompts.Of(M, Press(3, new LoadCanister(1), "items.notEnough")));
        Assert.Equal("use.catch", Prompts.Of(M, Press(3, new Grab(1)))?.Action);
        Assert.Equal("use.openCrate", Prompts.Of(M, new Aim(3, true, Crate: 4))?.Action);
    }

    [Fact]
    public void SaysWhetherHeldEBuildsOrTakesApart()
    {
        var planned = new TestWorld("#...#");
        planned.Step(new PlacePlan(2, new Placement("structure", "floor")), new Deconstruct(0, "structure"));
        static Aim Work(int plan) => new(0, true, Work: new WorkAim(plan, null));
        Assert.Equal(["use.build", "use.takeApart"], new[] { 1, 2 }.Select(plan => Prompts.Of(planned.M, Work(plan))?.Action));
    }

    [Fact]
    public void OffersNothingOutOfReachOrWithNothingToDo()
    {
        var door = new SetDoor(At(2, 1), false);
        Assert.Null(Prompts.Of(M, Press(At(2, 1), door) with { InReach = false }));
        Assert.Null(Prompts.Of(M, new Aim(3, true)));
        Assert.Null(Prompts.Of(M, null));
    }

    [Fact]
    public void HasWordsForEveryActionAndTellsTwoPromptsApart()
    {
        string[] actions = ["use.build", "use.takeApart", "use.openCrate", "use.loadCanister", "use.catch", "use.openDoor", "use.closeDoor", "use.openValve", "use.closeValve"];
        Assert.All(actions, action => Assert.True(Table.ContainsKey(action), action));
        var a = new Prompt("use.catch", null);
        Assert.Equal(a, a with { });
        Assert.NotEqual(a, a with { Problem = "debris.gone" });
        Assert.Equal((Prompt?)null, (Prompt?)null);
    }
}
