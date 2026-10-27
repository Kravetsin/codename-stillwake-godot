using Stillwake.Game.Features.Crew;
using Stillwake.Sim.Crew;
using Xunit;
using Suits = Stillwake.Content.Crew;

namespace Stillwake.Game.Tests;

/// <summary>Suit danger: apps/game/src/features/crew/danger.test.ts, one for one.</summary>
public class DangerTests
{
    private static readonly int Full = Stillwake.Content.Items.Get(Stillwake.Content.Suit.Basic).Tank;
    private static readonly int Low = (int)Math.Ceiling(Full * Danger.Low) - 1;

    [Fact]
    public void IsNoneForAHeroNotYetInTheSimOrWithSuitToSpare()
    {
        Assert.Equal(SuitDanger.None, Danger.Of(null, false));
        Assert.Equal(SuitDanger.None, Danger.Of((Low + 1, Full, 0), false));
    }

    [Fact]
    public void IsALowSuitOutOfRoomAirButNotInItWhereItFillsAgain()
    {
        Assert.Equal(SuitDanger.Low, Danger.Of((Low, Full, 0), false));
        Assert.Equal(SuitDanger.None, Danger.Of((Low, Full, 0), true));
    }

    [Fact]
    public void ReadsALowSuitByTheLifepodInVacuumAsNoDangerAndPastItsReachAsLow()
    {
        var w = new TestWorld(Stillwake.Sim.Layout.Parse("""
            #########
            #OOO....#
            #OOO....#
            #OOO....#
            #########
            """).Doubled());
        // A cell in the room past the pod's reach: the block (7, 3)'s bottom right, the map doubled (0104).
        int byPod = w.M.Crew.Spawn, away = 7 * 18 + 15;
        // Bare, as a peer's first heroes are: low by the lungs' capacity.
        int lungs = Suits.Lungs, low = (int)Math.Ceiling(lungs * Danger.Low) - 1;
        w.Step(new Enter(away) { From = 0 }).Step(new Enter(away) { From = 1 });
        while (w.M.Crew.HeroOf(0)!.Air > low) w.Step();
        w.Step(new Enter(byPod) { From = 1 });
        var read = Danger.Read(w.M.Crew, w.M.Atmosphere, 0);
        Assert.Equal((lungs, SuitDanger.Low, false), (read.Capacity, read.Danger, read.InAir));
        Assert.True(read.Air <= low);
        Assert.Equal(SuitDanger.None, Danger.Read(w.M.Crew, w.M.Atmosphere, 1).Danger);
    }

    [Fact]
    public void IsPassingOutWhateverTheAir() => Assert.Equal(SuitDanger.Out, Danger.Of((0, Full, 40), true));
}
