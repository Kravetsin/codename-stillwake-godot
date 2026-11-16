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
        // Docked back aboard: heard for a moment, never held back.
        Assert.Equal(0, a.DockedSince(3));
        for (int i = 0; i < FlightTimeline.DockS / Frame + 10; i++) a.Frame(Ship, true, [(3, true)], null, false, Frame);
        Assert.Null(a.DockedSince(3));
        // A friend's pod docking where this player stands aboard is heard too.
        a.Frame(Ship, true, [(3, true), (4, true)], null, false, Frame);
        Assert.Equal(0, a.DockedSince(4));
        Assert.False(a.Holds(4));
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

    private const int Vessel = 2;

    /// <summary>A frame on <paramref name="surface"/> with no pods and these shuttles, each with its riders.</summary>
    private static void Shuttles(Arrivals a, int surface, int? ownLanded, params (int Shuttle, int Riders)[] shuttles) =>
        a.Frame(surface, surface == Ship, [], ownLanded, false, Frame, shuttles);

    [Fact]
    public void AShuttleComeWithItsRidersIsHeldOffTheShipTillTheCameraHasClosedIn()
    {
        var a = new Arrivals();
        a.Frame(Vessel, false, [], null, true, Frame);
        Shuttles(a, Vessel, null, (9, 2));
        Assert.True(a.Holds(9));
        Assert.Equal((0.0, false), a.ShuttleSince(9));
        int frames = 0;
        while (a.Holds(9) && frames < 1000)
        {
            Shuttles(a, Vessel, null, (9, 2));
            frames++;
        }
        Assert.Equal(ShuttleTimeline.Hold(false), frames * Frame, 1);
        Assert.Null(a.ShuttleSince(9));
    }

    [Fact]
    public void AShuttleComeHomeIsHeldAboardForItsRiseThroughTheRun()
    {
        var a = new Arrivals();
        a.Frame(Vessel, false, [], null, true, Frame);
        // Its rider's own, the frame its voyage ends and the hero is home: another shuttle there stands.
        Shuttles(a, Ship, ownLanded: 9, (9, 1), (4, 0), (5, 2));
        Assert.Equal((0.0, true), a.ShuttleSince(9));
        Assert.False(a.Holds(4));
        Assert.False(a.Holds(5));
        int frames = 0;
        while (a.Holds(9) && frames < 1000)
        {
            Shuttles(a, Ship, null, (9, 1), (4, 0), (5, 2));
            frames++;
        }
        Assert.Equal(ShuttleTimeline.Hold(true), frames * Frame, 1);
        // Not heard docking as a pod is: it is drawn.
        Assert.Null(a.DockedSince(9));
    }

    [Fact]
    public void AShuttleIsLetGoAtOnceWhenARiderGetsOut()
    {
        var a = new Arrivals();
        a.Frame(Vessel, false, [], null, true, Frame);
        Shuttles(a, Vessel, null, (9, 1));
        // A second rider sits down in it, come on the same voyage: it is still coming.
        Shuttles(a, Vessel, null, (9, 2));
        Assert.True(a.Holds(9));
        int version = a.Version;
        Shuttles(a, Vessel, null, (9, 1));
        Assert.False(a.Holds(9));
        Assert.NotEqual(version, a.Version);
        // Nor does it come again when the rider sits back in it.
        Shuttles(a, Vessel, null, (9, 2));
        Assert.False(a.Holds(9));
    }

    [Fact]
    public void AnEmptyShuttleOrOneInAWorldJustLoadedDoesNotArrive()
    {
        var a = new Arrivals();
        a.Frame(Vessel, false, [], null, true, Frame, [(9, 2)]);
        Assert.False(a.Holds(9));
        Shuttles(a, Vessel, null, (9, 2), (11, 0));
        Assert.False(a.Holds(9));
        Assert.False(a.Holds(11));
    }
}
