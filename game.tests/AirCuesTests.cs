using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Crew;
using Xunit;
using CrewSpec = Stillwake.Content.Crew;

namespace Stillwake.Game.Tests;

/// <summary>Air coming back is shown once (0081): a canister breathed, room air breathed again, the danger letting go, a friend's canister.</summary>
public class AirCuesTests
{
    private const double Read = 0.25;

    [Fact]
    public void BreathesACanisterOnceUntilTheFirstHasLanded()
    {
        var refill = Report.RefillOf(null, []);
        Assert.NotNull(refill);
        // Clicked again before the host runs it: nothing more is sent, so nothing more is shown or heard.
        Assert.Null(Report.RefillOf(null, [refill]));
        Assert.Null(Report.RefillOf(null, [new Exert(true), refill]));
        Assert.NotNull(Report.RefillOf(null, [new Exert(true)]));
    }

    [Fact]
    public void BreathesNoCanisterTheSimWouldRefuse()
    {
        Assert.Null(Report.RefillOf("crew.suitFull", []));
        Assert.Null(Report.RefillOf("items.notEnough", []));
    }

    [Fact]
    public void BreathesAgainOnceAsTheHeroComesOutOfVacuumIntoAir()
    {
        var cues = new AirCues();
        Assert.False(cues.Read(30, SuitDanger.None, false, Read).BreathesAgain);
        Assert.False(cues.Read(29, SuitDanger.None, false, Read).BreathesAgain);
        Assert.True(cues.Read(29, SuitDanger.None, true, Read).BreathesAgain);
        // Every breath after it refills as ever, with no wave.
        for (int i = 0; i < 40; i++) Assert.False(cues.Read(30 + i / 4, SuitDanger.None, true, Read).BreathesAgain);
    }

    [Fact]
    public void ShowsNothingOnTheFirstReadNorWithAFullSuitNorPassedOut()
    {
        Assert.False(new AirCues().Read(20, SuitDanger.None, true, Read).BreathesAgain);
        var full = new AirCues();
        full.Read(CrewSpec.Suit, SuitDanger.None, false, Read);
        Assert.False(full.Read(CrewSpec.Suit, SuitDanger.None, true, Read).BreathesAgain);
        var out_ = new AirCues();
        out_.Read(0, SuitDanger.Out, false, Read);
        Assert.False(out_.Read(0, SuitDanger.Out, true, Read).BreathesAgain);
    }

    [Fact]
    public void WavesOnceForAHeroSteppingToAndFroOverASill()
    {
        var cues = new AirCues();
        cues.Read(20, SuitDanger.None, false, Read);
        int waves = 0;
        double spent = 0;
        // In and out each read, for less than the gap: one wave.
        for (int i = 0; spent + Read < WristLook.AgainGapS; i++, spent += Read)
            if (cues.Read(20, SuitDanger.None, i % 2 == 0, Read).BreathesAgain) waves++;
        Assert.Equal(1, waves);
        // Out a while past the gap, then in: again.
        cues.Read(20, SuitDanger.None, false, WristLook.AgainGapS);
        Assert.True(cues.Read(20, SuitDanger.None, true, Read).BreathesAgain);
    }

    [Fact]
    public void LetsGoOnceAsALowSuitStopsBeingInDangerButNotOnPassingOut()
    {
        var cues = new AirCues();
        cues.Read(8, SuitDanger.Low, false, Read);
        Assert.True(cues.Read(28, SuitDanger.None, false, Read).LetsGo);
        Assert.False(cues.Read(28, SuitDanger.None, false, Read).LetsGo);
        var dying = new AirCues();
        dying.Read(1, SuitDanger.Low, false, Read);
        Assert.False(dying.Read(0, SuitDanger.Out, false, Read).LetsGo);
        // Waking at the pod is no danger let go: it went through passing out.
        Assert.False(dying.Read(CrewSpec.Suit, SuitDanger.None, true, Read).LetsGo);
    }

    [Fact]
    public void SeesAFriendsCanisterOnceByItsSuitJumping()
    {
        var seen = new CanistersSeen();
        Assert.False(seen.Breathed(1, 10, 0));
        Assert.True(seen.Breathed(1, 10 + CrewSpec.CanisterRefill, 0));
        Assert.False(seen.Breathed(1, 10 + CrewSpec.CanisterRefill, 0));
        // Room air and the pod's together in one breath are no canister.
        Assert.False(seen.Breathed(1, 30 + CanistersSeen.MostByAir, 0));
    }

    [Fact]
    public void SeesNoCanisterInWakingNorInAHeroReadAfresh()
    {
        var seen = new CanistersSeen();
        seen.Breathed(2, 0, 3);
        Assert.False(seen.Breathed(2, CrewSpec.Suit, 0));
        seen.Breathed(3, 5, 0);
        seen.Sweep();
        // Hero 3 was not read in the frame swept: gone a while, it comes back with more air and no canister is seen.
        seen.Sweep();
        Assert.False(seen.Breathed(3, 40, 0));
    }
}
