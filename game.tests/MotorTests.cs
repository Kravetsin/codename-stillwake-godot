using System.Text.Json.Nodes;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>An electric tool in the hand runs on the left button, the sim told by <c>Run</c> while the two differ (0055).</summary>
public class MotorTests
{
    private const string Drill = "drill";
    private static readonly int Belt = Content.Inventory.Slots + 2;
    private static readonly int Tick = Content.Items.Get(Drill).Draw!.Value * Content.Charge.Scale;

    /// <summary>Peer 0 aboard with the drill and a battery holding <paramref name="charge"/> on its belt; none for none.</summary>
    private static TestWorld Holding(int? charge)
    {
        var w = new TestWorld("S....").Give(0, (Drill, 1));
        if (charge is int held)
            w.Edit(root => root["slices"]!["items"]!["inventories"]![0]!["slots"]![Belt] =
                new JsonObject { ["item"] = "battery", ["count"] = 1, ["charge"] = held });
        return w.Step(new Sim.Crew.Enter(1));
    }

    [Fact]
    public void WantsTheToolHeldRunWhileTheLeftButtonHoldsItAndTheBeltCanPayATick()
    {
        var m = Holding(Tick).M.Items;
        Assert.Equal(Drill, Motor.Wanted(m, 0, Drill, holdsLeft: true, grabHeld: false));
        // Let go, put away, a hold begun by a grab, or a tool with no draw: nothing runs.
        Assert.Null(Motor.Wanted(m, 0, Drill, holdsLeft: false, grabHeld: false));
        Assert.Null(Motor.Wanted(m, 0, null, holdsLeft: true, grabHeld: false));
        Assert.Null(Motor.Wanted(m, 0, Drill, holdsLeft: true, grabHeld: true));
        Assert.Null(Motor.Wanted(m, 0, "fireAxe", holdsLeft: true, grabHeld: false));
        // A belt short of a tick runs nothing, but in creative mode nothing is paid.
        var flat = Holding(Tick - 1);
        Assert.Null(Motor.Wanted(flat.M.Items, 0, Drill, holdsLeft: true, grabHeld: false));
        var bare = Holding(null).Step(new SetCreative(true));
        Assert.Equal(Drill, Motor.Wanted(bare.M.Items, 0, Drill, holdsLeft: true, grabHeld: false));
    }

    [Fact]
    public void SaysRunOnForWhatItWantsAndOffForWhatTheSimRunsWhileTheyDiffer()
    {
        var w = Holding(100 * Tick);
        var m = w.M.Items;
        Assert.Null(Motor.RunOf(m, 0, null));
        Assert.Equal(new Run(Drill, true), Motor.RunOf(m, 0, Drill));
        // Until the host releases it, asked again it says the same.
        Assert.Equal(new Run(Drill, true), Motor.RunOf(m, 0, Drill));
        w.Step(new Run(Drill, true));
        Assert.Null(Motor.RunOf(w.M.Items, 0, Drill));
        Assert.Equal(new Run(Drill, false), Motor.RunOf(w.M.Items, 0, null));
        w.Step(new Run(Drill, false));
        Assert.Null(Motor.RunOf(w.M.Items, 0, null));
    }

    [Fact]
    public void FollowsThePressAndTheReleaseThroughALaggingHostAndEndsAgreeing()
    {
        // A frame a tick, the host two frames behind: pressed on frames 3 to 9, then let go.
        var w = Holding(100 * Tick);
        var flying = new Queue<(int Due, Command Sent)>();
        var sent = new List<(int Frame, Command Sent)>();
        var ran = new List<int>();
        for (int frame = 0; frame < 20; frame++)
        {
            var due = new List<Command>();
            while (flying.Count > 0 && flying.Peek().Due == frame) due.Add(flying.Dequeue().Sent);
            w.Step([.. due]);
            bool pressed = frame is >= 3 and <= 9;
            var wanted = Motor.Wanted(w.M.Items, 0, Drill, pressed, grabHeld: false);
            if (Motor.RunOf(w.M.Items, 0, wanted) is { } run)
            {
                flying.Enqueue((frame + 2, run));
                sent.Add((frame, run));
            }
            if (w.M.Items.Running(0) != null) ran.Add(frame);
        }
        // On from the press till it lands, off from the release till that lands: sent each frame they differ.
        Assert.Equal([3, 4, 10, 11], sent.Select(s => s.Frame));
        Assert.Equal([true, true, false, false], sent.Select(s => ((Run)s.Sent).On));
        Assert.Equal(Enumerable.Range(5, 7), ran);
        Assert.Null(w.M.Items.Running(0));
    }

    [Fact]
    public void SendsNothingOnceTheSimStopsAFlatRun()
    {
        var w = Holding(2 * Tick).Step(new Run(Drill, true)).Step().Step();
        Assert.Null(w.M.Items.Running(0));
        var wanted = Motor.Wanted(w.M.Items, 0, Drill, holdsLeft: true, grabHeld: false);
        Assert.Null(wanted);
        Assert.Null(Motor.RunOf(w.M.Items, 0, wanted));
    }
}
