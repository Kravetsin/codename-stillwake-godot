using Stillwake.Game.Features.Crew;
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
    public void IsPassingOutWhateverTheAir() => Assert.Equal(SuitDanger.Out, Danger.Of((0, 40), true));
}
