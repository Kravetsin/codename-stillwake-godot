using Stillwake.Game.Features.Landing;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Which drop pods have just come down on the surface seen, and for how long they are held back from the grid.</summary>
public class ArrivalsTests
{
    private const double Frame = 1.0 / 60;
    private const int Planet = 1, Ship = 0;

    private static Arrivals Opened(params (int Pod, bool Seated)[] pods)
    {
        var a = new Arrivals();
        a.Frame(Planet, false, pods, null, newWorld: true, Frame);
        return a;
    }

    [Fact]
    public void NothingArrivesInAWorldJustLoadedOrJoined()
    {
        var a = Opened((7, true));
        Assert.False(a.Holds(7));
        a.Frame(Planet, false, [(7, true)], null, false, Frame);
        Assert.False(a.Holds(7));
    }

    [Fact]
    public void AFriendsPodSeatedOnArrivalComesDownHeldTillItsRelease()
    {
        var a = Opened();
        a.Frame(Planet, false, [(7, true)], null, false, Frame);
        Assert.True(a.Holds(7));
        Assert.Equal(0, a.SecondsSince(7));
        for (int i = 0; i < FlightTimeline.Release / Frame + 10 && a.Holds(7); i++) a.Frame(Planet, false, [(7, true)], null, false, Frame);
        Assert.False(a.Holds(7));
        Assert.Null(a.SecondsSince(7));
    }

    [Fact]
    public void AnEmptyPodOrOneBoardedWhereItStoodDoesNotArrive()
    {
        var a = Opened((7, false));
        a.Frame(Planet, false, [(7, false), (8, false)], null, false, Frame);
        a.Frame(Planet, false, [(7, true), (8, false)], null, false, Frame);
        Assert.False(a.Holds(7));
        Assert.False(a.Holds(8));
    }

    [Fact]
    public void YourOwnPodArrivesOnTheFrameYouLandButNoOtherPodThere()
    {
        var a = new Arrivals();
        a.Frame(Ship, true, [(1, false)], null, true, Frame);
        // The switch to the planet: its pods are all new to this player; only the one it came down in arrives.
        a.Frame(Planet, false, [(5, true), (7, true)], ownLanded: 7, false, Frame);
        Assert.True(a.Holds(7));
        Assert.False(a.Holds(5));
    }

    [Fact]
    public void NothingArrivesAboard()
    {
        var a = new Arrivals();
        a.Frame(Ship, true, [], null, true, Frame);
        a.Frame(Ship, true, [(3, true)], ownLanded: 3, false, Frame);
        Assert.False(a.Holds(3));
    }

    [Fact]
    public void APodLeftOrFlownInsideItsHoldIsLetGoAtOnce()
    {
        var a = Opened();
        a.Frame(Planet, false, [(7, true)], null, false, Frame);
        a.Frame(Planet, false, [(7, false)], null, false, Frame);
        Assert.False(a.Holds(7));
        a.Frame(Planet, false, [(8, true)], null, false, Frame);
        a.Frame(Planet, false, [], null, false, Frame);
        Assert.False(a.Holds(8));
    }

    [Fact]
    public void ALongFrameCountsAsAShortOneSoTheTouchdownIsSeen()
    {
        var a = Opened();
        a.Frame(Planet, false, [(7, true)], null, false, Frame);
        a.Frame(Planet, false, [(7, true)], null, false, 2.0);
        Assert.Equal(Arrivals.MostDelta, a.SecondsSince(7));
    }
}
