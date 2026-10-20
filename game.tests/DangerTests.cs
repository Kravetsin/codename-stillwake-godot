using Stillwake.Game.Features.Crew;
using Stillwake.Sim.Crew;
using Xunit;
using Suits = Stillwake.Content.Crew;

namespace Stillwake.Game.Tests;

/// <summary>Suit danger: apps/game/src/features/crew/danger.test.ts, one for one.</summary>
public class DangerTests
{
    private static readonly int Low = (int)Math.Ceiling(Suits.Suit * Danger.Low) - 1;

    [Fact]
    public void IsNoneForAHeroNotYetInTheSimOrWithSuitToSpare()
    {
        Assert.Equal(SuitDanger.None, Danger.Of(null, false));
        Assert.Equal(SuitDanger.None, Danger.Of((Low + 1, 0), false));
    }

    [Fact]
    public void IsALowSuitOutOfRoomAirButNotInItWhereItFillsAgain()
    {
        Assert.Equal(SuitDanger.Low, Danger.Of((Low, 0), false));
        Assert.Equal(SuitDanger.None, Danger.Of((Low, 0), true));
    }

    [Fact]
    public void ReadsALowSuitByTheLifepodInVacuumAsNoDangerAndPastItsReachAsLow()
    {
        var w = new TestWorld("""
            #########
            #OOO....#
            #OOO....#
            #OOO....#
            #########
            """);
        int byPod = w.M.Crew.Spawn, away = 3 * 9 + 7;
        w.Step(new Enter(away) { From = 0 }).Step(new Enter(away) { From = 1 });
        for (int i = 0; i < (Suits.Suit - Low) * Suits.BreathEvery; i++) w.Step();
        w.Step(new Enter(byPod) { From = 1 });
        Assert.Equal((Low, SuitDanger.Low, false), Danger.Read(w.M.Crew, w.M.Atmosphere, 0));
        Assert.Equal(SuitDanger.None, Danger.Read(w.M.Crew, w.M.Atmosphere, 1).Danger);
    }

    [Fact]
    public void IsPassingOutWhateverTheAir() => Assert.Equal(SuitDanger.Out, Danger.Of((0, 40), true));
}
