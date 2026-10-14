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
    public void SaysWhatAPressDoesByWhatIsThereAndHowItStands()
    {
        var doors = new[] { At(2, 1), At(4, 1) }.Select(cell => Prompts.Of(M, Press(cell, new ToggleDoor(cell)))?.Action);
        Assert.Equal(["use.closeDoor", "use.openDoor"], doors);
        var valves = new[] { At(6, 1), At(7, 1) }.Select(cell => Prompts.Of(M, Press(cell, new ToggleValve(cell)))?.Action);
        Assert.Equal(["use.closeValve", "use.openValve"], valves);
        Assert.Equal(new Prompt("use.loadCanister", "items.notEnough"), Prompts.Of(M, Press(3, new LoadCanister(1), "items.notEnough")));
        Assert.Equal("use.catch", Prompts.Of(M, Press(3, new Grab(1)))?.Action);
        Assert.Equal("use.openCrate", Prompts.Of(M, new Aim(3, true, Crate: 4))?.Action);
    }

    [Fact]
    public void SaysWhatTheNextPressDoesToADoorOrValveWhoseOwnToggleIsInFlight()
    {
        var m = new Registry();
        var client = new Client(World.Create(1, Layout.Parse("#########\n#.d.D.vx#\n#########"), m.All), new Fed(), 1);
        client.Send(new ToggleDoor(At(2, 1)));
        client.Send(new ToggleValve(At(7, 1)));
        var shown = Predicted.Grid(client, m.Grid);
        Assert.Equal("use.openDoor", Prompts.Of(m, Press(At(2, 1), new ToggleDoor(At(2, 1))), shown)?.Action);
        Assert.Equal("use.closeValve", Prompts.Of(m, Press(At(7, 1), new ToggleValve(At(7, 1))), shown)?.Action);
        // Doors and valves with nothing in flight read as they stand.
        Assert.Equal("use.openDoor", Prompts.Of(m, Press(At(4, 1), new ToggleDoor(At(4, 1))), shown)?.Action);
        Assert.Equal("use.closeValve", Prompts.Of(m, Press(At(6, 1), new ToggleValve(At(6, 1))), shown)?.Action);
    }

    [Fact]
    public void SaysWhetherHeldEBuildsOrTakesApart()
    {
        var planned = new TestWorld("#...#");
        planned.Step(new PlacePlan(2, new Placement("conduit", "pipe")), new Deconstruct(0, "structure"));
        static Aim Work(int plan) => new(0, true, Work: new WorkAim(plan, null));
        Assert.Equal(["use.build", "use.takeApart"], new[] { 1, 2 }.Select(plan => Prompts.Of(planned.M, Work(plan))?.Action));
    }

    [Fact]
    public void OffersNothingOutOfReachOrWithNothingToDo()
    {
        var door = new ToggleDoor(At(2, 1));
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
