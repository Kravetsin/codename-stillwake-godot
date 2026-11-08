using Stillwake.Game;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A hero's hurts as shown (2026-11-08-hurts-are-shown): each new one once, in its kind's colour, the edge and the blink timed.</summary>
public class HurtSeenTests
{
    [Fact]
    public void ACountRisenSinceTheLastReadIsAHurtOfTheNewestKind()
    {
        var seen = new HurtSeen();
        Assert.Null(seen.Read((3, Harm.Blow)));
        Assert.Null(seen.Read((3, Harm.Blow)));
        Assert.Equal(Harm.Poison, seen.Read((5, Harm.Poison)));
        Assert.Null(seen.Read((5, Harm.Poison)));
    }

    [Fact]
    public void NoHeroOrACountThatFellShowsNothingAndStartsAgain()
    {
        var seen = new HurtSeen();
        seen.Read((4, Harm.Burn));
        Assert.Null(seen.Read((0, Harm.None)));
        Assert.Equal(Harm.Shock, seen.Read((1, Harm.Shock)));
        Assert.Null(seen.Read(null));
        Assert.Null(seen.Read((2, Harm.Shock)));
    }

    [Fact]
    public void TheEdgeRisesFastFadesByItsEndAndTheBlinkStopsWhite()
    {
        Assert.Equal(0, HurtLook.Edge(-0.01));
        Assert.Equal(HurtLook.Most, HurtLook.Edge(HurtLook.RiseS), 6);
        Assert.True(HurtLook.Edge(0.3) is > 0 and < HurtLook.Most);
        Assert.Equal(0, HurtLook.Edge(HurtLook.FadeS));
        Assert.Equal(1, HurtLook.Blink(0), 6);
        Assert.Equal(0, HurtLook.Blink(HurtLook.BlinkS));
    }
}
